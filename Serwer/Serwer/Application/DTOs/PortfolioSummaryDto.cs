namespace Investe.Application.DTOs
{
    public class PortfolioSummaryDto
    {
        public List<PositionDto> Positions { get; set; } = new();
        public decimal TotalValueUsdt { get; set; }
        public decimal TotalPnlUsdt { get; set; }
    }
}
