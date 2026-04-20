using Microsoft.EntityFrameworkCore;
using QuickBite.Cart.Data;
using QuickBite.Cart.Entities;
using QuickBite.Cart.Interfaces;

namespace QuickBite.Cart.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly CartDbContext _context;

        public CartRepository(CartDbContext context)
        {
            _context = context;
        }

        public async Task<Entities.Cart?> GetCartByCustomerIdAsync(Guid customerId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public async Task<PromoCode?> GetPromoCodeAsync(string code)
        {
            return await _context.PromoCodes
                .FirstOrDefaultAsync(p => p.Code == code && p.IsActive && p.ExpiresAt > DateTime.UtcNow);
        }

        public async Task AddCartAsync(Entities.Cart cart)
        {
            await _context.Carts.AddAsync(cart);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCartAsync(Entities.Cart cart)
        {
            _context.Carts.Update(cart);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCartAsync(Entities.Cart cart)
        {
            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();
        }

        public async Task AddCartItemAsync(CartItem item)
        {
            await _context.CartItems.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCartItemAsync(CartItem item)
        {
            _context.CartItems.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCartItemAsync(CartItem item)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
        }

        public async Task ClearCartItemsAsync(Guid cartId)
        {
            var items = await _context.CartItems.Where(i => i.CartId == cartId).ToListAsync();
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }
    }
}
