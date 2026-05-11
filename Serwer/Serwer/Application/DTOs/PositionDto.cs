namespace Investe.Application.DTOs
{
    public class PositionDto
    {
        public string Symbol { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal AvgCostBasis { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal ValueUsdt { get; set; }
        public decimal PnlUsdt { get; set; }
        public decimal PnlPercent { get; set; }
    }
}
