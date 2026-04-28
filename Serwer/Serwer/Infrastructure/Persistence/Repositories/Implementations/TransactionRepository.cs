using Microsoft.EntityFrameworkCore;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories.Implementations
{
    public class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<IEnumerable<Transaction>> GetTransactionsByWalletIdAsync(int walletId)
        {
            return await _dbContext.Transactions
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.ExecutedAt)
                .ToListAsync();
        }
    }
}
