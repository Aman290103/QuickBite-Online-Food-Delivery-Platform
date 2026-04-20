using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using QuickBite.Notification.DTOs;
using QuickBite.Notification.Entities;
using QuickBite.Notification.Hubs;
using QuickBite.Notification.Interfaces;

namespace QuickBite.Notification.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _repository;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly EmailService _emailService;
        private readonly SmsService _smsService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository repository,
            IHubContext<NotificationHub> hubContext,
            EmailService emailService,
            SmsService smsService,
            IDistributedCache cache,
            ILogger<NotificationService> logger)
        {
            _repository = repository;
            _hubContext = hubContext;
            _emailService = emailService;
            _smsService = smsService;
            _cache = cache;
            _logger = logger;
        }

        public async Task SendAsync(SendNotificationDto dto)
        {
            var notification = new Entities.Notification
            {
                NotificationId = Guid.NewGuid(),
                RecipientId = dto.RecipientId,
                Type = dto.Type,
                Channel = dto.Channel,
                Title = dto.Title,
                Message = dto.Message,
                RelatedId = dto.RelatedId,
                RelatedType = dto.RelatedType,
                IsRead = false,
                IsAudio = dto.IsAudio
            };

            // 1. Save to DB
            await _repository.AddAsync(notification);
            await _repository.SaveChangesAsync();

            // 2. Increment Redis Counter
            await IncrementUnreadCount(dto.RecipientId);

            // 3. Route to Channel (Fire-and-forget)
            _ = RouteNotification(notification);
        }

        public async Task BroadcastAsync(BroadcastDto dto)
        {
            _logger.LogInformation("Broadcasting message: {Title}", dto.Title);
            
            // Broadcast to all connected users via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
            {
                dto.Title,
                dto.Message,
                Type = dto.Type.ToString(),
                SentAt = DateTime.UtcNow
            });
        }

        public async Task<IEnumerable<NotificationResponseDto>> GetUserNotificationsAsync(Guid userId)
        {
            var notifications = await _repository.GetByRecipientIdAsync(userId);
            return notifications.Select(MapToDto);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            var cacheKey = $"unread:{userId}";
            var countStr = await _cache.GetStringAsync(cacheKey);
            
            if (countStr != null) return int.Parse(countStr);

            // Cache miss: fall back to DB
            var dbCount = await _repository.CountUnreadAsync(userId);
            await _cache.SetStringAsync(cacheKey, dbCount.ToString());
            return dbCount;
        }

        public async Task MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await _repository.GetByIdAsync(notificationId);
            if (notification != null && notification.RecipientId == userId && !notification.IsRead)
            {
                notification.IsRead = true;
                await _repository.UpdateAsync(notification);
                await _repository.SaveChangesAsync();
                
                // Decrement Redis Counter
                await DecrementUnreadCount(userId);
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            await _repository.MarkAllAsReadAsync(userId);
            await _repository.SaveChangesAsync();
            await _cache.RemoveAsync($"unread:{userId}"); // Force refresh on next check
        }

        private async Task RouteNotification(Entities.Notification n)
        {
            switch (n.Channel)
            {
                case NotificationChannel.APP:
                    await _hubContext.Clients.Group(n.RecipientId.ToString()).SendAsync("ReceiveNotification", MapToDto(n));
                    break;
                case NotificationChannel.EMAIL:
                    // In real setup, you'd fetch recipient's email from User Service
                    // For now, logged in user's email is not accessible here easily without extra lookup
                    _logger.LogWarning("Email routing requested but not fully implemented (requires User Service lookup).");
                    break;
                case NotificationChannel.SMS:
                    _logger.LogWarning("SMS routing requested but not fully implemented (requires User Service lookup).");
                    break;
            }
        }

        private async Task IncrementUnreadCount(Guid userId)
        {
            var cacheKey = $"unread:{userId}";
            var current = await GetUnreadCountAsync(userId);
            await _cache.SetStringAsync(cacheKey, (current + 1).ToString());
        }

        private async Task DecrementUnreadCount(Guid userId)
        {
            var cacheKey = $"unread:{userId}";
            var current = await GetUnreadCountAsync(userId);
            var val = Math.Max(0, current - 1);
            await _cache.SetStringAsync(cacheKey, val.ToString());
        }

        private NotificationResponseDto MapToDto(Entities.Notification n) => new NotificationResponseDto(
            n.NotificationId, n.RecipientId, n.Type, n.Channel, n.Title, n.Message, n.RelatedId, n.RelatedType, n.IsRead, n.IsAudio, n.SentAt
        );
    }
}
