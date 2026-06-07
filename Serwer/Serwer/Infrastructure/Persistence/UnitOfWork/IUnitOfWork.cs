namespace Investe.Infrastructure.Persistence.UnitOfWork
{
    using Investe.Infrastructure.Persistence.Repositories;

    public interface IUnitOfWork : IDisposable
    {
        IWalletRepository Wallets { get; }
        IAssetRepository Assets { get; }
        ITransactionRepository Transactions { get; }
        IPriceAlertRepository PriceAlerts { get; }
        IUserRepository Users { get; }
        IPriceHistoryCacheRepository PriceHistory { get; }
        IReportRepository Reports { get; }
        IWatchlistRepository Watchlist { get; }
        Task<int> CompleteAsync();
    }
}
