using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serwer.Extensions;

namespace Serwer.Controllers
{
    [ApiController]
    [Route("api/watchlist")]
    [Authorize]
    public class WatchlistController : ControllerBase
    {
        private readonly IWatchlistService _watchlistService;

        public WatchlistController(IWatchlistService watchlistService)
        {
            _watchlistService = watchlistService;
        }

        private Guid UserId => User.GetUserId();

        [HttpGet]
        public async Task<IActionResult> GetWatchlist()
        {
            var watchlist = await _watchlistService.GetWatchlistAsync(UserId);
            return Ok(watchlist);
        }

        [HttpPost]
        public async Task<IActionResult> AddToWatchlist([FromBody] AddToWatchlistDto dto)
        {
            var item = await _watchlistService.AddToWatchlistAsync(UserId, dto);
            return Ok(item);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> RemoveFromWatchlist(Guid id)
        {
            await _watchlistService.RemoveFromWatchlistAsync(UserId, id);
            return NoContent();
        }

        [HttpGet("check/{coinId}")]
        public async Task<IActionResult> IsOnWatchlist(string coinId)
        {
            var isOnWatchlist = await _watchlistService.IsOnWatchlistAsync(UserId, coinId);
            return Ok(new { isOnWatchlist });
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestions([FromQuery] string query)
        {
            var suggestions = await _watchlistService.GetSuggestionsAsync(query);
            return Ok(suggestions);
        }
    }
}
