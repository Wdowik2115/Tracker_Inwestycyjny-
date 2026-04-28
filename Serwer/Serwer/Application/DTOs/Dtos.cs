namespace Investe.Application.DTOs
{
    public class WalletDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
    }

    public class TransactionCreateDto
    {
        public int WalletId { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PriceAtTime { get; set; }
        public string Type { get; set; } = "Buy"; 
        public string Notes { get; set; } = string.Empty;
    }

    public class AssetResponseDto
    {
        public int Id { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal TotalValue => Quantity * CurrentPrice;
        public decimal PnL => TotalValue - (Quantity * AverageBuyPrice);
    }
}
