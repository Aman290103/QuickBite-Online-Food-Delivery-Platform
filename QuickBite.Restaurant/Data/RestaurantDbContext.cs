using Microsoft.EntityFrameworkCore;
using QuickBite.Restaurant.Entities;

namespace QuickBite.Restaurant.Data
{
    public class RestaurantDbContext : DbContext
    {
        public RestaurantDbContext(DbContextOptions<RestaurantDbContext> options) : base(options)
        {
        }

        public DbSet<Entities.Restaurant> Restaurants { get; set; }
        public DbSet<RestaurantReview> Reviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Entities.Restaurant>(entity =>
            {
                entity.ToTable("Restaurants");
                entity.HasIndex(r => r.City);
                entity.HasIndex(r => r.Cuisine);
                entity.Property(r => r.MinOrderAmount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<RestaurantReview>(entity =>
            {
                entity.ToTable("RestaurantReviews");
                entity.HasIndex(r => r.OrderId).IsUnique();
                entity.HasIndex(r => r.RestaurantId);
                entity.HasOne(r => r.Restaurant)
                      .WithMany(res => res.Reviews)
                      .HasForeignKey(r => r.RestaurantId);
            });
        }
    }
}
