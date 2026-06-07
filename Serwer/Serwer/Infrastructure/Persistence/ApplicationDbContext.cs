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
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<ChatMessage> ChatMessages { get; set; } = null!;
        public DbSet<WatchlistItem> Watchlist { get; set; } = null!;

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

            // ChatMessage
            modelBuilder.Entity<ChatMessage>(e =>
            {
                e.HasKey(cm => cm.Id);
                e.Property(cm => cm.Id).ValueGeneratedOnAdd();
                e.HasOne(cm => cm.User)
                    .WithMany(u => u.ChatMessages)
                    .HasForeignKey(cm => cm.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
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
                e.HasMany(w => w.SharedWith)
                    .WithMany(u => u.SharedWallets)
                    .UsingEntity<Dictionary<string, object>>(
                        "UserWallet",
                        j => j.HasOne<User>().WithMany()
                            .HasForeignKey("SharedWithId")
                            .OnDelete(DeleteBehavior.Cascade),
                        j => j.HasOne<Wallet>().WithMany()
                            .HasForeignKey("SharedWalletsId")
                            .OnDelete(DeleteBehavior.NoAction),
                        j => j.ToTable("WalletShares")
                    );
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

            // Report
            modelBuilder.Entity<Report>(e =>
            {
                e.HasKey(r => r.Id);
                e.Property(r => r.Id).ValueGeneratedOnAdd();
                e.HasOne(r => r.User)
                    .WithMany()
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                e.HasOne(r => r.Wallet)
                    .WithMany()
                    .HasForeignKey(r => r.WalletId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // Watchlist
            modelBuilder.Entity<WatchlistItem>(e =>
            {
                e.HasKey(w => w.Id);
                e.Property(w => w.Id).ValueGeneratedOnAdd();
                e.HasOne(w => w.User)
                    .WithMany(u => u.WatchlistItems)
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
