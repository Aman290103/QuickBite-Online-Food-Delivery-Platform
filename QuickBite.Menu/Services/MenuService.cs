using Microsoft.Extensions.Caching.Distributed;
using QuickBite.Menu.DTOs;
using QuickBite.Menu.Entities;
using QuickBite.Menu.Interfaces;
using System.Text.Json;

namespace QuickBite.Menu.Services
{
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
            var cacheKey = $"{CacheKeyPrefix}{restaurantId}";
            string? cachedData = null;
            try { cachedData = await _cache.GetStringAsync(cacheKey); } catch { }

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<MenuResponseDto>(cachedData)!;
            }

            var categories = await _repository.GetMenuByRestaurantIdAsync(restaurantId);
            var response = new MenuResponseDto(
                restaurantId,
                categories.Select(MapToCategoryDto).ToList()
            );

            try
            {
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
            } catch { }

            return response;
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
            // Note: In real app, we should verify that this OwnerId owns the RestaurantId
            // via a call to Restaurant Service or by passing it in the token.
            
            var category = new MenuCategory
            {
                CategoryId = Guid.NewGuid(),
                RestaurantId = dto.RestaurantId,
                Name = dto.Name,
                Description = dto.Description,
                DisplayOrder = dto.DisplayOrder
            };

            await _repository.AddCategoryAsync(category);
            await InvalidateCache(dto.RestaurantId);
            
            return MapToCategoryDto(category);
        }

        public async Task UpdateCategoryAsync(Guid ownerId, Guid categoryId, AddCategoryDto dto)
        {
            var category = await _repository.GetCategoryByIdAsync(categoryId);
            if (category == null) throw new Exception("Category not found.");

            category.Name = dto.Name;
            category.Description = dto.Description;
            category.DisplayOrder = dto.DisplayOrder;

            await _repository.UpdateCategoryAsync(category);
            await InvalidateCache(category.RestaurantId);
        }

        public async Task DeleteCategoryAsync(Guid ownerId, Guid categoryId)
        {
            var category = await _repository.GetCategoryByIdAsync(categoryId);
            if (category == null) throw new Exception("Category not found.");

            await _repository.DeleteCategoryAsync(category);
            await InvalidateCache(category.RestaurantId);
        }

        public async Task<MenuItemResponseDto> AddMenuItemAsync(Guid ownerId, AddMenuItemDto dto)
        {
            var item = new MenuItem
            {
                ItemId = Guid.NewGuid(),
                RestaurantId = dto.RestaurantId,
                CategoryId = dto.CategoryId,
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                DiscountedPrice = dto.DiscountedPrice,
                IsVeg = dto.IsVeg,
                Calories = dto.Calories,
                Tags = string.Join(",", dto.Tags),
                IsAvailable = true
            };

            await _repository.AddMenuItemAsync(item);
            await InvalidateCache(dto.RestaurantId);

            return MapToItemDto(item);
        }

        public async Task<MenuItemResponseDto> UpdateMenuItemAsync(Guid ownerId, Guid itemId, UpdateMenuItemDto dto)
        {
            var item = await _repository.GetItemByIdAsync(itemId);
            if (item == null) throw new Exception("Item not found.");

            item.Name = dto.Name;
            item.Description = dto.Description;
            item.Price = dto.Price;
            item.DiscountedPrice = dto.DiscountedPrice;
            item.IsVeg = dto.IsVeg;
            item.Calories = dto.Calories;
            item.Tags = string.Join(",", dto.Tags);

            await _repository.UpdateMenuItemAsync(item);
            await InvalidateCache(item.RestaurantId);

            return MapToItemDto(item);
        }

        public async Task ToggleItemAvailabilityAsync(Guid ownerId, Guid itemId)
        {
            var item = await _repository.GetItemByIdAsync(itemId);
            if (item == null) throw new Exception("Item not found.");

            item.IsAvailable = !item.IsAvailable;
            await _repository.UpdateMenuItemAsync(item);
            await InvalidateCache(item.RestaurantId);
        }

        public async Task DeleteMenuItemAsync(Guid ownerId, Guid itemId)
        {
            var item = await _repository.GetItemByIdAsync(itemId);
            if (item == null) throw new Exception("Item not found.");

            await _repository.DeleteMenuItemAsync(item);
            await InvalidateCache(item.RestaurantId);
        }

        private async Task InvalidateCache(Guid restaurantId)
        {
            try { await _cache.RemoveAsync($"{CacheKeyPrefix}{restaurantId}"); } catch { }
        }

        private MenuCategoryResponseDto MapToCategoryDto(MenuCategory c) => new MenuCategoryResponseDto(
            c.CategoryId, c.Name, c.Description, c.ImageUrl, c.DisplayOrder,
            c.Items?.Select(MapToItemDto).ToList() ?? new()
        );

        private MenuItemResponseDto MapToItemDto(MenuItem m) => new MenuItemResponseDto(
            m.ItemId, m.CategoryId, m.Name, m.Description, m.Price, m.DiscountedPrice,
            m.ImageUrl, m.IsVeg, m.IsAvailable, m.Rating, m.Calories,
            m.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
        );
    }
}
