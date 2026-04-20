namespace QuickBite.Notification.Entities
{
    public enum NotificationType
    {
        ORDER,
        PAYMENT,
        PROMO,
        DELIVERY
    }

    public enum NotificationChannel
    {
        APP,
        EMAIL,
        SMS
    }

    public class Notification
    {
        public Guid NotificationId { get; set; }
        public Guid RecipientId { get; set; }
        
        public NotificationType Type { get; set; }
        public NotificationChannel Channel { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public string? RelatedId { get; set; }
        public string? RelatedType { get; set; } // "ORDER", "PAYMENT" etc

        public bool IsRead { get; set; } = false;
        public bool IsAudio { get; set; } = false; // For restaurant owner alerts

        public DateTime SentAt { get; set; } = DateTime.UtcNow;
    }
}
