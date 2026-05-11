namespace Investe.Domain.Entities
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal PriceAtTime { get; set; }
        public decimal TotalValue { get; set; }
        public decimal? CostBasisPerUnit { get; set; }
        public string? CostBasisSource { get; set; }
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
        public Guid WalletId { get; set; }
        public virtual Wallet Wallet { get; set; } = null!;
    }
}
