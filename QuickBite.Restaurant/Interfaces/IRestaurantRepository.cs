using QuickBite.Restaurant.Entities;

namespace QuickBite.Restaurant.Interfaces
{
    public interface IRestaurantRepository
    {
        Task<Entities.Restaurant?> GetByIdAsync(Guid id);
        Task<IEnumerable<Entities.Restaurant>> GetAllAsync();
        Task<IEnumerable<Entities.Restaurant>> SearchByNameAsync(string name);
        Task<IEnumerable<Entities.Restaurant>> FilterByCuisineAsync(string cuisine);
        Task<IEnumerable<Entities.Restaurant>> FindNearbyAsync(double latitude, double longitude, double radiusKm);
        Task AddAsync(Entities.Restaurant restaurant);
        Task UpdateAsync(Entities.Restaurant restaurant);
        Task DeleteAsync(Entities.Restaurant restaurant);
    }
}
