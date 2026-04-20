using Microsoft.EntityFrameworkCore;
using QuickBite.Menu.Entities;

namespace QuickBite.Menu.Data
{
    public class MenuDbContext : DbContext
    {
        public MenuDbContext(DbContextOptions<MenuDbContext> options) : base(options)
        {
        }

        public DbSet<MenuCategory> Categories { get; set; }
        public DbSet<MenuItem> Items { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<MenuCategory>(entity =>
            {
                entity.ToTable("MenuCategories");
            });

            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.ToTable("MenuItems");
                entity.HasIndex(m => m.Name);
                entity.HasIndex(m => m.RestaurantId);
                
                // Explicitly set decimal precision for SQL Server
                entity.Property(m => m.Price).HasPrecision(18, 2);
                entity.Property(m => m.DiscountedPrice).HasPrecision(18, 2);
            });
        }
    }
}
