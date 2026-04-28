using Investe.Infrastructure.Persistence.Repositories;
using Investe.Infrastructure.Persistence.Repositories.Implementations;

namespace Investe.Infrastructure.Persistence.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
            Wallets = new WalletRepository(_context);
            Assets = new AssetRepository(_context);
            Transactions = new TransactionRepository(_context);
            PriceAlerts = new PriceAlertRepository(_context);
        }

        public IWalletRepository Wallets { get; private set; }
        public IAssetRepository Assets { get; private set; }
        public ITransactionRepository Transactions { get; private set; }
        public IPriceAlertRepository PriceAlerts { get; private set; }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
