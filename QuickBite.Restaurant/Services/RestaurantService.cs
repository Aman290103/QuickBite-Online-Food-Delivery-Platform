using Microsoft.Extensions.Caching.Distributed;
using QuickBite.Restaurant.DTOs;
using QuickBite.Restaurant.Entities;
using QuickBite.Restaurant.Interfaces;
using System.Text.Json;

namespace QuickBite.Restaurant.Services
{
    public class RestaurantService : IRestaurantService
    {
        private readonly IRestaurantRepository _repository;
        private readonly IDistributedCache _cache;
        private const string CacheKeyPrefix = "Restaurant_";

        public RestaurantService(IRestaurantRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<RestaurantResponseDto> RegisterRestaurantAsync(Guid ownerId, RegisterRestaurantDto dto)
        {
            var restaurant = new Entities.Restaurant
            {
                RestaurantId = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = dto.Name,
                Description = dto.Description,
                Cuisine = dto.Cuisine,
                Address = dto.Address,
                City = dto.City,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Phone = dto.Phone,
                DeliveryRadiusKm = dto.DeliveryRadiusKm,
                MinOrderAmount = dto.MinOrderAmount,
                EstimatedDeliveryMin = dto.EstimatedDeliveryMin,
                IsApproved = false,
                IsOpen = false
            };

            await _repository.AddAsync(restaurant);
            return MapToDto(restaurant);
        }

        public async Task<RestaurantResponseDto?> GetRestaurantByIdAsync(Guid id)
        {
            var cacheKey = $"{CacheKeyPrefix}{id}";
            string? cachedData = null;
            try
            {
                cachedData = await _cache.GetStringAsync(cacheKey);
            }
            catch (Exception)
            {
                // Redis is down, proceed to database
            }

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<RestaurantResponseDto>(cachedData);
            }

            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null) return null;

            var dto = MapToDto(restaurant);
            
