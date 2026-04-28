using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Application.Mappings;
using Investe.Infrastructure.Persistence.UnitOfWork;

namespace Investe.Application.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinPriceService _priceService;

        public PortfolioService(IUnitOfWork unitOfWork, ICoinPriceService priceService)
        {
            _unitOfWork = unitOfWork;
            _priceService = priceService;
        }

        public async Task<decimal> CalculateTotalValueAsync(int walletId)
        {
            var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(walletId);
            decimal totalValue = 0;

            foreach (var asset in assets)
            {
                var price = await _priceService.GetCurrentPriceAsync(asset.CoinId);
                totalValue += asset.Quantity * price;
            }

            return totalValue;
        }

        public async Task<IEnumerable<AssetResponseDto>> GetPnLReportAsync(int walletId)
        {
            var assets = await _unitOfWork.Assets.GetAssetsByWalletIdAsync(walletId);
            var report = new List<AssetResponseDto>();

            foreach (var asset in assets)
            {
                var price = await _priceService.GetCurrentPriceAsync(asset.CoinId);
                report.Add(asset.ToDto(price));
            }

            return report;
        }
    }

    public class CoinPriceService : ICoinPriceService
    {
        public async Task<decimal> GetCurrentPriceAsync(string coinId)
        {
            return await Task.FromResult(0m); 
        }
    }
}
