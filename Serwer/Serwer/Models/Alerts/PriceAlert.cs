using Serwer.Models.Identity;

namespace Serwer.Models.Alerts
{
    public class PriceAlert
    {
        public int Id { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string CoinSymbol { get; set; } = string.Empty;
        public decimal TargetPrice { get; set; }
        public AlertDirection Direction { get; set; }
        public bool IsTriggered { get; set; }
        public DateTime? TriggeredAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Foreign key
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
