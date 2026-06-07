namespace Investe.Application.DTOs
{
    public class PortfolioSummaryDto
    {
        public List<PositionDto> Positions { get; set; } = new();
        public decimal TotalValue { get; set; }
        public decimal TotalPnl { get; set; }
        public decimal TotalInvested { get; set; }
    }
}
