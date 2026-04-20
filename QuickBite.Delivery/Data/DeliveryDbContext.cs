using Microsoft.EntityFrameworkCore;
using QuickBite.Delivery.Entities;

namespace QuickBite.Delivery.Data
{
    public class DeliveryDbContext : DbContext
    {
        public DeliveryDbContext(DbContextOptions<DeliveryDbContext> options) : base(options)
        {
        }

        public DbSet<DeliveryAgent> DeliveryAgents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DeliveryAgent>(entity =>
            {
                entity.HasKey(e => e.AgentId);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.Property(e => e.AvgRating).HasPrecision(3, 2);
            });
        }
    }
}
