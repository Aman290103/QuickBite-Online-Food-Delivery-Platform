using Microsoft.EntityFrameworkCore;
using QuickBite.Menu.Data;
using QuickBite.Menu.Entities;
using QuickBite.Menu.Interfaces;

namespace QuickBite.Menu.Repositories
{
    public class MenuRepository : IMenuRepository
    {
        private readonly MenuDbContext _context;

        public MenuRepository(MenuDbContext context)
        {
            _context = context;
        }

        public async Task<MenuCategory?> GetCategoryByIdAsync(Guid id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<MenuItem?> GetItemByIdAsync(Guid id)
        {
            return await _context.Items.FindAsync(id);
        }

        public async Task<IEnumerable<MenuCategory>> GetMenuByRestaurantIdAsync(Guid restaurantId)
        {
            return await _context.Categories
                .Include(c => c.Items)
                .Where(c => c.RestaurantId == restaurantId)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> SearchItemsAsync(Guid restaurantId, string keyword)
        {
            return await _context.Items
                .Where(m => m.RestaurantId == restaurantId && 
                           (m.Name.Contains(keyword) || m.Tags.Contains(keyword)) &&
                            m.IsAvailable)
                .ToListAsync();
        }

        public async Task<IEnumerable<MenuItem>> GetVegItemsAsync(Guid restaurantId)
        {
            return await _context.Items
                .Where(m => m.RestaurantId == restaurantId && m.IsVeg && m.IsAvailable)
                .ToListAsync();
        }

        public async Task AddCategoryAsync(MenuCategory category)
        {
            await _context.Categories.AddAsync(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(MenuCategory category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCategoryAsync(MenuCategory category)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task AddMenuItemAsync(MenuItem item)
        {
            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMenuItemAsync(MenuItem item)
        {
            _context.Items.Update(item);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMenuItemAsync(MenuItem item)
        {
            _context.Items.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
