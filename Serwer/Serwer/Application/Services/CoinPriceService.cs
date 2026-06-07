using System.Text.Json;
using Investe.Application.DTOs;
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
            ["XRP"]   = "ripple",
            ["DOGE"]  = "dogecoin",
            ["SHIB"]  = "shiba-inu",
            ["DAI"]   = "dai",
            ["LTC"]   = "litecoin",
            ["BCH"]   = "bitcoin-cash",
            ["ATOM"]  = "cosmos",
            ["UNI"]   = "uniswap",
            ["NEAR"]  = "near",
            ["POL"]   = "polygon-ecosystem-token",
            ["PEPE"]  = "pepe",
            ["WIF"]   = "dogwifhat"
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CoinPriceService> _logger;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

        public CoinPriceService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<CoinPriceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<decimal> GetCurrentPriceAsync(string symbol)
        {
            var cacheKey = $"price:{symbol.ToUpperInvariant()}";
            if (_cache.TryGetValue(cacheKey, out decimal cached))
                return cached;

            if (!SymbolToId.TryGetValue(symbol, out var coinId)) return 0m;

            try
            {
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync($"simple/price?ids={coinId}&vs_currencies=usd");
                using var doc = JsonDocument.Parse(response);
                var price = doc.RootElement.GetProperty(coinId).GetProperty("usd").GetDecimal();
                _cache.Set(cacheKey, price, CacheTtl);
                return price;
            }
            catch { return 0m; }
        }

        public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> symbols)
        {
            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in symbols) result[s] = await GetCurrentPriceAsync(s);
            return result;
        }

        public async Task<IEnumerable<CoinMarketDataDto>> GetTopMoversAsync(int count = 10, bool ascending = false)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync($"coins/markets?vs_currency=usd&order=market_cap_desc&per_page=100&page=1&sparkline=false&price_change_percentage=24h");
                using var doc = JsonDocument.Parse(response);

                var coins = doc.RootElement.EnumerateArray()
                    .Select(c => new CoinMarketDataDto
                    {
                        Symbol = c.GetProperty("symbol").GetString()?.ToUpper() ?? "",
                        Name = c.GetProperty("name").GetString() ?? "",
                        CurrentPrice = c.GetProperty("current_price").GetDecimal(),
                        PriceChangePercentage24h = c.TryGetProperty("price_change_percentage_24h", out var p) ? p.GetDecimal() : 0,
                        MarketCap = c.GetProperty("market_cap").GetDecimal()
                    })
                    .ToList();

                return ascending
                    ? coins.OrderBy(c => c.PriceChangePercentage24h).Take(count)
                    : coins.OrderByDescending(c => c.PriceChangePercentage24h).Take(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching top movers from CoinGecko");
                return Enumerable.Empty<CoinMarketDataDto>();
            }
        }

        public async Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime date)
        {
            if (!SymbolToId.TryGetValue(symbol, out var coinId)) return 0m;
            try
            {
                var dateStr = date.ToUniversalTime().ToString("dd-MM-yyyy");
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync($"coins/{coinId}/history?date={dateStr}");
                using var doc = JsonDocument.Parse(response);
                return doc.RootElement.GetProperty("market_data").GetProperty("current_price").GetProperty("usd").GetDecimal();
            }
            catch { return 0m; }
        }

        public async Task<List<HistoryPointDto>> GetPriceHistoryAsync(string symbol, int days)
        {
            if (!SymbolToId.TryGetValue(symbol, out var coinId)) return new List<HistoryPointDto>();
            try
            {
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync($"coins/{coinId}/market_chart?vs_currency=usd&days={days}&interval=daily");
                using var doc = JsonDocument.Parse(response);
                return doc.RootElement.GetProperty("prices").EnumerateArray()
                    .Select(p => new HistoryPointDto {
                        Date = DateTimeOffset.FromUnixTimeMilliseconds(p[0].GetInt64()).UtcDateTime.Date,
                        Value = p[1].GetDecimal()
                    }).ToList();
            }
            catch { return new List<HistoryPointDto>(); }
        }

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
                var imageUrl = doc.RootElement.GetProperty("image").GetProperty("large").GetString() ?? string.Empty;
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
