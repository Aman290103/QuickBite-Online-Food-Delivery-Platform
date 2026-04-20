using Microsoft.EntityFrameworkCore;
using QuickBite.Payment.Entities;

namespace QuickBite.Payment.Data
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
        {
        }

        public DbSet<Entities.Payment> Payments { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletStatement> WalletStatements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Entities.Payment>(entity =>
            {
                entity.ToTable("Payments");
                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.ToTable("Wallets");
                entity.Property(e => e.Balance).HasPrecision(18, 2);
                entity.HasIndex(e => e.CustomerId).IsUnique();
            });

            modelBuilder.Entity<WalletStatement>(entity =>
            {
                entity.ToTable("WalletStatements");
                entity.Property(e => e.Amount).HasPrecision(18, 2);
            });
        }
    }
}
