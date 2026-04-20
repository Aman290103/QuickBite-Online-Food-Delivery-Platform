using Microsoft.EntityFrameworkCore;
using QuickBite.Order.Entities;

namespace QuickBite.Order.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options)
        {
        }

        public DbSet<Entities.Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entities.Order>(entity =>
            {
                entity.ToTable("Orders");
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
                entity.Property(e => e.Discount).HasPrecision(18, 2);
                entity.Property(e => e.FinalAmount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");
                entity.Property(e => e.Price).HasPrecision(18, 2);
            });
        }
    }
}
