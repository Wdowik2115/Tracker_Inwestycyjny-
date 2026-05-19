using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;
using Microsoft.EntityFrameworkCore;

namespace Investe.Infrastructure.Persistence.Repositories.Implementations
{
    public class PriceHistoryCacheRepository : BaseRepository<PriceHistoryCache>, IPriceHistoryCacheRepository
    {
        private readonly ApplicationDbContext _ctx;

        public PriceHistoryCacheRepository(ApplicationDbContext ctx) : base(ctx)
        {
            _ctx = ctx;
        }

        public async Task<List<PriceHistoryCache>> GetByCoinAndDateRangeAsync(string coinId, DateTime from, DateTime to)
        {
            return await _ctx.PriceHistoryCache
                .Where(p => p.CoinId == coinId && p.Date >= from && p.Date <= to)
                .OrderBy(p => p.Date)
                .ToListAsync();
        }
    }
}
