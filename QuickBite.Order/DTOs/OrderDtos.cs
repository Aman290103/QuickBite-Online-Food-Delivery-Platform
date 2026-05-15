using QuickBite.Order.Entities;
using System.ComponentModel.DataAnnotations;

namespace QuickBite.Order.DTOs
{
    // [FEATURE: NOTIFICATIONS] - Data structure for incoming order requests
    // Added CustomerEmail and CustomerPhone to ensure contact info is available 
    // immediately after the order is submitted.
    public record PlaceOrderDto(
        [Required] string ModeOfPayment, 
        [Required] string DeliveryAddress,
        string? SpecialInstructions,
        string? PromoCode,
        string? CustomerEmail,
        string? CustomerPhone
    );

    public record OrderItemDto(
        Guid MenuItemId,
        string Name,
        decimal Price,
        int Quantity,
        string? Customization
    );

    public record OrderResponseDto(
        Guid Id,
        string RestaurantName,
        string OrderNumber,
        Guid RestaurantId,
        List<OrderItemDto> Items,
        decimal TotalAmount,
        string Status,
        DateTime CreatedAt,
        string DeliveryAddress,
        string? SpecialInstructions,
        Guid? AgentId
    );

    public record UpdateStatusDto(
        [Required] OrderStatus NewStatus
    );

    public record RestaurantStatsDto(
        decimal TodayRevenue,
        int ActiveOrdersCount,
        int TotalOrdersToday
    );
}
