using QuickBite.Menu.Entities;

namespace QuickBite.Menu.Interfaces
{
    public interface IMenuRepository
    {
        Task<MenuCategory?> GetCategoryByIdAsync(Guid id);
        Task<MenuItem?> GetItemByIdAsync(Guid id);
        Task<IEnumerable<MenuCategory>> GetMenuByRestaurantIdAsync(Guid restaurantId);
        Task<IEnumerable<MenuItem>> SearchItemsAsync(Guid restaurantId, string keyword);
        Task<IEnumerable<MenuItem>> GetVegItemsAsync(Guid restaurantId);
        
        Task AddCategoryAsync(MenuCategory category);
        Task UpdateCategoryAsync(MenuCategory category);
        Task DeleteCategoryAsync(MenuCategory category);
        
        Task AddMenuItemAsync(MenuItem item);
        Task UpdateMenuItemAsync(MenuItem item);
        Task DeleteMenuItemAsync(MenuItem item);
    }
}
