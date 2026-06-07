using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Investe.Application.Services;
using Investe.Application.Interfaces.Services;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Investe.Infrastructure.Persistence.Repositories;
using Investe.Application.DTOs;
using Investe.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Serwer.Tests.Application.Services
{
    public class TransactionServiceTests
    {
        private static (Mock<IUnitOfWork>, Mock<IWalletRepository>, Mock<IAssetRepository>, Mock<ITransactionRepository>, Mock<ICoinPriceService>, TransactionService)
            BuildSut(Guid userId, Guid walletId)
        {
            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var walletRepoMock = new Mock<IWalletRepository>();
            var assetRepoMock = new Mock<IAssetRepository>();
            var transRepoMock = new Mock<ITransactionRepository>();
            var priceServiceMock = new Mock<ICoinPriceService>();
            var loggerMock = new Mock<ILogger<TransactionService>>();

            var wallet = new Wallet { Id = walletId, UserId = userId };
            walletRepoMock.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);

            priceServiceMock
                .Setup(p => p.GetHistoricalPriceAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
                .ReturnsAsync(0m);

            unitOfWorkMock.Setup(u => u.Wallets).Returns(walletRepoMock.Object);
            unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);
            unitOfWorkMock.Setup(u => u.Transactions).Returns(transRepoMock.Object);

            var svc = new TransactionService(unitOfWorkMock.Object, priceServiceMock.Object, loggerMock.Object);

            return (unitOfWorkMock, walletRepoMock, assetRepoMock, transRepoMock, priceServiceMock, svc);
        }

        [Fact]
        public async Task AddTransactionAsync_Buy_NewAsset_AddsAssetAndTransactionAndCompletes()
        {
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var (uow, _, assetRepo, transRepo, _, svc) = BuildSut(userId, walletId);

            var dto = new TransactionCreateDto
            {
                WalletId = walletId,
                CoinId = "coin-x",
                Symbol = "CX",
                Type = "Buy",
                Quantity = 5m,
                PriceAtTime = 2m,
                Notes = "test buy"
            };

            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(new List<Asset>());

            await svc.AddTransactionAsync(userId, dto);

            assetRepo.Verify(r => r.AddAsync(It.Is<Asset>(a => a.CoinId == dto.CoinId && a.Quantity == dto.Quantity)), Times.Once);
            transRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task AddTransactionAsync_Buy_ExistingAsset_UpdatesAveragePrice()
        {
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var (uow, _, assetRepo, transRepo, _, svc) = BuildSut(userId, walletId);

            var existing = new Asset { WalletId = walletId, CoinId = "coin-y", Quantity = 2m, AverageBuyPrice = 10m };
            var dto = new TransactionCreateDto
            {
                WalletId = walletId,
                CoinId = "coin-y",
                Symbol = "CY",
                Type = "Buy",
                Quantity = 1m,
                PriceAtTime = 40m
            };

            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(new List<Asset> { existing });

            Asset? updatedArg = null;
            assetRepo.Setup(r => r.UpdateAsync(It.IsAny<Asset>()))
                .Callback<Asset>(a => updatedArg = a)
                .Returns(Task.CompletedTask);

            await svc.AddTransactionAsync(userId, dto);

            // Expected average: (2*10 + 1*40) / 3 = 20
            Assert.NotNull(updatedArg);
            Assert.Equal(3m, updatedArg!.Quantity);
            Assert.Equal(20m, updatedArg.AverageBuyPrice);
            assetRepo.Verify(r => r.UpdateAsync(It.IsAny<Asset>()), Times.Once);
            transRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task AddTransactionAsync_Sell_Insufficient_ThrowsInvalidOperationException()
        {
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var (uow, _, assetRepo, transRepo, _, svc) = BuildSut(userId, walletId);

            var dto = new TransactionCreateDto
            {
                WalletId = walletId,
                CoinId = "coin-z",
                Symbol = "CZ",
                Type = "Sell",
                Quantity = 5m,
                PriceAtTime = 1m
            };

            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(new List<Asset>());

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.AddTransactionAsync(userId, dto));
            transRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task AddTransactionAsync_Sell_AllQuantity_DeletesAsset()
        {
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var (uow, _, assetRepo, transRepo, _, svc) = BuildSut(userId, walletId);

            var existing = new Asset { WalletId = walletId, CoinId = "coin-s", Quantity = 1m, AverageBuyPrice = 5m };
            var dto = new TransactionCreateDto
            {
                WalletId = walletId,
                CoinId = "coin-s",
                Symbol = "CS",
                Type = "Sell",
                Quantity = 1m,
                PriceAtTime = 10m
            };

            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(new List<Asset> { existing });

            Asset? deletedArg = null;
            assetRepo.Setup(r => r.DeleteAsync(It.IsAny<Asset>()))
                .Callback<Asset>(a => deletedArg = a)
                .Returns(Task.CompletedTask);

            await svc.AddTransactionAsync(userId, dto);

            Assert.NotNull(deletedArg);
            Assert.Equal(existing.CoinId, deletedArg!.CoinId);
            transRepo.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateTransactionAsync_Buy_ChangeQtyAndPrice_RecalculatesAssetAverageCost()
        {
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var txId = Guid.NewGuid();
            var (uow, _, assetRepo, transRepo, _, svc) = BuildSut(userId, walletId);

            // Asset currently has 10 units with avg price 50 (total cost 500)
            // One of those units came from this transaction: 2 units at 40 (contribution 80)
            var asset = new Asset { WalletId = walletId, CoinId = "btc", Quantity = 10m, AverageBuyPrice = 50m };
            var tx = new Transaction 
            { 
                Id = txId, 
                WalletId = walletId, 
                CoinId = "btc", 
                Type = TransactionType.Buy, 
                Quantity = 2m, 
                PriceAtTime = 40m 
            };

            transRepo.Setup(r => r.GetByIdAsync(txId)).ReturnsAsync(tx);
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId)).ReturnsAsync(new List<Asset> { asset });

            var dto = new TransactionUpdateDto { Quantity = 4m, PriceAtTime = 60m }; // Total contribution: 240

            await svc.UpdateTransactionAsync(userId, txId, dto);

            // Calculation:
            // totalCostWithoutOld = 500 - (2 * 40) = 420
            // qtyWithoutOld = 10 - 2 = 8
            // newQty = 8 + 4 = 12
            // newTotalCost = 420 + (4 * 60) = 420 + 240 = 660
            // newAvg = 660 / 12 = 55
            Assert.Equal(12m, asset.Quantity);
            Assert.Equal(55m, asset.AverageBuyPrice);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }
    }
}
