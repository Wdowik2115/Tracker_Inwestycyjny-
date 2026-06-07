using System.Text.Json;
using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class CoinSearchService : ICoinSearchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<CoinSearchService> _logger;
        private static readonly TimeSpan SearchCacheTtl = TimeSpan.FromSeconds(60);

        public CoinSearchService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<CoinSearchService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<List<CoinSearchDto>> SearchCoinsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return [];

            var cacheKey = $"search:{query.Trim().ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out List<CoinSearchDto> cached))
                return cached;

            try
            {
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync($"search?query={Uri.EscapeDataString(query.Trim())}");

                using var doc = JsonDocument.Parse(response);
                var coins = doc.RootElement.GetProperty("coins");

                var results = new List<CoinSearchDto>();
                foreach (var coin in coins.EnumerateArray().Take(10))
                {
                    results.Add(new CoinSearchDto
                    {
                        CoinId = coin.GetProperty("id").GetString() ?? string.Empty,
                        Symbol = (coin.GetProperty("symbol").GetString() ?? string.Empty).ToUpperInvariant(),
                        Name = coin.GetProperty("name").GetString() ?? string.Empty,
                        ImageUrl = coin.TryGetProperty("large", out var large) ? large.GetString() : coin.TryGetProperty("thumb", out var thumb) ? thumb.GetString() : null,
                        Rank = coin.TryGetProperty("market_cap_rank", out var rank) && rank.ValueKind == JsonValueKind.Number ? rank.GetInt32() : null,
                    });
                }

                _cache.Set(cacheKey, results, SearchCacheTtl);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CoinGecko search failed for query {Query}", query);
                return [];
            }
        }

        public async Task<CoinDetailDto?> GetCoinDetailsAsync(string coinId)
        {
            if (string.IsNullOrWhiteSpace(coinId))
                return null;

            var cacheKey = $"detail:{coinId.ToLowerInvariant()}";
            if (_cache.TryGetValue(cacheKey, out CoinDetailDto cached))
                return cached;

            try
            {
                var client = _httpClientFactory.CreateClient("CoinGecko");
                var response = await client.GetStringAsync($"coins/{Uri.EscapeDataString(coinId.ToLowerInvariant())}?localization=false&tickers=false&market_data=false&community_data=false&developer_data=false");

                using var doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                var result = new CoinDetailDto
                {
                    CoinId = root.GetProperty("id").GetString() ?? coinId,
                    Symbol = (root.GetProperty("symbol").GetString() ?? string.Empty).ToUpperInvariant(),
                    Name = root.GetProperty("name").GetString() ?? string.Empty,
                    ImageUrl = root.TryGetProperty("image", out var img)
                        ? img.TryGetProperty("large", out var large) ? large.GetString() ?? string.Empty : string.Empty
                        : string.Empty,
                };

                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch coin details for {CoinId}", coinId);
                return null;
            }
        }
    }
}
