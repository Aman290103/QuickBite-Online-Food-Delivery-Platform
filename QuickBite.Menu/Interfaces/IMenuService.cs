using QuickBite.Menu.DTOs;

namespace QuickBite.Menu.Interfaces
{
    public interface IMenuService
    {
        Task<MenuResponseDto> GetMenuAsync(Guid restaurantId);
        Task<IEnumerable<MenuItemResponseDto>> SearchItemsAsync(Guid restaurantId, string keyword);
        Task<IEnumerable<MenuItemResponseDto>> GetVegItemsAsync(Guid restaurantId);
        
        Task<MenuCategoryResponseDto> AddCategoryAsync(Guid ownerId, AddCategoryDto dto);
        Task UpdateCategoryAsync(Guid ownerId, Guid categoryId, AddCategoryDto dto);
        Task DeleteCategoryAsync(Guid ownerId, Guid categoryId);
        
        Task<MenuItemResponseDto> AddMenuItemAsync(Guid ownerId, AddMenuItemDto dto);
        Task<MenuItemResponseDto> UpdateMenuItemAsync(Guid ownerId, Guid itemId, UpdateMenuItemDto dto);
        Task ToggleItemAvailabilityAsync(Guid ownerId, Guid itemId);
        Task DeleteMenuItemAsync(Guid ownerId, Guid itemId);

        // Review Logic
        Task<MenuItemReviewResponseDto> SubmitItemReviewAsync(Guid itemId, Guid customerId, SubmitMenuItemReviewDto dto);
        Task<IEnumerable<MenuItemReviewResponseDto>> GetItemReviewsAsync(Guid itemId, int page, int pageSize);
        Task<double> GetAvgItemRatingAsync(Guid itemId);
        Task ModerateItemReviewAsync(Guid reviewId);
    }
}
