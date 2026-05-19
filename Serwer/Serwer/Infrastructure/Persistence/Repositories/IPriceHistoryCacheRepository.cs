using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories
{
    public interface IPriceHistoryCacheRepository : IBaseRepository<PriceHistoryCache>
    {
        Task<List<PriceHistoryCache>> GetByCoinAndDateRangeAsync(string coinId, DateTime from, DateTime to);
    }
}
