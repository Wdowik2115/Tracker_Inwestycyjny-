using System;

namespace Investe.Application.DTOs
{
    public class WatchlistItemDto
    {
        public Guid Id { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public DateTime AddedAt { get; set; }
    }

    public class AddToWatchlistDto
    {
        public string CoinId { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }
}
