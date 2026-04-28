using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;

namespace Investe.Application.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ProcessTransactionAsync(TransactionCreateDto dto)
        {
            var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(dto.WalletId);
            var asset = assets.FirstOrDefault(a => a.CoinId == dto.CoinId);

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
                {
                    throw new InvalidOperationException("Insufficient assets to sell.");
                }

                asset.Quantity -= dto.Quantity;
                if (asset.Quantity == 0)
                {
                    await _unitOfWork.Assets.DeleteAsync(asset);
                }
                else
                {
                    await _unitOfWork.Assets.UpdateAsync(asset);
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
                ExecutedAt = DateTime.UtcNow,
                Notes = dto.Notes
            };

            await _unitOfWork.Transactions.AddAsync(transaction);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<Transaction>> GetHistoryAsync(int walletId)
        {
            return await _unitOfWork.Transactions.GetTransactionsByWalletIdAsync(walletId);
        }
    }
}
