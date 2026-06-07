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
        IReportRepository Reports { get; }
        IChatMessageRepository ChatMessages { get; }
        Task<int> CompleteAsync();
    }
}
