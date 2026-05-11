using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Application.Mappings;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinPriceService _priceService;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            IUnitOfWork unitOfWork,
            ICoinPriceService priceService,
            ILogger<TransactionService> logger)
        {
            _unitOfWork = unitOfWork;
            _priceService = priceService;
            _logger = logger;
        }

        /// <summary>Adds a buy/sell transaction for the user's wallet, auto-filling cost basis when missing.</summary>
        public async Task<TransactionDto> AddTransactionAsync(Guid userId, TransactionCreateDto dto)
        {
            var wallet = await _unitOfWork.Wallets.GetByIdAsync(dto.WalletId)
                ?? throw new KeyNotFoundException($"Wallet {dto.WalletId} not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Wallet does not belong to this user.");

            var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(dto.WalletId);
            var asset = assets.FirstOrDefault(a => a.CoinId == dto.CoinId);
            var executedAt = dto.ExecutedAt?.ToUniversalTime() ?? DateTime.UtcNow;

            if (dto.Type.Equals("Buy", StringComparison.OrdinalIgnoreCase))
            {
                if (asset == null)
                {
                    asset = new Asset
                    {
                        WalletId = dto.WalletId,
                        CoinId = dto.CoinId,
                        Symbol = dto.Symbol,
                        Name = dto.Symbol,
                        Quantity = dto.Quantity,
                        AverageBuyPrice = dto.PriceAtTime
                    };
                    await _unitOfWork.Assets.AddAsync(asset);
                }
                else
                {
                    var totalCost = (asset.Quantity * asset.AverageBuyPrice) + (dto.Quantity * dto.PriceAtTime);
                    asset.Quantity += dto.Quantity;
                    asset.AverageBuyPrice = totalCost / asset.Quantity;
                    await _unitOfWork.Assets.UpdateAsync(asset);
                }
            }
            else if (dto.Type.Equals("Sell", StringComparison.OrdinalIgnoreCase))
            {
                if (asset == null || asset.Quantity < dto.Quantity)
                    throw new InvalidOperationException("Insufficient assets to sell.");

                asset.Quantity -= dto.Quantity;
                if (asset.Quantity == 0)
                    await _unitOfWork.Assets.DeleteAsync(asset);
                else
                    await _unitOfWork.Assets.UpdateAsync(asset);
            }

            // Auto-fill cost basis for buy transactions
            decimal? costBasis = dto.CostBasisPerUnit;
            string? costBasisSource = null;
            if (dto.Type.Equals("Buy", StringComparison.OrdinalIgnoreCase) && costBasis == null)
            {
                var historicalPrice = await _priceService.GetHistoricalPriceAsync(dto.Symbol, executedAt);
                if (historicalPrice > 0)
                {
                    costBasis = historicalPrice;
                    costBasisSource = "historical_price";
                }
                else
                {
                    costBasisSource = "unknown";
                }
            }

            var transaction = new Transaction
            {
                WalletId = dto.WalletId,
                CoinId = dto.CoinId,
                Symbol = dto.Symbol,
                Type = Enum.Parse<TransactionType>(dto.Type, true),
                Quantity = dto.Quantity,
                PriceAtTime = dto.PriceAtTime,
                TotalValue = dto.Quantity * dto.PriceAtTime,
                CostBasisPerUnit = costBasis,
                CostBasisSource = costBasisSource,
                ExecutedAt = executedAt,
                Notes = dto.Notes
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Transaction {TxId} saved for wallet {WalletId}", transaction.Id, dto.WalletId);
            return transaction.ToDto();
        }

        /// <summary>Returns all transactions across all wallets owned by the user.</summary>
        public async Task<IEnumerable<TransactionDto>> GetUserTransactionsAsync(Guid userId)
        {
            var wallets = await _unitOfWork.Wallets.GetWalletsByUserIdAsync(userId);
            var result = new List<TransactionDto>();

            foreach (var wallet in wallets)
            {
                var txs = await _unitOfWork.Transactions.GetTransactionsByWalletIdAsync(wallet.Id);
                result.AddRange(txs.Select(t => t.ToDto()));
            }

            return result.OrderByDescending(t => t.ExecutedAt);
        }

        /// <summary>Deletes a transaction owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        public async Task DeleteTransactionAsync(Guid userId, Guid transactionId)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId)
                ?? throw new KeyNotFoundException($"Transaction {transactionId} not found.");

            var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId)
                ?? throw new KeyNotFoundException($"Wallet {transaction.WalletId} not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Transaction does not belong to this user.");

            await _unitOfWork.Transactions.DeleteAsync(transaction);
            await _unitOfWork.CompleteAsync();
        }
    }
}
