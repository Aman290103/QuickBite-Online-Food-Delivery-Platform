using Microsoft.EntityFrameworkCore;
using QuickBite.Restaurant.Data;
using QuickBite.Restaurant.Entities;
using QuickBite.Restaurant.Interfaces;

namespace QuickBite.Restaurant.Repositories
{
    public class RestaurantRepository : IRestaurantRepository
    {
        private readonly RestaurantDbContext _context;

        public RestaurantRepository(RestaurantDbContext context)
        {
            _context = context;
        }

        public async Task<Entities.Restaurant?> GetByIdAsync(Guid id)
        {
            return await _context.Restaurants.FindAsync(id);
        }

        public async Task<IEnumerable<Entities.Restaurant>> GetAllAsync()
        {
            return await _context.Restaurants.ToListAsync();
        }

        public async Task<IEnumerable<Entities.Restaurant>> SearchByNameAsync(string name)
        {
            return await _context.Restaurants
                .Where(r => r.Name.Contains(name) && r.IsApproved)
                .ToListAsync();
        }

        public async Task<IEnumerable<Entities.Restaurant>> FilterByCuisineAsync(string cuisine)
        {
            return await _context.Restaurants
                .Where(r => r.Cuisine.Contains(cuisine) && r.IsApproved)
                .ToListAsync();
        }

        public async Task<IEnumerable<Entities.Restaurant>> FindNearbyAsync(double latitude, double longitude, double radiusKm)
        {
            // Note: This is an approximation using the Haversine formula components that translate to SQL
            // For production with massive data, Spatial indexes (PostGIS) are preferred.
            
            var restaurants = await _context.Restaurants
                .Where(r => r.IsApproved && r.IsOpen)
                .ToListAsync(); // Pulling into memory for complex Math calculation if SQL translation fails

            return restaurants.Where(r => 
                CalculateDistance(latitude, longitude, r.Latitude, r.Longitude) <= radiusKm)
                .OrderBy(r => CalculateDistance(latitude, longitude, r.Latitude, r.Longitude));
        }

        public async Task AddAsync(Entities.Restaurant restaurant)
        {
            await _context.Restaurants.AddAsync(restaurant);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Entities.Restaurant restaurant)
        {
            _context.Restaurants.Update(restaurant);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Entities.Restaurant restaurant)
        {
            _context.Restaurants.Remove(restaurant);
            await _context.SaveChangesAsync();
        }

        // --- Review Methods ---

        public async Task AddReviewAsync(RestaurantReview review)
        {
            await _context.Reviews.AddAsync(review);
        }

        public async Task<IEnumerable<RestaurantReview>> GetReviewsByRestaurantIdAsync(Guid restaurantId, int page, int pageSize)
        {
            return await _context.Reviews
                .Where(r => r.RestaurantId == restaurantId && r.IsVerified)
                .OrderByDescending(r => r.ReviewDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<bool> ExistsReviewByOrderIdAsync(Guid orderId)
        {
            return await _context.Reviews.AnyAsync(r => r.OrderId == orderId);
        }

        public async Task<double> GetAvgFoodRatingAsync(Guid restaurantId)
        {
            var ratings = await _context.Reviews
                .Where(r => r.RestaurantId == restaurantId && r.IsVerified)
                .Select(r => r.FoodRating)
                .ToListAsync();

            if (!ratings.Any()) return 0;
            return ratings.Average();
        }

        public async Task<RestaurantReview?> GetReviewByIdAsync(Guid reviewId)
        {
            return await _context.Reviews.FindAsync(reviewId);
        }

        public async Task DeleteReviewAsync(RestaurantReview review)
        {
            review.IsVerified = false; 
            _context.Reviews.Update(review);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle) => Math.PI * angle / 180.0;
    }
}
