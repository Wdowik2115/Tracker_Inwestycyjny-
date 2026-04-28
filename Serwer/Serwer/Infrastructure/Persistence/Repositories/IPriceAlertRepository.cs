using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories
{
    public interface IPriceAlertRepository : IBaseRepository<PriceAlert>
    {
        Task<IEnumerable<PriceAlert>> GetAlertsByUserIdAsync(string userId);
        Task<IEnumerable<PriceAlert>> GetActiveAlertsAsync();
    }
}
