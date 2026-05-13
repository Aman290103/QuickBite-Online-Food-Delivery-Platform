using QuickBite.Order.Entities;
using System.ComponentModel.DataAnnotations;

namespace QuickBite.Order.DTOs
{
    public record PlaceOrderDto(
        [Required] string ModeOfPayment, // COD, ONLINE
        [Required] string DeliveryAddress,
        string? SpecialInstructions,
        string? PromoCode
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
