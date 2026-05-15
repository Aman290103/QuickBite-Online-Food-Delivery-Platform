using Microsoft.Extensions.Caching.Distributed;
using QuickBite.Cart.DTOs;
using QuickBite.Cart.Entities;
using QuickBite.Cart.Interfaces;
using System.Text.Json;

namespace QuickBite.Cart.Services
{
    // [SERVICE: CART MANAGEMENT]
    // Manages user shopping carts with auto-recalculation for taxes and discounts.
    public class CartService : ICartService
    {
        private readonly ICartRepository _repository;
        private readonly IDistributedCache _cache;

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
            
            if (cart.Items.Any() && cart.RestaurantId != dto.RestaurantId)
            {
                throw new InvalidOperationException("One restaurant at a time.");
            }

            if (!cart.Items.Any()) 
            {
                cart.RestaurantId = dto.RestaurantId;
                cart.RestaurantName = dto.RestaurantName;
                await _repository.UpdateCartAsync(cart);
            }

            var item = new CartItem {
                ItemId = Guid.NewGuid(),
                CartId = cart.CartId,
                MenuItemId = dto.MenuItemId,
                Name = dto.Name,
                Price = dto.Price,
                Quantity = dto.Quantity,
                Customization = dto.Customization
            };
            await _repository.AddCartItemAsync(item);
            
            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task<CartResponseDto> UpdateQuantityAsync(Guid customerId, Guid itemId, int quantity)
        {
            var cart = await GetOrCreateCart(customerId);
            var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item != null) {
                if (quantity <= 0) await _repository.DeleteCartItemAsync(item);
                else { item.Quantity = quantity; await _repository.UpdateCartItemAsync(item); }
            }
            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task<CartResponseDto> RemoveItemAsync(Guid customerId, Guid itemId)
        {
            var cart = await GetOrCreateCart(customerId);
            var item = cart.Items.FirstOrDefault(i => i.ItemId == itemId);
            if (item != null) await _repository.DeleteCartItemAsync(item);
            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task ClearCartAsync(Guid customerId)
        {
            var cart = await _repository.GetCartByCustomerIdAsync(customerId);
            if (cart != null) {
                await _repository.ClearCartItemsAsync(cart.CartId);
                cart.RestaurantId = Guid.Empty;
                cart.RestaurantName = "Restaurant";
                cart.AppliedPromoCode = null;
                await _repository.UpdateCartAsync(cart);
            }
        }

        public async Task<CartResponseDto> ApplyPromoCodeAsync(Guid customerId, string code)
        {
            var cart = await GetOrCreateCart(customerId);
            cart.AppliedPromoCode = code;
            var updated = await RecalculateAndSave(cart.CartId);
            return MapToDto(updated);
        }

        public async Task ClearAndSwitchRestaurantAsync(Guid customerId, Guid newRestaurantId)
        {
            await ClearCartAsync(customerId);
            var cart = await GetOrCreateCart(customerId);
            cart.RestaurantId = newRestaurantId;
            await _repository.UpdateCartAsync(cart);
        }

        private async Task<Entities.Cart> RecalculateAndSave(Guid cartId)
        {
            var cart = await _repository.GetCartByIdAsync(cartId);
            if (cart == null) throw new Exception("Cart not found");

            cart.SubTotal = cart.Items.Sum(i => i.Price * i.Quantity);
            
            if (!string.IsNullOrEmpty(cart.AppliedPromoCode)) {
                var promo = await _repository.GetPromoCodeAsync(cart.AppliedPromoCode);
                if (promo != null) {
                    if (promo.DiscountType == DiscountType.PERCENT) cart.DiscountAmount = cart.SubTotal * (promo.Value / 100);
                    else cart.DiscountAmount = Math.Min(promo.Value, cart.SubTotal);
                }
            } else { cart.DiscountAmount = 0; }

            cart.TaxAmount = Math.Round((cart.SubTotal - cart.DiscountAmount) * 0.18m, 2);
            cart.GrandTotal = cart.SubTotal - cart.DiscountAmount + cart.TaxAmount;
            
            await _repository.UpdateCartAsync(cart);
            return cart;
        }

        private async Task<Entities.Cart> GetOrCreateCart(Guid customerId)
        {
            var cart = await _repository.GetCartByCustomerIdAsync(customerId);
            if (cart == null) {
                cart = new Entities.Cart { CartId = Guid.NewGuid(), CustomerId = customerId };
                await _repository.AddCartAsync(cart);
            }
            return cart;
        }

        private CartResponseDto MapToDto(Entities.Cart c) => new CartResponseDto(
            c.CartId, c.RestaurantId, c.RestaurantName,
            c.Items.Select(i => new CartItemResponseDto(i.ItemId, i.MenuItemId, i.Name, i.Price, i.Quantity, i.Price * i.Quantity, i.Customization)).ToList(),
            c.SubTotal, c.DiscountAmount, c.TaxAmount, c.AppliedPromoCode, c.GrandTotal
        );
    }
}
