namespace Investe.Domain.Entities
{
    public class PriceHistoryCache
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CoinId { get; set; } = string.Empty;
        public DateTime Date { get; set; }      // UTC midnight — one row per coin per day
        public decimal PriceUsd { get; set; }
        public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    }
}
