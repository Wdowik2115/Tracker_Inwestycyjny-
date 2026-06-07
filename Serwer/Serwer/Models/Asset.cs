namespace Investe.Domain.Entities
{
    public class Asset
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public string? ImageUrl { get; set; }
        public Guid WalletId { get; set; }
        public virtual Wallet Wallet { get; set; } = null!;
    }
}
