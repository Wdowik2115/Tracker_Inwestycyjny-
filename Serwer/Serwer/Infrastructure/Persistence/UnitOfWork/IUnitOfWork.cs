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
        Task<int> CompleteAsync();
    }
}
