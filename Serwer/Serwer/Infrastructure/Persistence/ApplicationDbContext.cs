using Microsoft.EntityFrameworkCore;
using Investe.Domain.Entities;

namespace Investe.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Wallet> Wallets { get; set; } = null!;
        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<Transaction> Transactions { get; set; } = null!;
        public DbSet<PriceAlert> PriceAlerts { get; set; } = null!;
        public DbSet<PriceHistoryCache> PriceHistoryCache { get; set; } = null!;
        public DbSet<Report> Reports { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>(e =>
            {
                e.HasKey(u => u.Id);
                e.Property(u => u.Id).ValueGeneratedOnAdd();
                e.HasIndex(u => u.Email).IsUnique();
            });

            // Wallet
            modelBuilder.Entity<Wallet>(e =>
            {
                e.HasKey(w => w.Id);
                e.Property(w => w.Id).ValueGeneratedOnAdd();
                e.HasOne(w => w.User)
                    .WithMany(u => u.Wallets)
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasMany(w => w.Assets)
                    .WithOne(a => a.Wallet)
                    .HasForeignKey(a => a.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasMany(w => w.Transactions)
                    .WithOne(t => t.Wallet)
                    .HasForeignKey(t => t.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Asset
            modelBuilder.Entity<Asset>(e =>
            {
                e.HasKey(a => a.Id);
                e.Property(a => a.Id).ValueGeneratedOnAdd();
                e.Property(a => a.Quantity).HasPrecision(18, 8);
                e.Property(a => a.AverageBuyPrice).HasPrecision(18, 8);
            });

            // Transaction
            modelBuilder.Entity<Transaction>(e =>
            {
                e.HasKey(t => t.Id);
                e.Property(t => t.Id).ValueGeneratedOnAdd();
                e.Property(t => t.Quantity).HasPrecision(18, 8);
                e.Property(t => t.PriceAtTime).HasPrecision(18, 8);
                e.Property(t => t.TotalValue).HasPrecision(18, 8);
                e.Property(t => t.Fee).HasPrecision(18, 8);
                e.Property(t => t.CostBasisPerUnit).HasPrecision(18, 8);
            });

            // PriceAlert
            modelBuilder.Entity<PriceAlert>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.Property(p => p.TargetPrice).HasPrecision(18, 8);
                e.HasOne(p => p.User)
                    .WithMany(u => u.PriceAlerts)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // PriceHistoryCache
            modelBuilder.Entity<PriceHistoryCache>(e =>
            {
                e.HasKey(p => p.Id);
                e.Property(p => p.Id).ValueGeneratedOnAdd();
                e.HasIndex(p => new { p.CoinId, p.Date }).IsUnique();
                e.Property(p => p.PriceUsd).HasPrecision(18, 8);
            });

            // Report
            modelBuilder.Entity<Report>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).ValueGeneratedOnAdd();
                e.Property(r => r.Content).HasColumnType("varbinary(max)");
                e.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(r => r.Wallet)
                    .WithMany()
                    .HasForeignKey(r => r.WalletId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
