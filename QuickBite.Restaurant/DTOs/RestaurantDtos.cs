using System.ComponentModel.DataAnnotations;

namespace QuickBite.Restaurant.DTOs
{
    public record RegisterRestaurantDto(
        [Required] string Name,
        [Required] string Description,
        [Required] string Cuisine,
        [Required] string Address,
        [Required] string City,
        [Required] double Latitude,
        [Required] double Longitude,
        [Required] string Phone,
        double DeliveryRadiusKm,
        decimal MinOrderAmount,
        int EstimatedDeliveryMin
    );

    public record UpdateRestaurantDto(
        string Name,
        string Description,
        string Cuisine,
        string Address,
        string City,
        double Latitude,
        double Longitude,
        string Phone,
        double DeliveryRadiusKm,
        decimal MinOrderAmount,
        int EstimatedDeliveryMin
    );

    public record RestaurantResponseDto(
        Guid RestaurantId,
        Guid OwnerId,
        string Name,
        string Description,
        string Cuisine,
        string Address,
        string City,
        double Latitude,
        double Longitude,
        string Phone,
        double AvgRating,
        bool IsOpen,
        bool IsApproved,
        double DeliveryRadiusKm,
        decimal MinOrderAmount,
        int EstimatedDeliveryMin,
        DateTime CreatedAt
    );

    public record NearbySearchDto(
        [Required] double Latitude,
        [Required] double Longitude,
        double RadiusKm = 5.0
    );

    public record RatingUpdateDto(
        Guid RestaurantId,
        double NewAvgRating
    );
}
