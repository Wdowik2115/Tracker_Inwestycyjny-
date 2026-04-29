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

namespace Serwer.Tests.Application.Services
{
    public class PortfolioServiceTests
    {
        [Fact]
        public async Task CalculateTotalValueAsync_ReturnsSumOfQuantityTimesPrice()
        {
            // Arrange
            const int walletId = 1;
            var assets = new List<Asset>
            {
                new Asset { CoinId = "btc", Quantity = 2m },
                new Asset { CoinId = "eth", Quantity = 3m }
            };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var assetRepoMock = new Mock<IAssetRepository>();
            var priceServiceMock = new Mock<ICoinPriceService>();

            assetRepoMock
                .Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(assets);

            priceServiceMock
                .Setup(p => p.GetCurrentPriceAsync("btc"))
                .ReturnsAsync(100m);
            priceServiceMock
                .Setup(p => p.GetCurrentPriceAsync("eth"))
                .ReturnsAsync(10m);

            unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);

            var svc = new PortfolioService(unitOfWorkMock.Object, priceServiceMock.Object);

            // Act
            var total = await svc.CalculateTotalValueAsync(walletId);

            // Assert
            // 2 * 100 + 3 * 10 = 230
            Assert.Equal(230m, total);
        }

        [Fact]
        public async Task GetPnLReportAsync_CallsPriceServiceForEachAssetAndReturnsSameCount()
        {
            // Arrange
            const int walletId = 2;
            var assets = new List<Asset>
            {
                new Asset { CoinId = "a", Quantity = 1m, AverageBuyPrice = 5m },
                new Asset { CoinId = "b", Quantity = 2m, AverageBuyPrice = 3m }
            };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var assetRepoMock = new Mock<IAssetRepository>();
            var priceServiceMock = new Mock<ICoinPriceService>();

            assetRepoMock
                .Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(assets);

            priceServiceMock
                .Setup(p => p.GetCurrentPriceAsync(It.IsAny<string>()))
                .ReturnsAsync(10m);

            unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);

            var svc = new PortfolioService(unitOfWorkMock.Object, priceServiceMock.Object);

            // Act
            var report = (await svc.GetPnLReportAsync(walletId)).ToList();

            // Assert
            Assert.Equal(assets.Count, report.Count);
            // verify price service called for each asset coin id
            priceServiceMock.Verify(p => p.GetCurrentPriceAsync("a"), Times.Once);
            priceServiceMock.Verify(p => p.GetCurrentPriceAsync("b"), Times.Once);
        }
    }
}