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

        /// <summary>Returns the image URL for a coin from CoinGecko. Returns empty string on failure.</summary>
        public async Task<string> GetCoinImageUrlAsync(string coinId)
        {
            if (string.IsNullOrWhiteSpace(coinId))
                return string.Empty;

            var cacheKey = $"image:{coinId.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out string cached))
                return cached;

            try
            {
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync($"coins/{coinId.ToLowerInvariant()}");

                using var doc = JsonDocument.Parse(response);
                var imageUrl = doc.RootElement
                    .GetProperty("image")
                    .GetProperty("large")
                    .GetString() ?? string.Empty;

                _cache.Set(cacheKey, imageUrl, CacheTtl);
                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch image for {CoinId}", coinId);
                return string.Empty;
            }
        }
    }
}
