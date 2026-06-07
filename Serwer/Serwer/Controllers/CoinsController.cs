using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/coins")]
    [Authorize]
    public class CoinsController : ControllerBase
    {
        private readonly ICoinSearchService _coinSearchService;

        public CoinsController(ICoinSearchService coinSearchService)
        {
            _coinSearchService = coinSearchService;
        }

        /// <summary>Searches for coins by symbol or name with autocomplete support.</summary>
        /// <param name="query">Search query (symbol or name, min 1 char)</param>
        /// <returns>List of matching coins with images, max 10 results</returns>
        [HttpGet("search")]
        public async Task<ActionResult<List<CoinSearchDto>>> SearchCoins([FromQuery] string? query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 1)
                return Ok(new List<CoinSearchDto>());

            var coins = await _coinSearchService.SearchCoinsAsync(query);
            return Ok(coins);
        }

        /// <summary>Gets detailed information for a specific coin.</summary>
        /// <param name="coinId">CoinGecko coin ID (e.g., 'bitcoin')</param>
        /// <returns>Coin details including image URL</returns>
        [HttpGet("{coinId}")]
        public async Task<ActionResult<CoinDetailDto>> GetCoinDetails(string coinId)
        {
            var coin = await _coinSearchService.GetCoinDetailsAsync(coinId);
            if (coin == null)
                return NotFound();

            return Ok(coin);
        }
    }
}
