using QuickBite.Restaurant.DTOs;

namespace QuickBite.Restaurant.Interfaces
{
    public interface IRestaurantService
    {
        Task<RestaurantResponseDto> RegisterRestaurantAsync(Guid ownerId, RegisterRestaurantDto dto);
        Task<RestaurantResponseDto?> GetRestaurantByIdAsync(Guid id);
        Task<IEnumerable<RestaurantResponseDto>> GetNearbyRestaurantsAsync(double lat, double lon, double radius);
        Task<IEnumerable<RestaurantResponseDto>> SearchRestaurantsAsync(string name);
        Task<IEnumerable<RestaurantResponseDto>> GetRestaurantsByCuisineAsync(string cuisine);
        Task<RestaurantResponseDto> UpdateRestaurantAsync(Guid id, Guid ownerId, UpdateRestaurantDto dto);
        Task ApproveRestaurantAsync(Guid id);
        Task ToggleRestaurantStatusAsync(Guid id, Guid ownerId);
        Task UpdateRatingAsync(Guid id, double newRating);
        Task DeleteRestaurantAsync(Guid id);
    }
}
