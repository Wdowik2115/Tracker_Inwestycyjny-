using Microsoft.EntityFrameworkCore;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories.Implementations
{
    public class WatchlistRepository : BaseRepository<WatchlistItem>, IWatchlistRepository
    {
        public WatchlistRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<WatchlistItem>> GetByUserIdAsync(Guid userId)
        {
            return await _dbContext.Watchlist
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
        }

        public async Task<WatchlistItem?> GetByUserAndCoinAsync(Guid userId, string coinId)
        {
            return await _dbContext.Watchlist
                .FirstOrDefaultAsync(w => w.UserId == userId && w.CoinId == coinId);
        }
    }
}
