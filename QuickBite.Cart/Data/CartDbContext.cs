using Microsoft.EntityFrameworkCore;
using QuickBite.Cart.Entities;

namespace QuickBite.Cart.Data
{
    public class CartDbContext : DbContext
    {
        public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
        {
        }

        public DbSet<Entities.Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<PromoCode> PromoCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            modelBuilder.Entity<Entities.Cart>(entity =>
            {
                entity.ToTable("Carts");
                entity.HasIndex(c => c.CustomerId);
                entity.HasMany(c => c.Items).WithOne(i => i.Cart).HasForeignKey(i => i.CartId);
            });

            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.ToTable("CartItems");
            });

            modelBuilder.Entity<PromoCode>(entity =>
            {
                entity.ToTable("PromoCodes");
                entity.HasIndex(p => p.Code).IsUnique();
            });
        }
    }
}
