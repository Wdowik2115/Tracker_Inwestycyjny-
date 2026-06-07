using System.Text.Json;
using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Infrastructure.Persistence.UnitOfWork;
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

        public Task<Dictionary<string, string>> GetSupportedCoinsAsync()
        {
            return Task.FromResult(new Dictionary<string, string>(SymbolToId));
        }

        /// <summary>Returns the historical USD price for a coin symbol on the given UTC date. Returns 0 on failure.</summary>
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
    }
}
