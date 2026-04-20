using QuickBite.Notification.Entities;

namespace QuickBite.Notification.Interfaces
{
    public interface INotificationRepository
    {
        Task<Entities.Notification?> GetByIdAsync(Guid id);
        Task<IEnumerable<Entities.Notification>> GetByRecipientIdAsync(Guid recipientId, int count = 20);
        Task<int> CountUnreadAsync(Guid recipientId);
        
        Task AddAsync(Entities.Notification notification);
        Task UpdateAsync(Entities.Notification notification);
        Task DeleteAsync(Guid id);
        
        Task MarkAllAsReadAsync(Guid recipientId);
        Task SaveChangesAsync();
    }
}
