using Investe.Application.DTOs;
using Investe.Domain.Entities;

namespace Investe.Application.Interfaces.Services
{
    public interface IWalletService
    {
        Task<WalletDto> CreateAsync(string userId, string name, string description);
        Task<IEnumerable<WalletDto>> GetUserWalletsAsync(string userId);
        Task<WalletDto?> GetDetailsAsync(int walletId);
    }

    public interface ITransactionService
    {
        Task ProcessTransactionAsync(TransactionCreateDto dto);
        Task<IEnumerable<Transaction>> GetHistoryAsync(int walletId);
    }

    public interface IPortfolioService
    {
        Task<decimal> CalculateTotalValueAsync(int walletId);
        Task<IEnumerable<AssetResponseDto>> GetPnLReportAsync(int walletId);
    }

    public interface ICoinPriceService
    {
        Task<decimal> GetCurrentPriceAsync(string coinId);
    }
}
