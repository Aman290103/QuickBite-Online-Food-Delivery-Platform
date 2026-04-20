using QuickBite.Cart.DTOs;

namespace QuickBite.Cart.Interfaces
{
    public interface ICartService
    {
        Task<CartResponseDto> GetCartAsync(Guid customerId);
        Task<CartResponseDto> AddToCartAsync(Guid customerId, AddToCartDto dto);
        Task<CartResponseDto> UpdateQuantityAsync(Guid customerId, Guid itemId, int quantity);
        Task<CartResponseDto> RemoveItemAsync(Guid customerId, Guid itemId);
        Task ClearCartAsync(Guid customerId);
        Task<CartResponseDto> ApplyPromoCodeAsync(Guid customerId, string code);
        Task ClearAndSwitchRestaurantAsync(Guid customerId, Guid newRestaurantId);
    }
}
