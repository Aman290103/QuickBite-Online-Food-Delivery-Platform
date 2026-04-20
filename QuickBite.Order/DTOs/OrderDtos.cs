using QuickBite.Order.Entities;
using System.ComponentModel.DataAnnotations;

namespace QuickBite.Order.DTOs
{
    public record PlaceOrderDto(
        [Required] string ModeOfPayment, // COD, ONLINE
        [Required] string DeliveryAddress,
        string? SpecialInstructions
    );

    public record OrderItemDto(
        Guid MenuItemId,
        string Name,
        decimal Price,
        int Quantity,
        string? Customization
    );

    public record OrderResponseDto(
        Guid OrderId,
        Guid CustomerId,
        Guid RestaurantId,
        Guid? DeliveryAgentId,
        decimal TotalAmount,
        decimal Discount,
        decimal FinalAmount,
        string ModeOfPayment,
        OrderStatus Status,
        DateTime OrderDate,
        DateTime? EstimatedDelivery,
        string DeliveryAddress,
        string? SpecialInstructions,
        List<OrderItemDto> Items
    );

    public record UpdateStatusDto(
        [Required] OrderStatus NewStatus
    );
}
