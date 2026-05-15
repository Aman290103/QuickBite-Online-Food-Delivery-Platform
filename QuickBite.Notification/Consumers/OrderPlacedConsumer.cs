using MassTransit;
using QuickBite.Notification.DTOs;
using QuickBite.Notification.Entities;
using QuickBite.Notification.Interfaces;

namespace QuickBite.Notification.Consumers
{
    // Define the event structure to match the Order Service's broadcast
    public record OrderPlacedEvent(
        Guid OrderId, 
        Guid CustomerId, 
        Guid RestaurantId, 
        decimal TotalAmount,
        string CustomerEmail,
        string CustomerPhone
    );

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

            // [FEATURE: NOTIFICATIONS] - Email Dispatch
            // Sends a branded HTML receipt to the customer's verified email.
            if (!string.IsNullOrEmpty(message.CustomerEmail))
            {
                await _notificationService.SendAsync(new SendNotificationDto(
                    message.CustomerId,
                    NotificationType.ORDER,
                    NotificationChannel.EMAIL,
                    "Order Confirmed!",
                    $"Your order #{message.OrderId} for ₹{message.TotalAmount} has been received and is being prepared.",
                    message.OrderId.ToString(),
                    "ORDER",
                    RecipientContact: message.CustomerEmail
                ));
            }

            // [FEATURE: NOTIFICATIONS] - SMS Dispatch
            // Sends a real-time tracking link to the customer's phone via Twilio.
            if (!string.IsNullOrEmpty(message.CustomerPhone))
            {
                await _notificationService.SendAsync(new SendNotificationDto(
                    message.CustomerId,
                    NotificationType.ORDER,
                    NotificationChannel.SMS,
                    "QuickBite Update",
                    $"Order confirmed! Tracking: http://quickbite.com/track/{message.OrderId}",
                    message.OrderId.ToString(),
                    "ORDER",
                    RecipientContact: message.CustomerPhone
                ));
            }

            // 4. Notify Restaurant Owner (In-App + Audio Alert)
            await _notificationService.SendAsync(new SendNotificationDto(
                message.RestaurantId,
                NotificationType.ORDER,
                NotificationChannel.APP,
                "New Order Recieved!",
                $"You have a new order worth ₹{message.TotalAmount}.",
                message.OrderId.ToString(),
                "ORDER",
                IsAudio: true
            ));

            _logger.LogInformation("Notifications sent for Order {OrderId}", message.OrderId);
        }
    }
}
