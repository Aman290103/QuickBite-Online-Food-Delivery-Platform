using MassTransit;
using QuickBite.Notification.DTOs;
using QuickBite.Notification.Entities;
using QuickBite.Notification.Interfaces;

namespace QuickBite.Notification.Consumers
{
    // Define the event structure to match the Order Service's broadcast
    public record OrderPlacedEvent(Guid OrderId, Guid CustomerId, Guid RestaurantId, decimal TotalAmount);

    public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<OrderPlacedConsumer> _logger;

        public OrderPlacedConsumer(INotificationService notificationService, ILogger<OrderPlacedConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
        {
            var message = context.Message;
            _logger.LogInformation("Processing OrderPlacedEvent for Order {OrderId}", message.OrderId);

            // 1. Notify Customer (In-App)
            await _notificationService.SendAsync(new SendNotificationDto(
                message.CustomerId,
                NotificationType.ORDER,
                NotificationChannel.APP,
                "Order Placed Success!",
                $"Your order for ₹{message.TotalAmount} has been placed successfully.",
                message.OrderId.ToString(),
                "ORDER"
            ));

            // 2. Notify Restaurant Owner (In-App + Audio Alert)
            // Note: Currently RestaurantId is used as RecipientId for the owner group
            await _notificationService.SendAsync(new SendNotificationDto(
                message.RestaurantId,
                NotificationType.ORDER,
                NotificationChannel.APP,
                "New Order Recieved!",
                $"You have a new order worth ₹{message.TotalAmount}.",
                message.OrderId.ToString(),
                "ORDER",
                IsAudio: true // Triggers audio alert in dashboard
            ));

            _logger.LogInformation("Notifications sent for Order {OrderId}", message.OrderId);
        }
    }
}
