using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories
{
    public interface IAssetRepository : IBaseRepository<Asset>
    {
        Task<IEnumerable<Asset>> GetAssetsByWalletIdAsync(Guid walletId);
    }
}
