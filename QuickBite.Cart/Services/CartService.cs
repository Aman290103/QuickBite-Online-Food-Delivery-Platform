using Microsoft.Extensions.Caching.Distributed;
using QuickBite.Cart.DTOs;
using QuickBite.Cart.Entities;
using QuickBite.Cart.Interfaces;
using System.Text.Json;

namespace QuickBite.Cart.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _repository;
        private readonly IDistributedCache _cache;
        private const string CacheKeyPrefix = "Cart_";

        public CartService(ICartRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<CartResponseDto> GetCartAsync(Guid customerId)
        {
            var cart = await GetOrCreateCart(customerId);
            return MapToDto(cart);
        }

        public async Task<CartResponseDto> AddToCartAsync(Guid customerId, AddToCartDto dto)
        {
            var cart = await GetOrCreateCart(customerId);

            // Single Restaurant Rule
            if (cart.Items.Any() && cart.RestaurantId != dto.RestaurantId)
            {
                throw new InvalidOperationException("You can only add items from one restaurant at a time. Clear your cart or switch restaurant.");
            }

            // Set RestaurantId if cart is empty
            if (!cart.Items.Any())
            {
                cart.RestaurantId = dto.RestaurantId;
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.MenuItemId == dto.MenuItemId);
            if (existingItem != null)
            {
                existingItem.Quantity += dto.Quantity;
                await _repository.UpdateCartItemAsync(existingItem);
            }
            else
            {
                var newItem = new CartItem
                {
                    ItemId = Guid.NewGuid(),
                    CartId = cart.CartId,
                    MenuItemId = dto.MenuItemId,
                    Name = dto.Name,
                    Price = dto.Price, // Snapshot
                    Quantity = dto.Quantity,
                    Customization = dto.Customization
                };
                await _repository.AddCartItemAsync(newItem);
                cart.Items.Add(newItem);
            }

            await RecalculateAndSave(cart);
            return MapToDto(cart);
        }

        public async Task<CartResponseDto> UpdateQuantityAsync(Guid customerId, Guid itemId, int quantity)
        {
            var cart = await GetOrCreateCart(customerId);
            var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null) throw new Exception("Item not found in cart.");

            if (quantity <= 0)
            {
                await _repository.DeleteCartItemAsync(item);
                cart.Items.Remove(item);
            }
            else
            {
                item.Quantity = quantity;
                await _repository.UpdateCartItemAsync(item);
            }

            await RecalculateAndSave(cart);
            return MapToDto(cart);
        }

        public async Task<CartResponseDto> RemoveItemAsync(Guid customerId, Guid itemId)
        {
            var cart = await GetOrCreateCart(customerId);
            var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null) throw new Exception("Item not found in cart.");

            await _repository.DeleteCartItemAsync(item);
            cart.Items.Remove(item);

            await RecalculateAndSave(cart);
            return MapToDto(cart);
        }

        public async Task ClearCartAsync(Guid customerId)
        {
            var cart = await _repository.GetCartByCustomerIdAsync(customerId);
            if (cart != null)
            {
                await _repository.ClearCartItemsAsync(cart.CartId);
                cart.Items.Clear();
                cart.AppliedPromoCode = null;
                cart.DiscountAmount = 0;
                await RecalculateAndSave(cart);
            }
        }

        public async Task<CartResponseDto> ApplyPromoCodeAsync(Guid customerId, string code)
        {
            var cart = await GetOrCreateCart(customerId);
            var promo = await _repository.GetPromoCodeAsync(code);
            
            if (promo == null) throw new Exception("Invalid or expired promo code.");

            cart.AppliedPromoCode = promo.Code;
            await RecalculateAndSave(cart);
            return MapToDto(cart);
        }

        public async Task ClearAndSwitchRestaurantAsync(Guid customerId, Guid newRestaurantId)
        {
            var cart = await GetOrCreateCart(customerId);
            await _repository.ClearCartItemsAsync(cart.CartId);
            cart.Items.Clear();
            cart.RestaurantId = newRestaurantId;
            cart.AppliedPromoCode = null;
            cart.DiscountAmount = 0;
            await RecalculateAndSave(cart);
        }

        private async Task<Entities.Cart> GetOrCreateCart(Guid customerId)
        {
            var cart = await _repository.GetCartByCustomerIdAsync(customerId);
            if (cart == null)
            {
                cart = new Entities.Cart
                {
                    CartId = Guid.NewGuid(),
                    CustomerId = customerId,
                    RestaurantId = Guid.Empty
                };
                await _repository.AddCartAsync(cart);
            }
            return cart;
        }

        private async Task RecalculateAndSave(Entities.Cart cart)
        {
            cart.SubTotal = cart.Items.Sum(i => i.Price * i.Quantity);
            
            if (!string.IsNullOrEmpty(cart.AppliedPromoCode))
            {
                var promo = await _repository.GetPromoCodeAsync(cart.AppliedPromoCode);
                if (promo != null)
                {
                    if (promo.DiscountType == DiscountType.PERCENT)
                        cart.DiscountAmount = cart.SubTotal * (promo.Value / 100);
                    else
                        cart.DiscountAmount = Math.Min(promo.Value, cart.SubTotal);
                }
                else
                {
                    cart.AppliedPromoCode = null;
                    cart.DiscountAmount = 0;
                }
            }

            cart.GrandTotal = cart.SubTotal - cart.DiscountAmount;
            cart.UpdatedAt = DateTime.UtcNow;
            
            await _repository.UpdateCartAsync(cart);
            
            // Update Cache
            try
            {
                await _cache.SetStringAsync($"{CacheKeyPrefix}{cart.CustomerId}", JsonSerializer.Serialize(MapToDto(cart)), new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            } catch { }
        }

        private CartResponseDto MapToDto(Entities.Cart c) => new CartResponseDto(
            c.CartId,
            c.RestaurantId,
            c.Items.Select(i => new CartItemResponseDto(
                i.ItemId, i.MenuItemId, i.Name, i.Price, i.Quantity, i.Price * i.Quantity, i.Customization
            )).ToList(),
            c.SubTotal,
            c.DiscountAmount,
            c.AppliedPromoCode,
            c.GrandTotal
        );
    }
}
