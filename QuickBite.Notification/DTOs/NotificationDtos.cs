using QuickBite.Notification.Entities;
using System.ComponentModel.DataAnnotations;

namespace QuickBite.Notification.DTOs
{
    public record SendNotificationDto(
        [Required] Guid RecipientId,
        [Required] NotificationType Type,
        [Required] NotificationChannel Channel,
        [Required] string Title,
        [Required] string Message,
        string? RelatedId = null,
        string? RelatedType = null,
        bool IsAudio = false
    );

    public record NotificationResponseDto(
        Guid NotificationId,
        Guid RecipientId,
        NotificationType Type,
        NotificationChannel Channel,
        string Title,
        string Message,
        string? RelatedId,
        string? RelatedType,
        bool IsRead,
        bool IsAudio,
        DateTime SentAt
    );

    public record BroadcastDto(
        [Required] string Title,
        [Required] string Message,
        NotificationType Type = NotificationType.PROMO
    );
}
