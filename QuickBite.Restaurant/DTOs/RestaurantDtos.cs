using System.ComponentModel.DataAnnotations;

namespace QuickBite.Restaurant.DTOs
{
    public class RegisterRestaurantDto
    {
        public Guid Id { get; set; } // Optional ID for internal batch tracking
        [Required] public string Name { get; set; } = string.Empty;
        [Required] public string Description { get; set; } = string.Empty;
        [Required] public string Cuisine { get; set; } = string.Empty;
        [Required] public string Address { get; set; } = string.Empty;
        [Required] public string City { get; set; } = string.Empty;
        [Required] public double Latitude { get; set; }
        [Required] public double Longitude { get; set; }
        [Required] public string Phone { get; set; } = string.Empty;
        public double DeliveryRadiusKm { get; set; } = 5.0;
        public decimal MinOrderAmount { get; set; } = 0;
        public int EstimatedDeliveryMin { get; set; } = 30;
        public string? ImageUrl { get; set; }
        public string? PlaceId { get; set; }
        public float Rating { get; set; } = 4.0f;
    }

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
        int EstimatedDeliveryMin,
        string? ImageUrl
    );

    public record RestaurantResponseDto(
        Guid Id,
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
        string? ImageUrl,
        string? PlaceId,
        int ReviewCount,
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
