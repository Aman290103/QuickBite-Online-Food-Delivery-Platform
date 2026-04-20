using QuickBite.Notification.DTOs;

namespace QuickBite.Notification.Interfaces
{
    public interface INotificationService
    {
        Task SendAsync(SendNotificationDto dto);
        Task BroadcastAsync(BroadcastDto dto);
        
        Task<IEnumerable<NotificationResponseDto>> GetUserNotificationsAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        
        Task MarkAsReadAsync(Guid userId, Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
    }
}
