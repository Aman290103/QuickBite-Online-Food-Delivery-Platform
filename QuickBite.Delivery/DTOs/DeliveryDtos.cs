using QuickBite.Delivery.Entities;
using System.ComponentModel.DataAnnotations;

namespace QuickBite.Delivery.DTOs
{
    public record RegisterAgentDto(
        [Required] string FullName,
        [Required] string Phone,
        [Required] VehicleType VehicleType,
        [Required] string VehicleNumber
    );

    public record UpdateLocationDto(
        [Required] double Latitude,
        [Required] double Longitude,
        Guid? OrderId // Optional: associated order for SignalR group targeting
    );

    public record AgentResponseDto(
        Guid AgentId,
        Guid UserId,
        string FullName,
        string Phone,
        VehicleType VehicleType,
        string VehicleNumber,
        bool IsAvailable,
        bool IsVerified,
        decimal AvgRating,
        int TotalDeliveries,
        double? CurrentLatitude,
        double? CurrentLongitude,
        double? DistanceKm = null
    );

    public record ToggleAvailabilityDto(bool IsAvailable);
}
