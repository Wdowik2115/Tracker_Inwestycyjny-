using Serwer.Models.Portfolio;

namespace Serwer.Models.Transactions
{
    public class Transaction
    {
        public int Id { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string CoinSymbol { get; set; } = string.Empty;
        public TransactionType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal PriceAtTime { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime ExecutedAt { get; set; }
        public string Notes { get; set; } = string.Empty;

        // Foreign key
        public int WalletId { get; set; }
        public virtual Wallet Wallet { get; set; } = null!;
    }
}
