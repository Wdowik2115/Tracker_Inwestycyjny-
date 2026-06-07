using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.Repositories.Common;

namespace Investe.Infrastructure.Persistence.Repositories
{
    public interface ITransactionRepository : IBaseRepository<Transaction>
    {
        Task<IEnumerable<Transaction>> GetByUserIdAsync(Guid userId, int count = 10);
        Task<IEnumerable<Transaction>> GetTransactionsByWalletIdAsync(Guid walletId);
        
        Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedTransactionsAsync(
            Guid userId, 
            int page, 
            int pageSize, 
            Guid? walletId = null, 
            string? symbol = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null);
    }
}
