using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Investe.Application.Services;
using Investe.Application.Interfaces.Services;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Investe.Infrastructure.Persistence.Repositories;
using Investe.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Serwer.Tests.Application.Services
{
    public class PortfolioServiceTests
    {
        private static (Mock<IUnitOfWork>, Mock<IWalletRepository>, Mock<IAssetRepository>, Mock<ICoinPriceService>, PortfolioService)
            BuildSut()
        {
            var uow = new Mock<IUnitOfWork>();
            var walletRepo = new Mock<IWalletRepository>();
            var assetRepo = new Mock<IAssetRepository>();
            var priceService = new Mock<ICoinPriceService>();
            var logger = new Mock<ILogger<PortfolioService>>();

            uow.Setup(u => u.Wallets).Returns(walletRepo.Object);
            uow.Setup(u => u.Assets).Returns(assetRepo.Object);

            var svc = new PortfolioService(uow.Object, priceService.Object, logger.Object);
            return (uow, walletRepo, assetRepo, priceService, svc);
        }

        [Fact]
        public async Task GetSummaryAsync_SingleAsset_ReturnsCorrectPosition()
        {
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var (_, walletRepo, assetRepo, priceService, svc) = BuildSut();

            var wallet = new Wallet { Id = walletId, UserId = userId };
            var assets = new List<Asset>
            {
                new Asset { Symbol = "BTC", CoinId = "bitcoin", Quantity = 2m, AverageBuyPrice = 30000m }
            };

            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId)).ReturnsAsync(new List<Wallet> { wallet });
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(assets);
            priceService.Setup(p => p.GetCurrentPriceAsync("BTC")).ReturnsAsync(35000m);

            var summary = await svc.GetSummaryAsync(userId);

            Assert.Single(summary.Positions);
            var pos = summary.Positions.First();
            Assert.Equal("BTC", pos.Symbol);
            Assert.Equal(2m, pos.Quantity);
            Assert.Equal(35000m, pos.CurrentPrice);
            Assert.Equal(70000m, pos.ValueUsdt);    // 2 * 35000
            Assert.Equal(10000m, pos.PnlUsdt);      // 70000 - 60000
            Assert.Equal(70000m, summary.TotalValueUsdt);
            Assert.Equal(10000m, summary.TotalPnlUsdt);
        }

        [Fact]
        public async Task GetSummaryAsync_EmptyPortfolio_ReturnsZeroTotals()
        {
            var userId = Guid.NewGuid();
            var (_, walletRepo, _, _, svc) = BuildSut();

            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId)).ReturnsAsync(new List<Wallet>());

            var summary = await svc.GetSummaryAsync(userId);

            Assert.Empty(summary.Positions);
            Assert.Equal(0m, summary.TotalValueUsdt);
            Assert.Equal(0m, summary.TotalPnlUsdt);
        }
    }
}
