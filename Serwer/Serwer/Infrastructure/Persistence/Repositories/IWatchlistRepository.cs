using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories
{
    public interface IWatchlistRepository : IBaseRepository<WatchlistItem>
    {
        Task<IEnumerable<WatchlistItem>> GetByUserIdAsync(Guid userId);
        Task<WatchlistItem?> GetByUserAndCoinAsync(Guid userId, string coinId);
    }
}
