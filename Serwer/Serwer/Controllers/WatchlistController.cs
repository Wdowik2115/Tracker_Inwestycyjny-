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

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetWatchlistItem(Guid id)
        {
            var item = await _watchlistService.GetWatchlistItemByIdAsync(UserId, id);
            return Ok(item);
        }

        [HttpPost]
        public async Task<IActionResult> AddToWatchlist([FromBody] AddToWatchlistDto dto)
        {
            var (item, isCreated) = await _watchlistService.AddToWatchlistAsync(UserId, dto);
            
            if (isCreated)
                return CreatedAtAction(nameof(GetWatchlistItem), new { id = item.Id }, item);

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

    }
}
