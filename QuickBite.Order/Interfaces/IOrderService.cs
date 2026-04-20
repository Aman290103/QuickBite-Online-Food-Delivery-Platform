using QuickBite.Order.DTOs;
using QuickBite.Order.Entities;

namespace QuickBite.Order.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> PlaceOrderAsync(Guid customerId, PlaceOrderDto dto);
        Task<OrderResponseDto?> GetOrderByIdAsync(Guid orderId);
        Task<IEnumerable<OrderResponseDto>> GetCustomerHistoryAsync(Guid customerId);
        Task<IEnumerable<OrderResponseDto>> GetRestaurantOrdersAsync(Guid restaurantId);
        Task<IEnumerable<OrderResponseDto>> GetAllOrdersAsync();
        
        Task<OrderResponseDto> UpdateStatusAsync(Guid orderId, OrderStatus newStatus, string actorRole);
        Task<OrderResponseDto> CancelOrderAsync(Guid orderId, Guid customerId);
        Task<OrderResponseDto> ReorderAsync(Guid pastOrderId, Guid customerId);
        Task<OrderResponseDto> AssignAgentAsync(Guid orderId, Guid agentId);
    }
}
