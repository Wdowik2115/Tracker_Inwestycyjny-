namespace Investe.Application.DTOs
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public Guid WalletId { get; set; }
        public string WalletName { get; set; } = string.Empty;
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal PriceAtTime { get; set; }
        public decimal TotalValue { get; set; }
        public decimal Fee { get; set; }
        public string FeeCurrency { get; set; } = string.Empty;
        public decimal? CostBasisPerUnit { get; set; }
        public string? CostBasisSource { get; set; }
        public DateOnly ExecutedAt { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
    }
}
