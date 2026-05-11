using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IPortfolioService
    {
        /// <summary>Returns a full portfolio summary with per-asset P&amp;L using live prices from CoinGecko.</summary>
        Task<PortfolioSummaryDto> GetSummaryAsync(Guid userId);
    }
}
