using QuickBite.Restaurant.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickBite.Restaurant.Interfaces
{
    public record GooglePlacesResult(IEnumerable<RegisterRestaurantDto> Restaurants, string Status, string ErrorMessage = "");

    public interface IGooglePlacesService
    {
        Task<GooglePlacesResult> GetNearbyRestaurantsAsync(double lat, double lon, double radius);
        Task EnrichWithTravelTimesAsync(double originLat, double originLon, List<RegisterRestaurantDto> restaurants);
    }
}
