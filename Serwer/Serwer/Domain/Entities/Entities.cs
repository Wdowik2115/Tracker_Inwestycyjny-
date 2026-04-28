namespace Investe.Domain.Entities
{
    public enum TransactionType
    {
        Buy,
        Sell
    }

    public enum AlertDirection
    {
        Above,
        Below
    }

    public class Wallet
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public virtual ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public class Asset
    {
        public int Id { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public int WalletId { get; set; }
        public virtual Wallet Wallet { get; set; } = null!;
    }

    public class Transaction
    {
        public int Id { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal PriceAtTime { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
        public int WalletId { get; set; }
        public virtual Wallet Wallet { get; set; } = null!;
    }

    public class PriceAlert
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal TargetPrice { get; set; }
        public AlertDirection Direction { get; set; }
        public bool IsTriggered { get; set; }
        public DateTime? TriggeredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
