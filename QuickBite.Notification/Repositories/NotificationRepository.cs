using Microsoft.EntityFrameworkCore;
using QuickBite.Notification.Data;
using QuickBite.Notification.Entities;
using QuickBite.Notification.Interfaces;

namespace QuickBite.Notification.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly NotificationDbContext _context;

        public NotificationRepository(NotificationDbContext context)
        {
            _context = context;
        }

        public async Task<Entities.Notification?> GetByIdAsync(Guid id) => await _context.Notifications.FindAsync(id);

        public async Task<IEnumerable<Entities.Notification>> GetByRecipientIdAsync(Guid recipientId, int count = 20)
        {
            return await _context.Notifications
                .Where(n => n.RecipientId == recipientId)
                .OrderByDescending(n => n.SentAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> CountUnreadAsync(Guid recipientId)
        {
            return await _context.Notifications
                .CountAsync(n => n.RecipientId == recipientId && !n.IsRead);
        }

        public async Task AddAsync(Entities.Notification notification) => await _context.Notifications.AddAsync(notification);

        public async Task UpdateAsync(Entities.Notification notification)
        {
            _context.Notifications.Update(notification);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(Guid id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null) _context.Notifications.Remove(notification);
        }

        public async Task MarkAllAsReadAsync(Guid recipientId)
        {
            var unread = await _context.Notifications
                .Where(n => n.RecipientId == recipientId && !n.IsRead)
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
        }

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
