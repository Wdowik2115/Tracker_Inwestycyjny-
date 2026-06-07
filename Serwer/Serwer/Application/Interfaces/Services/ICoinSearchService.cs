using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface ICoinSearchService
    {
        /// <summary>Searches for coins by symbol or name, returns up to 10 matching results with images.</summary>
        Task<List<CoinSearchDto>> SearchCoinsAsync(string query);

        /// <summary>Gets detailed information for a specific coin including its image.</summary>
        Task<CoinDetailDto?> GetCoinDetailsAsync(string coinId);
    }
}
