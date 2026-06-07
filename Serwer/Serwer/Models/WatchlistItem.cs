using System;

namespace Investe.Domain.Entities
{
    public class WatchlistItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string CoinId { get; set; } = string.Empty; // e.g., "bitcoin"
        public string Symbol { get; set; } = string.Empty; // e.g., "BTC"
        public string? ImageUrl { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}
