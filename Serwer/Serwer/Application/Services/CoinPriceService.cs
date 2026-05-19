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

        /// <summary>Returns current USD prices for multiple symbols in a single CoinGecko API call.</summary>
        public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> symbols)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var symbolList = symbols.Select(s => s.ToUpperInvariant()).Distinct().ToList();

            // Separate symbols that are already cached from those that need an API call
            var uncachedIds = new List<(string symbol, string coinId)>();
            foreach (var symbol in symbolList)
            {
                var cacheKey = $"price:{symbol}";
                if (_cache.TryGetValue(cacheKey, out decimal cached))
                {
                    result[symbol] = cached;
                }
                else if (SymbolToId.TryGetValue(symbol, out var coinId))
                {
                    uncachedIds.Add((symbol, coinId));
                }
                else
                {
                    _logger.LogWarning("Unknown symbol {Symbol} — no CoinGecko mapping", symbol);
                    result[symbol] = 0m;
                }
            }

            if (uncachedIds.Count == 0)
                return result;

            try
            {
                var ids = string.Join(",", uncachedIds.Select(x => x.coinId));
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync($"simple/price?ids={ids}&vs_currencies=usd");

                using var doc = JsonDocument.Parse(response);
                foreach (var (symbol, coinId) in uncachedIds)
                {
                    if (doc.RootElement.TryGetProperty(coinId, out var coinEl) &&
                        coinEl.TryGetProperty("usd", out var usdEl))
                    {
                        var price = usdEl.GetDecimal();
                        _cache.Set($"price:{symbol}", price, CacheTtl);
                        result[symbol] = price;
                    }
                    else
                    {
                        result[symbol] = 0m;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch batch prices for {Symbols}", string.Join(",", uncachedIds.Select(x => x.symbol)));
                foreach (var (symbol, _) in uncachedIds)
                    result.TryAdd(symbol, 0m);
            }

            return result;
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
