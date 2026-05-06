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
            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task<CartResponseDto> AddToCartAsync(Guid customerId, AddToCartDto dto)
        {
            var cart = await GetOrCreateCart(customerId);

            // Single Restaurant Rule
            if (cart.Items.Any() && cart.RestaurantId != dto.RestaurantId)
            {
                throw new InvalidOperationException("You can only add items from one restaurant at a time.");
            }

            if (!cart.Items.Any()) 
            {
                cart.RestaurantId = dto.RestaurantId;
                cart.RestaurantName = dto.RestaurantName;
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
                    Price = dto.Price,
                    Quantity = dto.Quantity,
                    Customization = dto.Customization
                };
                await _repository.AddCartItemAsync(newItem);
            }

            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task<CartResponseDto> UpdateQuantityAsync(Guid customerId, Guid itemId, int quantity)
        {
            var cart = await GetOrCreateCart(customerId);
            var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item == null) throw new Exception("Item not found.");

            if (quantity <= 0)
            {
                await _repository.DeleteCartItemAsync(item);
            }
            else
            {
                item.Quantity = quantity;
                await _repository.UpdateCartItemAsync(item);
            }

            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task<CartResponseDto> RemoveItemAsync(Guid customerId, Guid itemId)
        {
            var cart = await GetOrCreateCart(customerId);
            var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item != null)
            {
                await _repository.DeleteCartItemAsync(item);
            }

            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task ClearCartAsync(Guid customerId)
        {
            var cart = await _repository.GetCartByCustomerIdAsync(customerId);
            if (cart != null)
            {
                await _repository.ClearCartItemsAsync(cart.CartId);
                await RecalculateAndSave(cart.CartId);
            }
        }

        public async Task<CartResponseDto> ApplyPromoCodeAsync(Guid customerId, string code)
        {
            var cart = await GetOrCreateCart(customerId);
            var promo = await _repository.GetPromoCodeAsync(code);
            
            if (promo == null) throw new Exception("Invalid or expired promo code.");

            cart.AppliedPromoCode = promo.Code;
            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task ClearAndSwitchRestaurantAsync(Guid customerId, Guid newRestaurantId)
        {
            var cart = await GetOrCreateCart(customerId);
            await _repository.ClearCartItemsAsync(cart.CartId);
            cart.RestaurantId = newRestaurantId;
            cart.RestaurantName = "Restaurant"; // Ideally fetch from restaurant service, but we'll let AddToCart update it
            cart.AppliedPromoCode = null;
            cart.DiscountAmount = 0;
            await RecalculateAndSave(cart.CartId);
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
        private async Task<Entities.Cart> RecalculateAndSave(Guid cartId)
        {
            // 1. Force a clean refresh from the DB
            var cart = await _repository.GetCartByIdAsync(cartId);
            if (cart == null) throw new Exception("Cart not found.");

            // 2. HARD MERGE: Ensure only one entry per MenuItemId
            var duplicates = cart.Items
                .GroupBy(i => i.MenuItemId)
                .Where(g => g.Count() > 1)
                .ToList();

            if (duplicates.Any())
            {
                foreach (var group in duplicates)
                {
                    var mainItem = group.First();
                    var others = group.Skip(1).ToList();

                    mainItem.Quantity = group.Sum(i => i.Quantity);
                    await _repository.UpdateCartItemAsync(mainItem);

                    foreach (var other in others)
                    {
                        await _repository.DeleteCartItemAsync(other);
                    }
                }
                // Re-fetch again after hard DB changes
                cart = await _repository.GetCartByIdAsync(cartId);
                if (cart == null) throw new Exception("Cart not found after merge.");
            }

            // 3. FINAL MATH
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

            // 4. TAXES: 18% GST
            var discountedSubtotal = cart.SubTotal - cart.DiscountAmount;
            cart.TaxAmount = Math.Round(discountedSubtotal * 0.18m, 2);
            cart.GrandTotal = Math.Round(discountedSubtotal + cart.TaxAmount, 2);
            cart.UpdatedAt = DateTime.UtcNow;
            
            await _repository.UpdateCartAsync(cart);

            // 5. Final Fetch to be absolutely certain
            var finalCart = await _repository.GetCartByIdAsync(cartId) ?? cart;

            // Update Cache
            try
            {
                await _cache.SetStringAsync($"{CacheKeyPrefix}{finalCart.CustomerId}", JsonSerializer.Serialize(MapToDto(finalCart)), new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromMinutes(30)
                });
            } catch { }

            return finalCart;
        }

        private CartResponseDto MapToDto(Entities.Cart c) => new CartResponseDto(
            c.CartId,
            c.RestaurantId,
            c.RestaurantName,
            c.Items.Select(i => new CartItemResponseDto(
                i.ItemId, i.MenuItemId, i.Name, i.Price, i.Quantity, i.Price * i.Quantity, i.Customization
            )).ToList(),
            c.SubTotal,
            c.DiscountAmount,
            c.TaxAmount,
            c.AppliedPromoCode,
            c.GrandTotal
        );
    }
}
