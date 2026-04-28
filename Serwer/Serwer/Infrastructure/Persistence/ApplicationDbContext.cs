using Microsoft.EntityFrameworkCore;
using Investe.Domain.Entities;

namespace Investe.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Wallet> Wallets { get; set; } = null!;
        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<PriceAlert> PriceAlerts { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Wallet>()
                .HasMany(w => w.Assets)
                .WithOne(a => a.Wallet)
                .HasForeignKey(a => a.WalletId);

            modelBuilder.Entity<Wallet>()
                .HasMany(w => w.Transactions)
                .WithOne(t => t.Wallet)
                .HasForeignKey(t => t.WalletId);

            modelBuilder.Entity<Asset>()
                .Property(a => a.Quantity)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Asset>()
                .Property(a => a.AverageBuyPrice)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.Quantity)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.PriceAtTime)
                .HasPrecision(18, 8);

            modelBuilder.Entity<Transaction>()
                .Property(t => t.TotalValue)
                .HasPrecision(18, 8);

            modelBuilder.Entity<PriceAlert>()
                .Property(p => p.TargetPrice)
                .HasPrecision(18, 8);
        }
    }
}
