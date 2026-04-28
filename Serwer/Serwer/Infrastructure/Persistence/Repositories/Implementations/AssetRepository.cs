using Microsoft.EntityFrameworkCore;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories.Implementations
{
    public class AssetRepository : BaseRepository<Asset>, IAssetRepository
    {
        public AssetRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Asset>> GetAssetsByWalletIdAsync(int walletId)
        {
            return await _dbContext.Assets
                .Where(a => a.WalletId == walletId)
                .ToListAsync();
        }
    }
}
