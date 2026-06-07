using Microsoft.EntityFrameworkCore;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories.Implementations
{
    public class WalletRepository : BaseRepository<Wallet>, IWalletRepository
    {
        public WalletRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Wallet>> GetWalletsByUserIdAsync(Guid userId)
        {
            return await _dbContext.Wallets
                .Include(w => w.SharedWith)
                .Where(w => w.UserId == userId || w.SharedWith.Any(u => u.Id == userId))
                .ToListAsync();
        }

        public async Task<Wallet?> GetWalletWithMembersAsync(Guid walletId)
        {
            return await _dbContext.Wallets
                .Include(w => w.SharedWith)
                .FirstOrDefaultAsync(w => w.Id == walletId);
        }
    }
}
