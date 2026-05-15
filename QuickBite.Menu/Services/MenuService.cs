using Microsoft.Extensions.Caching.Distributed;
using QuickBite.Menu.DTOs;
using QuickBite.Menu.Entities;
using QuickBite.Menu.Interfaces;
using System.Text.Json;

namespace QuickBite.Menu.Services
{
    // [SERVICE: MENU MANAGEMENT]
    // Manages the food catalog and categories.
    public class MenuService : IMenuService
    {
        private readonly IMenuRepository _repository;
        private readonly IDistributedCache _cache;
        private const string CacheKeyPrefix = "Menu_";

        public MenuService(IMenuRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<MenuResponseDto> GetMenuAsync(Guid restaurantId)
        {
            var categories = await _repository.GetMenuByRestaurantIdAsync(restaurantId);
            return new MenuResponseDto(restaurantId, categories.Select(MapToCategoryDto).ToList());
        }

        public async Task<IEnumerable<MenuItemResponseDto>> SearchItemsAsync(Guid restaurantId, string keyword)
        {
            var items = await _repository.SearchItemsAsync(restaurantId, keyword);
            return items.Select(MapToItemDto);
        }

        public async Task<IEnumerable<MenuItemResponseDto>> GetVegItemsAsync(Guid restaurantId)
        {
            var items = await _repository.GetVegItemsAsync(restaurantId);
            return items.Select(MapToItemDto);
        }

        public async Task<MenuCategoryResponseDto> AddCategoryAsync(Guid ownerId, AddCategoryDto dto)
        {
            var c = new MenuCategory { CategoryId = Guid.NewGuid(), RestaurantId = dto.RestaurantId, Name = dto.Name, Description = dto.Description };
            await _repository.AddCategoryAsync(c);
            return MapToCategoryDto(c);
        }

        public async Task UpdateCategoryAsync(Guid ownerId, Guid categoryId, AddCategoryDto dto)
        {
            var c = await _repository.GetCategoryByIdAsync(categoryId);
            if (c != null) { c.Name = dto.Name; c.Description = dto.Description; await _repository.UpdateCategoryAsync(c); }
        }

        public async Task DeleteCategoryAsync(Guid ownerId, Guid categoryId)
        {
            var c = await _repository.GetCategoryByIdAsync(categoryId);
            if (c != null) await _repository.DeleteCategoryAsync(c);
        }

        public async Task<MenuItemResponseDto> AddMenuItemAsync(Guid ownerId, AddMenuItemDto dto)
        {
            var i = new MenuItem { ItemId = Guid.NewGuid(), RestaurantId = dto.RestaurantId, CategoryId = dto.CategoryId, Name = dto.Name, Price = dto.Price, IsVeg = dto.IsVeg, IsAvailable = true };
            await _repository.AddMenuItemAsync(i);
            return MapToItemDto(i);
        }

        public async Task<MenuItemResponseDto> UpdateMenuItemAsync(Guid ownerId, Guid itemId, UpdateMenuItemDto dto)
        {
            var i = await _repository.GetItemByIdAsync(itemId);
            if (i == null) throw new Exception("Not found");
            i.Name = dto.Name; i.Price = dto.Price; i.IsVeg = dto.IsVeg;
            await _repository.UpdateMenuItemAsync(i);
            return MapToItemDto(i);
        }

        public async Task ToggleItemAvailabilityAsync(Guid ownerId, Guid itemId)
        {
            var i = await _repository.GetItemByIdAsync(itemId);
            if (i != null) { i.IsAvailable = !i.IsAvailable; await _repository.UpdateMenuItemAsync(i); }
        }

        public async Task DeleteMenuItemAsync(Guid ownerId, Guid itemId)
        {
            var i = await _repository.GetItemByIdAsync(itemId);
            if (i != null) await _repository.DeleteMenuItemAsync(i);
        }

        public async Task<MenuItemReviewResponseDto> SubmitItemReviewAsync(Guid itemId, Guid customerId, SubmitMenuItemReviewDto dto)
        {
            var r = new MenuItemReview { ReviewId = Guid.NewGuid(), MenuItemId = itemId, CustomerId = customerId, ItemRating = dto.ItemRating, Comment = dto.Comment };
            await _repository.AddItemReviewAsync(r);
            return new MenuItemReviewResponseDto(r.ReviewId, r.MenuItemId, r.CustomerId, r.ItemRating, r.Comment, r.ReviewDate);
        }

        public async Task<IEnumerable<MenuItemReviewResponseDto>> GetItemReviewsAsync(Guid itemId, int page, int pageSize)
        {
            var reviews = await _repository.GetReviewsByItemIdAsync(itemId, page, pageSize);
            return reviews.Select(r => new MenuItemReviewResponseDto(r.ReviewId, r.MenuItemId, r.CustomerId, r.ItemRating, r.Comment, r.ReviewDate));
        }

        public async Task<double> GetAvgItemRatingAsync(Guid itemId)
        {
            return await _repository.GetAvgItemRatingAsync(itemId);
        }

        public async Task ModerateItemReviewAsync(Guid reviewId)
        {
            var review = await _repository.GetReviewByIdAsync(reviewId);
            if (review != null) await _repository.DeleteItemReviewAsync(review);
        }

        private MenuCategoryResponseDto MapToCategoryDto(MenuCategory c) => new MenuCategoryResponseDto(c.CategoryId, c.Name, c.Description, null, c.DisplayOrder, c.Items?.Select(MapToItemDto).ToList() ?? new());
        private MenuItemResponseDto MapToItemDto(MenuItem m) => new MenuItemResponseDto(m.ItemId, m.CategoryId, m.Name, m.Description, m.Price, m.DiscountedPrice, m.ImageUrl, m.IsVeg, m.IsAvailable, m.Rating, m.Calories, new());
    }
}
