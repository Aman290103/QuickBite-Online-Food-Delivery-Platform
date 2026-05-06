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
            // 1. Priority check: PlaceId
            if (!string.IsNullOrEmpty(dto.PlaceId))
            {
                var existingByPlaceId = await _repository.GetAllAsync();
                var match = existingByPlaceId.FirstOrDefault(r => r.PlaceId == dto.PlaceId);
                if (match != null) return MapToDto(match);
            }

            // 2. Secondary check: Name + Address
            if (await _repository.ExistsByNameAndAddressAsync(dto.Name, dto.Address))
            {
                // Find and return existing one
                var existingResults = await _repository.SearchByNameAsync(dto.Name);
                var exactMatch = existingResults.FirstOrDefault(r => 
                    r.Name.ToLower().Trim() == dto.Name.ToLower().Trim() && 
                    r.Address.ToLower().Trim() == dto.Address.ToLower().Trim());
                
                if (exactMatch != null) return MapToDto(exactMatch);
            }

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
                ImageUrl = dto.ImageUrl,
                PlaceId = dto.PlaceId,
                AvgRating = dto.Rating > 0 ? (double)dto.Rating : 0.0, 
                IsApproved = false, 
                IsOpen = true,
                CreatedAt = DateTime.UtcNow
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
            catch (Exception) { }

            if (!string.IsNullOrEmpty(cachedData))
            {
                return JsonSerializer.Deserialize<RestaurantResponseDto>(cachedData);
            }

            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null) return null;

            // Direct query for stats to bypass any navigation property or filter issues
            var allReviews = await _repository.GetReviewsByRestaurantIdAsync(id, 1, 1000);
            var reviewsList = allReviews.ToList();
            
            restaurant.ReviewCount = reviewsList.Count;
            if (reviewsList.Any()) 
            {
                restaurant.AvgRating = reviewsList.Average(r => r.FoodRating);
            }

            var dto = MapToDto(restaurant);
            
            // Bypass cache for GetById during this dynamic phase to ensure users see updates instantly
            return dto;
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetNearbyRestaurantsAsync(double lat, double lon, double radius)
        {
            var restaurants = await _repository.FindNearbyAsync(lat, lon, radius);
            var results = new List<RestaurantResponseDto>();
            
            foreach (var r in restaurants.Where(r => r.IsApproved))
            {
                var reviews = (await _repository.GetReviewsByRestaurantIdAsync(r.RestaurantId, 1, 1000)).ToList();
                r.ReviewCount = reviews.Count;
                if (reviews.Any()) r.AvgRating = reviews.Average(rev => rev.FoodRating);
                results.Add(MapToDto(r));
            }
            
            return results;
        }

        public async Task<IEnumerable<RestaurantResponseDto>> SearchRestaurantsAsync(string name)
        {
            var results = await _repository.SearchByNameAsync(name);
            var dtos = new List<RestaurantResponseDto>();
            foreach (var r in results)
            {
                var reviews = (await _repository.GetReviewsByRestaurantIdAsync(r.RestaurantId, 1, 1000)).ToList();
                r.ReviewCount = reviews.Count;
                if (reviews.Any()) r.AvgRating = reviews.Average(rev => rev.FoodRating);
                dtos.Add(MapToDto(r));
            }
            return dtos;
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetRestaurantsByCuisineAsync(string cuisine)
        {
            var results = await _repository.FilterByCuisineAsync(cuisine);
            var dtos = new List<RestaurantResponseDto>();
            foreach (var r in results)
            {
                var reviews = (await _repository.GetReviewsByRestaurantIdAsync(r.RestaurantId, 1, 1000)).ToList();
                r.ReviewCount = reviews.Count;
                if (reviews.Any()) r.AvgRating = reviews.Average(rev => rev.FoodRating);
                dtos.Add(MapToDto(r));
            }
            return dtos;
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetRestaurantsByOwnerAsync(Guid ownerId)
        {
            var results = await _repository.GetAllAsync();
            return results.Where(r => r.OwnerId == ownerId).Select(MapToDto);
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetPendingApprovalsAsync()
        {
            var all = await _repository.GetAllAsync();
            return all.Where(r => !r.IsApproved).Select(MapToDto);
        }

        public async Task<RestaurantResponseDto> UpdateRestaurantAsync(Guid id, Guid ownerId, UpdateRestaurantDto dto)
        {
            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null || restaurant.OwnerId != ownerId)
                throw new UnauthorizedAccessException("Not authorized.");

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
            restaurant.ImageUrl = dto.ImageUrl;

            await _repository.UpdateAsync(restaurant);
            try { await _cache.RemoveAsync($"{CacheKeyPrefix}{id}"); } catch { }
            
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
            try { await _cache.RemoveAsync($"{CacheKeyPrefix}{id}"); } catch { }
        }

        public async Task UpdateRatingAsync(Guid id, double newRating)
        {
            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null) throw new Exception("Restaurant not found.");

            restaurant.AvgRating = newRating;
            await _repository.UpdateAsync(restaurant);
            try { await _cache.RemoveAsync($"{CacheKeyPrefix}{id}"); } catch { }
        }

        public async Task DeleteRestaurantAsync(Guid id)
        {
            var restaurant = await _repository.GetByIdAsync(id);
            if (restaurant == null) throw new Exception("Restaurant not found.");
            await _repository.DeleteAsync(restaurant);
            try { await _cache.RemoveAsync($"{CacheKeyPrefix}{id}"); } catch { }
        }

        public async Task<bool> IsDuplicateAsync(string name, string address, string? placeId = null)
        {
            if (!string.IsNullOrEmpty(placeId))
            {
                if (await _repository.ExistsByPlaceIdAsync(placeId)) return true;
            }
            return await _repository.ExistsByNameAndAddressAsync(name, address);
        }

        public async Task UpdateTravelTimeAsync(Guid restaurantId, int estimatedMinutes)
        {
            var restaurant = await _repository.GetByIdAsync(restaurantId);
            if (restaurant != null)
            {
                restaurant.EstimatedDeliveryMin = estimatedMinutes;
                await _repository.UpdateAsync(restaurant);
                try { await _cache.RemoveAsync($"{CacheKeyPrefix}{restaurantId}"); } catch { }
            }
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetAllRestaurantsAsync()
        {
            var results = await _repository.GetAllAsync();
            return results.Select(MapToDto);
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
            restaurant.ReviewCount = await _repository.GetReviewCountAsync(restaurantId);
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
                restaurant.ReviewCount = await _repository.GetReviewCountAsync(review.RestaurantId);
                await _repository.SaveChangesAsync();
                try { await _cache.RemoveAsync($"{CacheKeyPrefix}{review.RestaurantId}"); } catch { }
            }
        }

        private ReviewResponseDto MapToReviewEntityDto(RestaurantReview r) => new ReviewResponseDto(
            r.ReviewId, r.RestaurantId, r.OrderId, r.CustomerId, "Customer", r.FoodRating, r.Comment, r.ReviewDate
        );

        private RestaurantResponseDto MapToDto(Entities.Restaurant r) => new RestaurantResponseDto(
            r.RestaurantId, r.OwnerId, r.Name, r.Description, r.Cuisine, r.Address, r.City, r.Latitude, r.Longitude, r.Phone, r.AvgRating, r.IsOpen, r.IsApproved, r.DeliveryRadiusKm, r.MinOrderAmount, r.EstimatedDeliveryMin, r.ImageUrl, r.PlaceId, r.ReviewCount, r.CreatedAt
        );
    }
}
