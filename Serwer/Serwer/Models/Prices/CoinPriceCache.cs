namespace Serwer.Models.Prices
{
    public class CoinPriceCache
    {
        public int Id { get; set; }
        public string CoinId { get; set; } = string.Empty;
        public string CoinSymbol { get; set; } = string.Empty;
        public string CoinName { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public decimal PriceChange24h { get; set; }
        public decimal PriceChangePercent24h { get; set; }
        public decimal MarketCap { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
