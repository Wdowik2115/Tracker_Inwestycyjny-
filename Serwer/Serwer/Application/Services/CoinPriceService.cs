using System.Text.Json;
using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Domain.Entities;
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
            ["UNI"]   = "uniswap",
            ["NEAR"]  = "near"
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<CoinPriceService> _logger;
        private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan TodayHistoryTtl = TimeSpan.FromHours(4);

        public CoinPriceService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IUnitOfWork unitOfWork,
            ILogger<CoinPriceService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _unitOfWork = unitOfWork;
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

        public Task<Dictionary<string, string>> GetSupportedCoinsAsync()
        {
            return Task.FromResult(new Dictionary<string, string>(SymbolToId));
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

        /// <summary>
        /// Returns daily USD prices for the last <paramref name="days"/> days from DB cache.
        /// Fetches from CoinGecko when data is missing or today's entry is older than 4 hours.
        /// </summary>
        public async Task<List<HistoryPointDto>> GetPriceHistoryAsync(string symbol, int days)
        {
            if (!SymbolToId.TryGetValue(symbol.ToUpperInvariant(), out var coinId))
            {
                _logger.LogWarning("Unknown symbol {Symbol} — no CoinGecko mapping", symbol);
                return new List<HistoryPointDto>();
            }

            var today = DateTime.UtcNow.Date;
            var from = today.AddDays(-(days - 1));

            var cached = await _unitOfWork.PriceHistory.GetByCoinAndDateRangeAsync(coinId, from, today);

            // Check completeness: all expected past dates present + today fresh
            var cachedDateSet = cached.Select(c => c.Date.Date).ToHashSet();
            var allPastDatesPresent = Enumerable.Range(0, days - 1)
                .Select(i => from.AddDays(i))
                .All(d => cachedDateSet.Contains(d));

            var todayEntry = cached.FirstOrDefault(c => c.Date.Date == today);
            var todayIsFresh = todayEntry != null && (DateTime.UtcNow - todayEntry.FetchedAt) < TodayHistoryTtl;

            if (!allPastDatesPresent || !todayIsFresh)
            {
                try
                {
                    var client = _httpClientFactory.CreateClient("CoinGecko");
                    var response = await client.GetStringAsync(
                        $"coins/{coinId}/market_chart?vs_currency=usd&days={days}&interval=daily");

                    using var doc = JsonDocument.Parse(response);
                    var fetchedAt = DateTime.UtcNow;

                    foreach (var point in doc.RootElement.GetProperty("prices").EnumerateArray())
                    {
                        var timestampMs = point[0].GetInt64();
                        var price = point[1].GetDecimal();
                        var date = DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime.Date;

                        if (date < from || date > today) continue;

                        var existing = cached.FirstOrDefault(c => c.Date.Date == date);
                        if (existing != null)
                        {
                            existing.PriceUsd = price;
                            existing.FetchedAt = fetchedAt;
                        }
                        else
                        {
                            var entry = new PriceHistoryCache
                            {
                                CoinId = coinId,
                                Date = date,
                                PriceUsd = price,
                                FetchedAt = fetchedAt
                            };
                            await _unitOfWork.PriceHistory.AddAsync(entry);
                            cached.Add(entry);
                        }
                    }

                    await _unitOfWork.CompleteAsync();

                    _logger.LogInformation("Fetched {Days}-day price history for {Symbol} from CoinGecko", days, symbol);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch price history for {Symbol}", symbol);
                }
            }

            return cached
                .Select(c => new HistoryPointDto { Date = c.Date, Value = c.PriceUsd })
                .OrderBy(p => p.Date)
                .ToList();
        }
    }
}
