using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Application.Mappings;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinPriceService _priceService;
        private readonly ILogger<WalletService> _logger;

        public WalletService(IUnitOfWork unitOfWork, ICoinPriceService priceService, ILogger<WalletService> logger)
        {
            _unitOfWork = unitOfWork;
            _priceService = priceService;
            _logger = logger;
        }

        /// <summary>Creates a new wallet for the given user.</summary>
        public async Task<WalletDto> CreateWalletAsync(Guid userId, CreateWalletDto dto)
        {
            var wallet = new Wallet
            {
                UserId = userId,
                Name = dto.Name,
                Description = dto.Description ?? string.Empty
            };

            await _unitOfWork.Wallets.AddAsync(wallet);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Wallet {WalletId} created for user {UserId}", wallet.Id, userId);
            return wallet.ToDto();
        }

        /// <summary>Returns all wallets belonging to the given user with their total value calculated.</summary>
        public async Task<IEnumerable<WalletDto>> GetUserWalletsAsync(Guid userId)
        {
            var wallets = await _unitOfWork.Wallets.GetWalletsByUserIdAsync(userId);
            var result = new List<WalletDto>();

            foreach (var wallet in wallets)
            {
                var dto = new WalletDto
                {
                    Id = wallet.Id,
                    Name = wallet.Name,
                    Description = wallet.Description
                };

                var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(wallet.Id);
                
                decimal totalValue = 0;
                foreach (var asset in assets)
                {
                    var price = await _priceService.GetCurrentPriceAsync(asset.Symbol);
                    totalValue += asset.Quantity * price;
                }
                
                dto.TotalValue = totalValue;
                result.Add(dto);
            }

            return result;
        }

        /// <summary>Returns detailed information about a specific wallet, including assets and P&L.</summary>
        public async Task<WalletDetailsDto> GetWalletDetailsAsync(Guid userId, Guid walletId)
        {
            var wallet = await _unitOfWork.Wallets.GetByIdAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Wallet does not belong to this user.");

            var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(walletId);
            var positions = new List<PositionDto>();
            decimal totalValue = 0;

            foreach (var asset in assets)
            {
                var currentPrice = await _priceService.GetCurrentPriceAsync(asset.Symbol);
                var valueUsdt = asset.Quantity * currentPrice;
                var costBasis = asset.Quantity * asset.AverageBuyPrice;
                var pnlUsdt = valueUsdt - costBasis;
                var pnlPercent = costBasis > 0 ? pnlUsdt / costBasis * 100m : 0m;

                positions.Add(new PositionDto
                {
                    Symbol = asset.Symbol,
                    Quantity = asset.Quantity,
                    AvgCostBasis = asset.AverageBuyPrice,
                    CurrentPrice = currentPrice,
                    ValueUsdt = valueUsdt,
                    PnlUsdt = pnlUsdt,
                    PnlPercent = pnlPercent
                });

                totalValue += valueUsdt;
            }

            return new WalletDetailsDto
            {
                Id = wallet.Id,
                Name = wallet.Name,
                Description = wallet.Description,
                TotalValue = totalValue,
                Assets = positions
            };
        }

        /// <summary>Deletes a wallet owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        public async Task DeleteWalletAsync(Guid userId, Guid walletId)
        {
            var wallet = await _unitOfWork.Wallets.GetByIdAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Wallet does not belong to this user.");

            await _unitOfWork.Wallets.DeleteAsync(wallet);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Wallet {WalletId} deleted by user {UserId}", walletId, userId);
        }
    }
}
