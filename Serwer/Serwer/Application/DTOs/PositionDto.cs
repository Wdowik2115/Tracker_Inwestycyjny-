namespace Investe.Application.DTOs
{
    public class PositionDto
    {
        public string Symbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal Quantity { get; set; }
        public decimal AvgCostBasis { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal Value { get; set; }
        public decimal Pnl { get; set; }
        public decimal PnlPercent { get; set; }
    }
}
