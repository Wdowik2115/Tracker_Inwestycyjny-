using System.Text.Json;
using Investe.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class CoinPriceService : ICoinPriceService
    {
        private static readonly Dictionary<string, string> SymbolToId = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BTC"]   = "bitcoin",
            ["ETH"]   = "ethereum",
            ["SOL"]   = "solana",
            ["BNB"]   = "binancecoin",
            ["USDT"]  = "tether",
            ["USDC"]  = "usd-coin",
            ["ADA"]   = "cardano",
            ["DOT"]   = "polkadot",
            ["AVAX"]  = "avalanche-2",
            ["MATIC"] = "matic-network",
            ["LINK"]  = "chainlink",
            ["XRP"]   = "ripple"
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CoinPriceService> _logger;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

        public CoinPriceService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<CoinPriceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>Returns the current USD price for a coin symbol. Returns 0 on failure.</summary>
        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            var cacheKey = $"price:{symbol.ToUpperInvariant()}";
            if (_cache.TryGetValue(cacheKey, out decimal cached))
                return cached;

            if (!SymbolToId.TryGetValue(symbol, out var coinId))
            {
                _logger.LogWarning("Unknown symbol {Symbol} — no CoinGecko mapping", symbol);
                return 0m;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync(
                    $"simple/price?ids={coinId}&vs_currencies=usd");

                using var doc = JsonDocument.Parse(response);
                var price = doc.RootElement
                    .GetProperty(coinId)
                    .GetProperty("usd")
                    .GetDecimal();

                _cache.Set(cacheKey, price, CacheTtl);
                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch current price for {Symbol}", symbol);
                return 0m;
            }
        }

        /// <summary>Returns the historical USD price for a coin symbol on the given UTC date. Returns 0 on failure.</summary>
        public async Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime date)
        {
            if (!SymbolToId.TryGetValue(symbol, out var coinId))
            {
                _logger.LogWarning("Unknown symbol {Symbol} — no CoinGecko mapping", symbol);
                return 0m;
            }

            try
            {
                var dateStr = date.ToUniversalTime().ToString("dd-MM-yyyy");
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync(
                    $"coins/{coinId}/history?date={dateStr}");

                using var doc = JsonDocument.Parse(response);
                var price = doc.RootElement
                    .GetProperty("market_data")
                    .GetProperty("current_price")
                    .GetProperty("usd")
                    .GetDecimal();

                return price;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch historical price for {Symbol} on {Date}", symbol, date);
                return 0m;
            }
        }
    }
}
