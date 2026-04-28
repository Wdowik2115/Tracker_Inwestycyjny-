namespace Serwer.Models.Portfolio
{
    public class Holding
    {
        public int Id { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string CoinSymbol { get; set; } = string.Empty;
        public string CoinName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageBuyPrice { get; set; }

        // Foreign key
        public int WalletId { get; set; }
        public virtual Wallet Wallet { get; set; } = null!;
    }
}
