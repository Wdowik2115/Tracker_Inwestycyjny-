using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinPriceService _priceService;
        private readonly ILogger<PortfolioService> _logger;

        public PortfolioService(
            IUnitOfWork unitOfWork,
            ICoinPriceService priceService,
            ILogger<PortfolioService> logger)
        {
            _unitOfWork = unitOfWork;
            _priceService = priceService;
            _logger = logger;
        }

        /// <summary>Returns a full portfolio summary with per-asset P&amp;L using live prices from CoinGecko.</summary>
        public async Task<PortfolioSummaryDto> GetSummaryAsync(Guid userId)
        {
            var wallets = await _unitOfWork.Wallets.GetWalletsByUserIdAsync(userId);
            var allAssets = new List<(string Symbol, decimal Quantity, decimal AvgCost)>();

            foreach (var wallet in wallets)
            {
                var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(wallet.Id);
                foreach (var a in assets)
                    allAssets.Add((a.Symbol, a.Quantity, a.AverageBuyPrice));
            }

            // Group by symbol, compute AVCO across wallets
            var positions = new List<PositionDto>();
            foreach (var group in allAssets.GroupBy(a => a.Symbol))
            {
                var totalQty = group.Sum(a => a.Quantity);
                var weightedCost = group.Sum(a => a.Quantity * a.AvgCost);
                var avgCost = totalQty > 0 ? weightedCost / totalQty : 0m;

                var currentPrice = await _priceService.GetCurrentPriceAsync(group.Key);
                var valueUsdt = totalQty * currentPrice;
                var costBasis = totalQty * avgCost;
                var pnlUsdt = valueUsdt - costBasis;
                var pnlPercent = costBasis > 0 ? pnlUsdt / costBasis * 100m : 0m;

                positions.Add(new PositionDto
                {
                    Symbol = group.Key,
                    Quantity = totalQty,
                    AvgCostBasis = avgCost,
                    CurrentPrice = currentPrice,
                    ValueUsdt = valueUsdt,
                    PnlUsdt = pnlUsdt,
                    PnlPercent = pnlPercent
                });
            }

            var summary = new PortfolioSummaryDto
            {
                Positions = positions,
                TotalValueUsdt = positions.Sum(p => p.ValueUsdt),
                TotalPnlUsdt = positions.Sum(p => p.PnlUsdt)
            };

            _logger.LogInformation("Portfolio summary built for user {UserId}: {Count} positions", userId, positions.Count);
            return summary;
        }
    }
}
