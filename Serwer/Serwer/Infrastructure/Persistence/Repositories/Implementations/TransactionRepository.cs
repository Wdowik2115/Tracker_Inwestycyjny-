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

        public async Task<IEnumerable<Transaction>> GetTransactionsByWalletIdAsync(Guid walletId)
        {
            return await _dbContext.Transactions
                .Where(t => t.WalletId == walletId)
                .OrderByDescending(t => t.ExecutedAt)
                .ToListAsync();
        }

        public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedTransactionsAsync(
            Guid userId, 
            int page, 
            int pageSize, 
            Guid? walletId = null, 
            string? symbol = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            var query = _dbContext.Transactions
                .Include(t => t.Wallet)
                .Where(t => t.Wallet.UserId == userId);

            if (walletId.HasValue)
                query = query.Where(t => t.WalletId == walletId.Value);

            if (!string.IsNullOrWhiteSpace(symbol))
                query = query.Where(t => t.Symbol.Contains(symbol.ToUpper()));

            if (startDate.HasValue)
                query = query.Where(t => t.ExecutedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(t => t.ExecutedAt <= endDate.Value);

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(t => t.ExecutedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
