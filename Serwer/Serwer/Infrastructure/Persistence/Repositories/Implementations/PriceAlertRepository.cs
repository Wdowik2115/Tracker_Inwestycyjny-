using Microsoft.EntityFrameworkCore;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories.Implementations
{
    public class PriceAlertRepository : BaseRepository<PriceAlert>, IPriceAlertRepository
    {
        public PriceAlertRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<PriceAlert>> GetByUserIdAsync(Guid userId)
        {
            return await GetAlertsByUserIdAsync(userId);
        }

        public async Task<IEnumerable<PriceAlert>> GetAlertsByUserIdAsync(Guid userId)
        {
            return await _dbContext.PriceAlerts
                .Where(p => p.UserId == userId)
                .ToListAsync();
        }

        public async Task<IEnumerable<PriceAlert>> GetActiveAlertsAsync()
        {
            return await _dbContext.PriceAlerts
                .Where(p => !p.IsTriggered)
                .ToListAsync();
        }
    }
}
