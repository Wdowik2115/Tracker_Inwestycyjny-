using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories
{
    public interface IWalletRepository : IBaseRepository<Wallet>
    {
        Task<IEnumerable<Wallet>> GetWalletsByUserIdAsync(Guid userId);
    }
}