            if (restaurant.IsApproved)
            {
                try
                {
                    await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(dto), new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                    });
                }
                catch (Exception)
                {
                    // Redis is down, skip caching
                }
            }

            return dto;
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetNearbyRestaurantsAsync(double lat, double lon, double radius)
        {
            var restaurants = await _repository.FindNearbyAsync(lat, lon, radius);
            return restaurants.Select(MapToDto);
        }

        public async Task<IEnumerable<RestaurantResponseDto>> SearchRestaurantsAsync(string name)
        {
            var results = await _repository.SearchByNameAsync(name);
            return results.Select(MapToDto);
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetRestaurantsByCuisineAsync(string cuisine)
        {
            var results = await _repository.FilterByCuisineAsync(cuisine);
            return results.Select(MapToDto);
        }

        public async Task<RestaurantResponseDto> UpdateRestaurantAsync(Guid id, Guid ownerId, UpdateRestaurantDto dto)
        {
            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null || restaurant.OwnerId != ownerId)
                throw new UnauthorizedAccessException("Not authorized to update this restaurant.");

            restaurant.Name = dto.Name;
            restaurant.Description = dto.Description;
            restaurant.Cuisine = dto.Cuisine;
            restaurant.Address = dto.Address;
            restaurant.City = dto.City;
            restaurant.Latitude = dto.Latitude;
            restaurant.Longitude = dto.Longitude;
            restaurant.Phone = dto.Phone;
            restaurant.DeliveryRadiusKm = dto.DeliveryRadiusKm;
            restaurant.MinOrderAmount = dto.MinOrderAmount;
            restaurant.EstimatedDeliveryMin = dto.EstimatedDeliveryMin;

            await _repository.UpdateAsync(restaurant);
            try
            {
                await _cache.RemoveAsync($"{CacheKeyPrefix}{id}");
            }
            catch (Exception) { }
            
            return MapToDto(restaurant);
        }

        public async Task ApproveRestaurantAsync(Guid id)
        {
            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null) throw new Exception("Restaurant not found.");

            restaurant.IsApproved = true;
            await _repository.UpdateAsync(restaurant);
        }

        public async Task ToggleRestaurantStatusAsync(Guid id, Guid ownerId)
        {
            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null || restaurant.OwnerId != ownerId)
                throw new UnauthorizedAccessException("Not authorized.");

            restaurant.IsOpen = !restaurant.IsOpen;
            await _repository.UpdateAsync(restaurant);
            try
            {
                await _cache.RemoveAsync($"{CacheKeyPrefix}{id}");
            }
            catch (Exception) { }
        }

        public async Task UpdateRatingAsync(Guid id, double newRating)
        {
            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null) throw new Exception("Restaurant not found.");

            restaurant.AvgRating = newRating;
            await _repository.UpdateAsync(restaurant);
            try
            {
                await _cache.RemoveAsync($"{CacheKeyPrefix}{id}");
            }
            catch (Exception) { }
        }

        public async Task DeleteRestaurantAsync(Guid id)
        {
            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null) throw new Exception("Restaurant not found.");

            await _repository.DeleteAsync(restaurant);
            try
            {
                await _cache.RemoveAsync($"{CacheKeyPrefix}{id}");
            }
            catch (Exception) { }
        }

        // --- Review Implementation ---

        public async Task<ReviewResponseDto> SubmitReviewAsync(Guid restaurantId, Guid customerId, AddReviewDto dto)
        {
            var exists = await _repository.ExistsReviewByOrderIdAsync(dto.OrderId);
            if (exists) throw new InvalidOperationException("Review already exists for this order.");

            var review = new RestaurantReview
            {
                ReviewId = Guid.NewGuid(),
                RestaurantId = restaurantId,
                CustomerId = customerId,
                OrderId = dto.OrderId,
                FoodRating = dto.FoodRating,
                Comment = dto.Comment
            };
            await _repository.AddReviewAsync(review);

            var restaurant = await _repository.GetByIdAsync(restaurantId);
            if (restaurant == null) throw new Exception("Restaurant not found.");
            
            await _repository.SaveChangesAsync(); 
            
            restaurant.AvgRating = await _repository.GetAvgFoodRatingAsync(restaurantId); 
            await _repository.SaveChangesAsync(); 

            try { await _cache.RemoveAsync($"{CacheKeyPrefix}{restaurantId}"); } catch { }

            return MapToReviewEntityDto(review);
        }

        public async Task<IEnumerable<ReviewResponseDto>> GetReviewsAsync(Guid restaurantId, int page, int pageSize)
        {
            var reviews = await _repository.GetReviewsByRestaurantIdAsync(restaurantId, page, pageSize);
            return reviews.Select(MapToReviewEntityDto);
        }

        public async Task<double> GetAvgRatingAsync(Guid restaurantId)
        {
            return await _repository.GetAvgFoodRatingAsync(restaurantId);
        }

        public async Task DeleteReviewAsync(Guid reviewId)
        {
            var review = await _repository.GetReviewByIdAsync(reviewId);
            if (review == null) throw new Exception("Review not found.");

            await _repository.DeleteReviewAsync(review);
            await _repository.SaveChangesAsync();
            
            var restaurant = await _repository.GetByIdAsync(review.RestaurantId);
            if (restaurant != null)
            {
                restaurant.AvgRating = await _repository.GetAvgFoodRatingAsync(review.RestaurantId);
                await _repository.SaveChangesAsync();
                try { await _cache.RemoveAsync($"{CacheKeyPrefix}{review.RestaurantId}"); } catch { }
            }
        }

        private ReviewResponseDto MapToReviewEntityDto(RestaurantReview r) => new ReviewResponseDto(
            r.ReviewId, r.RestaurantId, r.OrderId, r.CustomerId, "Customer", r.FoodRating, r.Comment, r.ReviewDate
        );

        private RestaurantResponseDto MapToDto(Entities.Restaurant r) => new RestaurantResponseDto(
            r.RestaurantId, r.OwnerId, r.Name, r.Description, r.Cuisine, r.Address, r.City, r.Latitude, r.Longitude, r.Phone, r.AvgRating, r.IsOpen, r.IsApproved, r.DeliveryRadiusKm, r.MinOrderAmount, r.EstimatedDeliveryMin, r.CreatedAt
        );
    }
}
