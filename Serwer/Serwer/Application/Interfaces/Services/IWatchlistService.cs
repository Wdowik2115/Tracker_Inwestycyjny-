using Investe.Application.DTOs;

namespace Investe.Application.Interfaces.Services
{
    public interface IWatchlistService
    {
        Task<IEnumerable<WatchlistItemDto>> GetWatchlistAsync(Guid userId);
        Task<WatchlistItemDto> AddToWatchlistAsync(Guid userId, AddToWatchlistDto dto);
        Task RemoveFromWatchlistAsync(Guid userId, Guid id);
        Task<bool> IsOnWatchlistAsync(Guid userId, string coinId);
        Task<IEnumerable<string>> GetSuggestionsAsync(string query);
    }
}
