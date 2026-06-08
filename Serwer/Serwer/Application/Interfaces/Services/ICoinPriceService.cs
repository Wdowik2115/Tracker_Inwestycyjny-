using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface ICoinPriceService
    {
        /// <summary>Returns the current USD price for a coin symbol (e.g. "BTC"). Returns 0 on failure.</summary>
        Task<decimal> GetCurrentPriceAsync(string symbol);

        /// <summary>Returns current USD prices for multiple symbols in a single API call. Missing/unknown symbols return 0.</summary>
        Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> symbols);

        /// <summary>Returns the historical USD price for a coin symbol on the given UTC date. Returns 0 on failure.</summary>
        Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime date);

        /// <summary>Returns daily USD prices for the last <paramref name="days"/> days.</summary>
        Task<List<HistoryPointDto>> GetPriceHistoryAsync(string symbol, int days);

/// <summary>Returns the top N coins by price change percentage in the last 24h.</summary>
        Task<IEnumerable<CoinMarketDataDto>> GetTopMoversAsync(int count = 10, bool ascending = false);

        /// <summary>Returns the image URL for a coin from CoinGecko. Returns empty string on failure.</summary>
        Task<string> GetCoinImageUrlAsync(string coinId);
    }
}
