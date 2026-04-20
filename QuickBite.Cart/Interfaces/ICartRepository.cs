using QuickBite.Cart.Entities;

namespace QuickBite.Cart.Interfaces
{
    public interface ICartRepository
    {
        Task<Entities.Cart?> GetCartByCustomerIdAsync(Guid customerId);
        Task<PromoCode?> GetPromoCodeAsync(string code);
        
        Task AddCartAsync(Entities.Cart cart);
        Task UpdateCartAsync(Entities.Cart cart);
        Task DeleteCartAsync(Entities.Cart cart);
        
        Task AddCartItemAsync(CartItem item);
        Task UpdateCartItemAsync(CartItem item);
        Task DeleteCartItemAsync(CartItem item);
        Task ClearCartItemsAsync(Guid cartId);
    }
}
