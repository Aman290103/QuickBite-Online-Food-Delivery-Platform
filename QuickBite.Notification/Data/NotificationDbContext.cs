using Microsoft.EntityFrameworkCore;
using QuickBite.Notification.Entities;

namespace QuickBite.Notification.Data
{
    public class NotificationDbContext : DbContext
    {
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
        {
        }

        public DbSet<Entities.Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entities.Notification>(entity =>
            {
                entity.HasKey(e => e.NotificationId);
                entity.HasIndex(e => e.RecipientId);
                entity.HasIndex(e => new { e.RecipientId, e.IsRead });
            });
        }
    }
}
