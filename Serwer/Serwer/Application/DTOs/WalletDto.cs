namespace Investe.Application.DTOs
{
    public class WalletDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal TotalValue { get; set; }
        public int AssetCount { get; set; }
        public decimal Pnl { get; set; }
        public decimal PnlPercent { get; set; }
        public List<string> SharedWithEmails { get; set; } = new();
    }
}
