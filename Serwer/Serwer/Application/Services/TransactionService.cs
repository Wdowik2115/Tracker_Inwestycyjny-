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
            var executedAt = dto.ExecutedAt?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;

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
                        AverageBuyPrice = dto.PriceAtTime,
                        ImageUrl = dto.ImageUrl
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
                Fee = dto.Fee,
                FeeCurrency = dto.FeeCurrency,
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

        public async Task<(IEnumerable<TransactionDto> Items, int TotalCount)> GetPagedTransactionsAsync(
            Guid userId, 
            int page, 
            int pageSize, 
            Guid? walletId = null, 
            string? symbol = null, 
            DateOnly? startDate = null, 
            DateOnly? endDate = null)
        {
            var startDateTime = startDate?.ToDateTime(TimeOnly.MinValue);
            var endDateTime = endDate?.ToDateTime(TimeOnly.MaxValue);

            var (items, totalCount) = await _unitOfWork.Transactions.GetPagedTransactionsAsync(
                userId, page, pageSize, walletId, symbol, startDateTime, endDateTime);

            return (items.Select(t => t.ToDto()), totalCount);
        }

        /// <summary>Deletes a transaction and reverses its effect on the asset position.</summary>
        public async Task DeleteTransactionAsync(Guid userId, Guid transactionId)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId)
                ?? throw new KeyNotFoundException($"Transaction {transactionId} not found.");

            var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId)
                ?? throw new KeyNotFoundException($"Wallet {transaction.WalletId} not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Transaction does not belong to this user.");

            var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(transaction.WalletId);
            var asset = assets.FirstOrDefault(a => a.CoinId == transaction.CoinId);

            if (transaction.Type == TransactionType.Buy)
            {
                if (asset != null)
                {
                    var newQty = asset.Quantity - transaction.Quantity;
                    if (newQty <= 0)
                    {
                        await _unitOfWork.Assets.DeleteAsync(asset);
                    }
                    else
                    {
                        var newAvgCost = (asset.Quantity * asset.AverageBuyPrice - transaction.Quantity * transaction.PriceAtTime) / newQty;
                        asset.Quantity = newQty;
                        asset.AverageBuyPrice = Math.Max(0m, newAvgCost);
                        await _unitOfWork.Assets.UpdateAsync(asset);
                    }
                }
            }
            else if (transaction.Type == TransactionType.Sell)
            {
                if (asset != null)
                {
                    asset.Quantity += transaction.Quantity;
                    await _unitOfWork.Assets.UpdateAsync(asset);
                }
                else
                {
                    // Asset was fully sold and removed — recreate it with the restored quantity.
                    // AverageBuyPrice cannot be recovered here; it will be 0 until the user corrects it.
                    var restored = new Asset
                    {
                        WalletId = transaction.WalletId,
                        CoinId = transaction.CoinId,
                        Symbol = transaction.Symbol,
                        Name = transaction.Symbol,
                        Quantity = transaction.Quantity,
                        AverageBuyPrice = 0m
                    };
                    await _unitOfWork.Assets.AddAsync(restored);
                }
            }

            await _unitOfWork.Transactions.DeleteAsync(transaction);
            await _unitOfWork.CompleteAsync();
        }

        /// <summary>Updates editable fields and adjusts the asset position when quantity or price changes.</summary>
        public async Task<TransactionDto> UpdateTransactionAsync(Guid userId, Guid transactionId, TransactionUpdateDto dto)
        {
            var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId)
                ?? throw new KeyNotFoundException($"Transaction {transactionId} not found.");

            var wallet = await _unitOfWork.Wallets.GetByIdAsync(transaction.WalletId)
                ?? throw new KeyNotFoundException($"Wallet {transaction.WalletId} not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Transaction does not belong to this user.");

            var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(transaction.WalletId);
            var asset = assets.FirstOrDefault(a => a.CoinId == transaction.CoinId);

            // Handle Quantity and Price changes affecting Asset
            if ((dto.Quantity.HasValue && dto.Quantity.Value != transaction.Quantity) || 
                (dto.PriceAtTime.HasValue && dto.PriceAtTime.Value != transaction.PriceAtTime))
            {
                var newQty = dto.Quantity ?? transaction.Quantity;
                var newPrice = dto.PriceAtTime ?? transaction.PriceAtTime;

                if (asset != null)
                {
                    if (transaction.Type == TransactionType.Buy)
                    {
                        // Reverse old buy
                        var qtyWithoutOld = asset.Quantity - transaction.Quantity;
                        var totalCostWithoutOld = (asset.Quantity * asset.AverageBuyPrice) - (transaction.Quantity * transaction.PriceAtTime);
                        
                        // Apply new buy
                        asset.Quantity = qtyWithoutOld + newQty;
                        if (asset.Quantity > 0)
                            asset.AverageBuyPrice = (totalCostWithoutOld + (newQty * newPrice)) / asset.Quantity;
                        else
                            asset.AverageBuyPrice = 0;
                    }
                    else if (transaction.Type == TransactionType.Sell)
                    {
                        // Reverse old sell, apply new sell
                        var qtyWithoutOld = asset.Quantity + transaction.Quantity;
                        asset.Quantity = qtyWithoutOld - newQty;
                    }

                    if (asset.Quantity < 0)
                        throw new InvalidOperationException("Update would result in negative asset quantity.");

                    if (asset.Quantity == 0)
                        await _unitOfWork.Assets.DeleteAsync(asset);
                    else
                        await _unitOfWork.Assets.UpdateAsync(asset);
                }

                transaction.Quantity = newQty;
                transaction.PriceAtTime = newPrice;
                transaction.TotalValue = newQty * newPrice;
            }

            if (dto.Fee.HasValue)
                transaction.Fee = dto.Fee.Value;
            if (dto.FeeCurrency != null)
                transaction.FeeCurrency = dto.FeeCurrency;
            if (dto.CostBasisPerUnit.HasValue)
                transaction.CostBasisPerUnit = dto.CostBasisPerUnit.Value;
            if (dto.ExecutedAt.HasValue)
                transaction.ExecutedAt = dto.ExecutedAt.Value.ToDateTime(TimeOnly.MinValue);
            if (dto.Notes is not null)
                transaction.Notes = dto.Notes;

            await _unitOfWork.Transactions.UpdateAsync(transaction);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Transaction {TxId} updated by user {UserId}", transactionId, userId);
            return transaction.ToDto();
        }
    }
}
