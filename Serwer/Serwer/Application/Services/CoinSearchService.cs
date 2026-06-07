using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class CoinSearchService : ICoinSearchService
    {
        private static readonly Dictionary<string, (string name, string symbol, string coinId)> KnownCoins = new(StringComparer.OrdinalIgnoreCase)
        {
            ["BTC"] = ("Bitcoin", "BTC", "bitcoin"),
            ["ETH"] = ("Ethereum", "ETH", "ethereum"),
            ["SOL"] = ("Solana", "SOL", "solana"),
            ["BNB"] = ("Binance Coin", "BNB", "binancecoin"),
            ["USDT"] = ("Tether", "USDT", "tether"),
            ["USDC"] = ("USD Coin", "USDC", "usd-coin"),
            ["ADA"] = ("Cardano", "ADA", "cardano"),
            ["DOT"] = ("Polkadot", "DOT", "polkadot"),
            ["AVAX"] = ("Avalanche", "AVAX", "avalanche-2"),
            ["MATIC"] = ("Polygon", "MATIC", "matic-network"),
            ["LINK"] = ("Chainlink", "LINK", "chainlink"),
            ["XRP"] = ("Ripple", "XRP", "ripple"),
            ["LTC"] = ("Litecoin", "LTC", "litecoin"),
            ["BCH"] = ("Bitcoin Cash", "BCH", "bitcoin-cash"),
            ["XLM"] = ("Stellar", "XLM", "stellar"),
            ["DOGE"] = ("Dogecoin", "DOGE", "dogecoin"),
            ["UNI"] = ("Uniswap", "UNI", "uniswap"),
            ["ATOM"] = ("Cosmos", "ATOM", "cosmos"),
            ["ARB"] = ("Arbitrum", "ARB", "arbitrum"),
            ["OP"] = ("Optimism", "OP", "optimism"),
        };

        private readonly ICoinPriceService _coinPriceService;
        private readonly ILogger<CoinSearchService> _logger;

        public CoinSearchService(ICoinPriceService coinPriceService, ILogger<CoinSearchService> logger)
        {
            _coinPriceService = coinPriceService;
            _logger = logger;
        }

        public async Task<List<CoinSearchDto>> SearchCoinsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<CoinSearchDto>();

            var searchQuery = query.Trim().ToUpperInvariant();
            var results = new List<CoinSearchDto>();

            foreach (var (symbol, (name, _, coinId)) in KnownCoins)
            {
                if (symbol.StartsWith(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    var imageUrl = await _coinPriceService.GetCoinImageUrlAsync(coinId);
                    results.Add(new CoinSearchDto
                    {
                        CoinId = coinId,
                        Symbol = symbol,
                        Name = name,
                        ImageUrl = imageUrl
                    });

                    if (results.Count >= 10)
                        break;
                }
            }

            return results;
        }

        public async Task<CoinDetailDto?> GetCoinDetailsAsync(string coinId)
        {
            if (string.IsNullOrWhiteSpace(coinId))
                return null;

            var coin = KnownCoins.Values.FirstOrDefault(c => 
                c.coinId.Equals(coinId, StringComparison.OrdinalIgnoreCase));

            if (coin == default)
            {
                _logger.LogWarning("Coin not found: {CoinId}", coinId);
                return null;
            }

            var imageUrl = await _coinPriceService.GetCoinImageUrlAsync(coinId);

            return new CoinDetailDto
            {
                CoinId = coinId,
                Symbol = coin.symbol,
                Name = coin.name,
                ImageUrl = imageUrl
            };
        }
    }
}
