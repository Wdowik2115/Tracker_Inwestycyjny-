using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IWatchlistService
    {
        Task<IEnumerable<WatchlistItemDto>> GetWatchlistAsync(Guid userId);
        Task<WatchlistItemDto> GetWatchlistItemByIdAsync(Guid userId, Guid id);
        Task<(WatchlistItemDto Item, bool IsCreated)> AddToWatchlistAsync(Guid userId, AddToWatchlistDto dto);
        /// <summary>Deletes a watchlist item owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        Task RemoveFromWatchlistAsync(Guid userId, Guid id);
        Task<bool> IsOnWatchlistAsync(Guid userId, string coinId);
    }
}
