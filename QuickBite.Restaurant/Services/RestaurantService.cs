using Microsoft.Extensions.Caching.Distributed;
using QuickBite.Restaurant.DTOs;
using QuickBite.Restaurant.Entities;
using QuickBite.Restaurant.Interfaces;
using System.Text.Json;

namespace QuickBite.Restaurant.Services
{
    // [SERVICE: RESTAURANT MANAGEMENT]
    // Manages restaurant lifecycle and reviews.
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
                ImageUrl = dto.ImageUrl,
                PlaceId = dto.PlaceId,
                AvgRating = dto.Rating > 0 ? (double)dto.Rating : 0.0, 
                IsApproved = false,
                IsOpen = true
            };
            await _repository.AddAsync(restaurant);
            await _repository.SaveChangesAsync();
            return MapToDto(restaurant);
        }

        public async Task<RestaurantResponseDto?> GetRestaurantByIdAsync(Guid id)
        {
            var r = await _repository.GetByIdAsync(id);
            return r == null ? null : MapToDto(r);
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetNearbyRestaurantsAsync(double lat, double lon, double radius)
        {
            var results = await _repository.FindNearbyAsync(lat, lon, radius);
            return results.Where(r => r.IsApproved).Select(MapToDto);
        }

        public async Task<IEnumerable<RestaurantResponseDto>> SearchRestaurantsAsync(string name)
        {
            var results = await _repository.SearchByNameAsync(name);
            return results.Where(r => r.IsApproved).Select(MapToDto);
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetRestaurantsByCuisineAsync(string cuisine)
        {
            var results = await _repository.GetAllAsync();
            return results.Where(r => r.Cuisine.Contains(cuisine, StringComparison.OrdinalIgnoreCase) && r.IsApproved).Select(MapToDto);
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetPendingApprovalsAsync()
        {
            var results = await _repository.GetAllAsync();
            return results.Where(r => !r.IsApproved).Select(MapToDto);
        }

        public async Task<RestaurantResponseDto> UpdateRestaurantAsync(Guid id, Guid ownerId, UpdateRestaurantDto dto)
        {
            var r = await _repository.GetByIdAsync(id);
            if (r == null || r.OwnerId != ownerId) throw new Exception("Unauthorized.");
            r.Name = dto.Name;
            r.Description = dto.Description;
            r.Cuisine = dto.Cuisine;
            r.Address = dto.Address;
            r.ImageUrl = dto.ImageUrl;
            await _repository.UpdateAsync(r);
            await _repository.SaveChangesAsync();
            return MapToDto(r);
        }

        public async Task ApproveRestaurantAsync(Guid id)
        {
            var r = await _repository.GetByIdAsync(id);
            if (r != null) { r.IsApproved = true; await _repository.UpdateAsync(r); await _repository.SaveChangesAsync(); }
        }

        public async Task ToggleRestaurantStatusAsync(Guid id, Guid ownerId)
        {
            var r = await _repository.GetByIdAsync(id);
            if (r != null && r.OwnerId == ownerId) { r.IsOpen = !r.IsOpen; await _repository.UpdateAsync(r); await _repository.SaveChangesAsync(); }
        }

        public async Task UpdateRatingAsync(Guid id, double newRating)
        {
            var r = await _repository.GetByIdAsync(id);
            if (r != null) { r.AvgRating = newRating; await _repository.UpdateAsync(r); await _repository.SaveChangesAsync(); }
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetRestaurantsByOwnerAsync(Guid ownerId)
        {
            var results = await _repository.GetAllAsync();
            return results.Where(r => r.OwnerId == ownerId).Select(MapToDto);
        }

        public async Task DeleteRestaurantAsync(Guid id)
        {
            var r = await _repository.GetByIdAsync(id);
            if (r != null) { await _repository.DeleteAsync(r); await _repository.SaveChangesAsync(); }
        }

        public async Task<bool> IsDuplicateAsync(string name, string address, string? placeId = null)
        {
            if (!string.IsNullOrEmpty(placeId)) {
                var all = await _repository.GetAllAsync();
                return all.Any(r => r.PlaceId == placeId);
            }
            return await _repository.ExistsByNameAndAddressAsync(name, address);
        }

        public async Task UpdateTravelTimeAsync(Guid restaurantId, int estimatedMinutes)
        {
            var r = await _repository.GetByIdAsync(restaurantId);
            if (r != null) { r.EstimatedDeliveryMin = estimatedMinutes; await _repository.UpdateAsync(r); await _repository.SaveChangesAsync(); }
        }

        public async Task<IEnumerable<RestaurantResponseDto>> GetAllRestaurantsAsync()
        {
            var results = await _repository.GetAllAsync();
            return results.Select(MapToDto);
        }

        public async Task<ReviewResponseDto> SubmitReviewAsync(Guid restaurantId, Guid customerId, AddReviewDto dto)
        {
            var review = new RestaurantReview {
                ReviewId = Guid.NewGuid(),
                RestaurantId = restaurantId,
                CustomerId = customerId,
                OrderId = dto.OrderId,
                FoodRating = dto.FoodRating,
                Comment = dto.Comment
            };
            await _repository.AddReviewAsync(review);
            await _repository.SaveChangesAsync();
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
            if (review != null) { await _repository.DeleteReviewAsync(review); await _repository.SaveChangesAsync(); }
        }

        private RestaurantResponseDto MapToDto(Entities.Restaurant r) => new RestaurantResponseDto(
            r.RestaurantId, r.OwnerId, r.Name, r.Description, r.Cuisine, r.Address, r.City, r.Latitude, r.Longitude, r.Phone, r.AvgRating, r.IsOpen, r.IsApproved, r.DeliveryRadiusKm, r.MinOrderAmount, r.EstimatedDeliveryMin, r.ImageUrl, r.PlaceId, r.ReviewCount, r.CreatedAt
        );

        private ReviewResponseDto MapToReviewEntityDto(RestaurantReview r) => new ReviewResponseDto(
            r.ReviewId, r.RestaurantId, r.OrderId, r.CustomerId, "Customer", r.FoodRating, r.Comment, r.ReviewDate
        );
    }
}
