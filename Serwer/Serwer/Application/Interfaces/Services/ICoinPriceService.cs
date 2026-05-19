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

        /// <summary>
        /// Returns daily USD prices for the last <paramref name="days"/> days.
        /// Served from the DB cache; only calls CoinGecko when data is missing or today's price is stale (&gt; 4 h old).
        /// Returns an empty list for unknown symbols.
        /// </summary>
        Task<List<HistoryPointDto>> GetPriceHistoryAsync(string symbol, int days);
    }
}
