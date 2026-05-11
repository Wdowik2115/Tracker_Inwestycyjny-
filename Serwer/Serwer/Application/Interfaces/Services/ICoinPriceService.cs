namespace Investe.Application.Interfaces.Services
{
    public interface ICoinPriceService
    {
        /// <summary>Returns the current USD price for a coin symbol (e.g. "BTC"). Returns 0 on failure.</summary>
        Task<decimal> GetCurrentPriceAsync(string symbol);

        /// <summary>Returns the historical USD price for a coin symbol on the given UTC date. Returns 0 on failure.</summary>
        Task<decimal> GetHistoricalPriceAsync(string symbol, DateTime date);
    }
}
