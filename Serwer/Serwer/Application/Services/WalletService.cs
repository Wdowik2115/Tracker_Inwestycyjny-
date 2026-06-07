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

        /// <summary>Returns all wallets belonging to or shared with the given user.</summary>
        public async Task<IEnumerable<WalletDto>> GetUserWalletsAsync(Guid userId)
        {
            var wallets = await _unitOfWork.Wallets.GetWalletsByUserIdAsync(userId);
            var walletList = wallets.ToList();

            // Collect all unique symbols across all wallets for a single batch price fetch
            var allAssets = new List<(Guid walletId, Domain.Entities.Asset asset)>();
            foreach (var wallet in walletList)
            {
                var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(wallet.Id);
                allAssets.AddRange(assets.Select(a => (wallet.Id, a)));
            }

            var symbols = allAssets.Select(x => x.asset.Symbol).Distinct();
            var prices = await _priceService.GetCurrentPricesAsync(symbols);

            return walletList.Select(wallet =>
            {
                var walletAssets = allAssets.Where(x => x.walletId == wallet.Id).ToList();
                var totalValue = walletAssets.Sum(x => x.asset.Quantity * prices.GetValueOrDefault(x.asset.Symbol, 0m));
                var costBasis = walletAssets.Sum(x => x.asset.Quantity * x.asset.AverageBuyPrice);
                var pnl = totalValue - costBasis;
                var pnlPercent = costBasis > 0 ? pnl / costBasis * 100m : 0m;

                var dto = wallet.ToDto();
                dto.TotalValue = totalValue;
                dto.AssetCount = walletAssets.Count;
                dto.Pnl = pnl;
                dto.PnlPercent = pnlPercent;
                return dto;
            });
        }

        /// <summary>Returns detailed information about a specific wallet, including assets and P&amp;L.</summary>
        public async Task<WalletDetailsDto> GetWalletDetailsAsync(Guid userId, Guid walletId)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletWithMembersAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            if (wallet.UserId != userId && !wallet.SharedWith.Any(u => u.Id == userId))
                throw new UnauthorizedAccessException("You do not have access to this wallet.");

            var assets = (await _unitOfWork.Assets.GetAssetsByWalletIdAsync(walletId)).ToList();
            var prices = await _priceService.GetCurrentPricesAsync(assets.Select(a => a.Symbol));

            var positions = new List<PositionDto>();
            decimal totalValue = 0;

            foreach (var asset in assets)
            {
                var currentPrice = prices.GetValueOrDefault(asset.Symbol, 0m);
                var value = asset.Quantity * currentPrice;
                var costBasis = asset.Quantity * asset.AverageBuyPrice;
                var pnl = value - costBasis;
                var pnlPercent = costBasis > 0 ? pnl / costBasis * 100m : 0m;

                positions.Add(new PositionDto
                {
                    Symbol = asset.Symbol,
                    Name = asset.Name,
                    Quantity = asset.Quantity,
                    AvgCostBasis = asset.AverageBuyPrice,
                    CurrentPrice = currentPrice,
                    Value = value,
                    Pnl = pnl,
                    PnlPercent = pnlPercent
                });

                totalValue += value;
            }

            var totalCostBasis = positions.Sum(p => p.Quantity * p.AvgCostBasis);
            var totalPnl = totalValue - totalCostBasis;
            var totalPnlPercent = totalCostBasis > 0 ? totalPnl / totalCostBasis * 100m : 0m;

            var allTxs = (await _unitOfWork.Transactions.GetTransactionsByWalletIdAsync(walletId))
                .OrderBy(t => t.ExecutedAt)
                .ToList();
            var realizedPnl = CalculateRealizedPnl(allTxs);

            var walletDto = wallet.ToDto();
            return new WalletDetailsDto
            {
                Id = walletDto.Id,
                OwnerId = walletDto.OwnerId,
                Name = walletDto.Name,
                Description = walletDto.Description,
                SharedWithEmails = walletDto.SharedWithEmails,
                TotalValue = totalValue,
                AssetCount = positions.Count,
                Pnl = totalPnl,
                PnlPercent = totalPnlPercent,
                RealizedPnl = realizedPnl,
                Assets = positions
            };
        }

        /// <summary>Updates the name and description of a wallet owned by the user.</summary>
        public async Task<WalletDto> UpdateWalletAsync(Guid userId, Guid walletId, UpdateWalletDto dto)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletWithMembersAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            // Allow both owner and shared users to update name/description? 
            // Usually, only the owner should manage the wallet settings.
            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Only the owner can update wallet settings.");

            wallet.Name = dto.Name;
            wallet.Description = dto.Description ?? string.Empty;

            await _unitOfWork.Wallets.UpdateAsync(wallet);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Wallet {WalletId} updated by user {UserId}", walletId, userId);
            return wallet.ToDto();
        }

        /// <summary>Returns daily portfolio value history for a wallet.</summary>
        public async Task<WalletHistoryDto> GetWalletHistoryAsync(Guid userId, Guid walletId, int days)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletWithMembersAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            if (wallet.UserId != userId && !wallet.SharedWith.Any(u => u.Id == userId))
                throw new UnauthorizedAccessException("You do not have access to this wallet.");

            // Use transactions (not current assets) so holdings are date-accurate
            var allTxs = (await _unitOfWork.Transactions.GetTransactionsByWalletIdAsync(walletId))
                .OrderBy(t => t.ExecutedAt)
                .ToList();

            if (allTxs.Count == 0)
                return new WalletHistoryDto { WalletId = walletId };

            var symbols = allTxs.Select(t => t.Symbol).Distinct().ToList();

            var symbolHistories = new Dictionary<string, List<HistoryPointDto>>(StringComparer.OrdinalIgnoreCase);
            foreach (var symbol in symbols)
                symbolHistories[symbol] = await _priceService.GetPriceHistoryAsync(symbol, days);

            var cutoff = DateTime.UtcNow.Date.AddDays(-days);
            var allDates = symbolHistories.Values
                .SelectMany(h => h.Select(p => p.Date.Date))
                .Distinct()
                .Where(d => d >= cutoff)
                .OrderBy(d => d)
                .ToList();

            if (allDates.Count == 0)
                return new WalletHistoryDto { WalletId = walletId };

            var points = allDates
                .Select(date =>
                {
                    // Replay all transactions up to this date to get holdings as of that day
                    var holdings = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                    foreach (var tx in allTxs.Where(t => t.ExecutedAt.Date <= date))
                    {
                        holdings.TryGetValue(tx.Symbol, out var qty);
                        holdings[tx.Symbol] = tx.Type == TransactionType.Buy
                            ? qty + tx.Quantity
                            : qty - tx.Quantity;
                    }

                    var value = holdings.Sum(h =>
                    {
                        if (h.Value <= 0) return 0m;
                        if (!symbolHistories.TryGetValue(h.Key, out var history)) return 0m;
                        var pricePoint = history.FirstOrDefault(p => p.Date.Date == date);
                        return pricePoint != null ? h.Value * pricePoint.Value : 0m;
                    });

                    return new HistoryPointDto { Date = date, Value = value };
                })
                .Where(p => p.Value > 0)
                .ToList();

            return new WalletHistoryDto { WalletId = walletId, Points = points };
        }

        /// <summary>Shares a wallet with another user by their email.</summary>
        public async Task ShareWalletAsync(Guid ownerId, Guid walletId, string email)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletWithMembersAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            if (wallet.UserId != ownerId)
                throw new UnauthorizedAccessException("Only the owner can share the wallet.");

            var userToShareWith = await _unitOfWork.Users.GetByEmailAsync(email)
                ?? throw new KeyNotFoundException($"User with email {email} not found.");

            if (userToShareWith.Id == ownerId)
                throw new InvalidOperationException("You cannot share a wallet with yourself.");

            if (wallet.SharedWith.Any(u => u.Id == userToShareWith.Id))
                return; // Already shared

            wallet.SharedWith.Add(userToShareWith);
            await _unitOfWork.Wallets.UpdateAsync(wallet);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Wallet {WalletId} shared with {Email}", walletId, email);
        }

        /// <summary>Removes a user from a shared wallet.</summary>
        public async Task UnshareWalletAsync(Guid ownerId, Guid walletId, string email)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletWithMembersAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            if (wallet.UserId != ownerId)
                throw new UnauthorizedAccessException("Only the owner can unshare the wallet.");

            var userToRemove = wallet.SharedWith.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (userToRemove == null)
                return;

            wallet.SharedWith.Remove(userToRemove);
            await _unitOfWork.Wallets.UpdateAsync(wallet);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("User {Email} removed from wallet {WalletId}", email, walletId);
        }

        /// <summary>Calculates realized P&amp;L by replaying transactions per symbol using weighted average cost.</summary>
        private static decimal CalculateRealizedPnl(IEnumerable<Transaction> transactions)
        {
            decimal realized = 0;
            var bySymbol = transactions.GroupBy(t => t.Symbol, StringComparer.OrdinalIgnoreCase);

            foreach (var group in bySymbol)
            {
                decimal avgCost = 0;
                decimal qty = 0;

                foreach (var tx in group.OrderBy(t => t.ExecutedAt))
                {
                    if (tx.Type == TransactionType.Buy)
                    {
                        var totalCost = qty * avgCost + tx.Quantity * tx.PriceAtTime;
                        qty += tx.Quantity;
                        avgCost = qty > 0 ? totalCost / qty : 0;
                    }
                    else if (tx.Type == TransactionType.Sell)
                    {
                        realized += (tx.PriceAtTime - avgCost) * tx.Quantity;
                        qty = Math.Max(0, qty - tx.Quantity);
                    }
                }
            }

            return realized;
        }

        /// <summary>Deletes a wallet owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        public async Task DeleteWalletAsync(Guid userId, Guid walletId)
        {
            var wallet = await _unitOfWork.Wallets.GetWalletWithMembersAsync(walletId)
                ?? throw new KeyNotFoundException($"Wallet {walletId} not found.");

            if (wallet.UserId != userId)
                throw new UnauthorizedAccessException("Only the owner can delete the wallet.");

            await _unitOfWork.Wallets.DeleteAsync(wallet);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Wallet {WalletId} deleted by user {UserId}", walletId, userId);
        }
    }
}
