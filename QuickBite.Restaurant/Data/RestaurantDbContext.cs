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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Entities.Restaurant>(entity =>
            {
                entity.ToTable("Restaurants");
                entity.HasIndex(r => r.City);
                entity.HasIndex(r => r.Cuisine);
            });
        }
    }
}
