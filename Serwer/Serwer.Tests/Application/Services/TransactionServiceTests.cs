using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Investe.Application.Services;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Investe.Infrastructure.Persistence.Repositories;
using Investe.Application.DTOs;
using Investe.Domain.Entities;

namespace Serwer.Tests.Application.Services
{
    public class TransactionServiceTests
    {
        [Fact]
        public async Task ProcessTransactionAsync_Buy_NewAsset_AddsAssetAndTransactionAndCompletes()
        {
            // Arrange
            var dto = new TransactionCreateDto
            {
                WalletId = 1,
                CoinId = "coin-x",
                Symbol = "CX",
                Type = "Buy",
                Quantity = 5m,
                PriceAtTime = 2m,
                Notes = "test buy"
            };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var assetRepoMock = new Mock<IAssetRepository>();
            var transRepoMock = new Mock<ITransactionRepository>();

            assetRepoMock
                .Setup(r => r.GetAssetsByWalletIdAsync(dto.WalletId))
                .ReturnsAsync(new List<Asset>()); // no existing assets

            unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);
            unitOfWorkMock.Setup(u => u.Transactions).Returns(transRepoMock.Object);

            var svc = new TransactionService(unitOfWorkMock.Object);

            // Act
            await svc.ProcessTransactionAsync(dto);

            // Assert
            assetRepoMock.Verify(r => r.AddAsync(It.Is<Asset>(a => a.CoinId == dto.CoinId && a.Quantity == dto.Quantity)), Times.Once);
            transRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessTransactionAsync_Buy_ExistingAsset_UpdatesAveragePrice()
        {
            // Arrange
            var existing = new Asset
            {
                WalletId = 1,
                CoinId = "coin-y",
                Quantity = 2m,
                AverageBuyPrice = 10m
            };

            var dto = new TransactionCreateDto
            {
                WalletId = 1,
                CoinId = "coin-y",
                Symbol = "CY",
                Type = "Buy",
                Quantity = 1m,
                PriceAtTime = 40m
            };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var assetRepoMock = new Mock<IAssetRepository>();
            var transRepoMock = new Mock<ITransactionRepository>();

            assetRepoMock
                .Setup(r => r.GetAssetsByWalletIdAsync(dto.WalletId))
                .ReturnsAsync(new List<Asset> { existing });

            Asset? updatedArg = null;
            assetRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Asset>()))
                .Callback<Asset>(a => updatedArg = a)
                .Returns(Task.CompletedTask);

            unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);
            unitOfWorkMock.Setup(u => u.Transactions).Returns(transRepoMock.Object);

            var svc = new TransactionService(unitOfWorkMock.Object);

            // Act
            await svc.ProcessTransactionAsync(dto);

            // Assert
            // Expected new average: (2*10 + 1*40) / (2+1) = 20
            Assert.NotNull(updatedArg);
            Assert.Equal(3m, updatedArg!.Quantity);
            Assert.Equal(20m, updatedArg.AverageBuyPrice);
            assetRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Asset>()), Times.Once);
            transRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessTransactionAsync_Sell_Insufficient_ThrowsInvalidOperationException()
        {
            // Arrange
            var dto = new TransactionCreateDto
            {
                WalletId = 1,
                CoinId = "coin-z",
                Symbol = "CZ",
                Type = "Sell",
                Quantity = 5m,
                PriceAtTime = 1m
            };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var assetRepoMock = new Mock<IAssetRepository>();
            var transRepoMock = new Mock<ITransactionRepository>();

            // no assets => insufficient
            assetRepoMock
                .Setup(r => r.GetAssetsByWalletIdAsync(dto.WalletId))
                .ReturnsAsync(new List<Asset>());

            unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);
            unitOfWorkMock.Setup(u => u.Transactions).Returns(transRepoMock.Object);

            var svc = new TransactionService(unitOfWorkMock.Object);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ProcessTransactionAsync(dto));
            transRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Never);
            unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task ProcessTransactionAsync_Sell_AllQuantity_DeletesAsset()
        {
            // Arrange
            var existing = new Asset
            {
                WalletId = 1,
                CoinId = "coin-s",
                Quantity = 1m,
                AverageBuyPrice = 5m
            };

            var dto = new TransactionCreateDto
            {
                WalletId = 1,
                CoinId = "coin-s",
                Symbol = "CS",
                Type = "Sell",
                Quantity = 1m,
                PriceAtTime = 10m
            };

            var unitOfWorkMock = new Mock<IUnitOfWork>();
            var assetRepoMock = new Mock<IAssetRepository>();
            var transRepoMock = new Mock<ITransactionRepository>();

            assetRepoMock
                .Setup(r => r.GetAssetsByWalletIdAsync(dto.WalletId))
                .ReturnsAsync(new List<Asset> { existing });

            Asset? deletedArg = null;
            assetRepoMock
                .Setup(r => r.DeleteAsync(It.IsAny<Asset>()))
                .Callback<Asset>(a => deletedArg = a)
                .Returns(Task.CompletedTask);

            unitOfWorkMock.Setup(u => u.Assets).Returns(assetRepoMock.Object);
            unitOfWorkMock.Setup(u => u.Transactions).Returns(transRepoMock.Object);

            var svc = new TransactionService(unitOfWorkMock.Object);

            // Act
            await svc.ProcessTransactionAsync(dto);

            // Assert
            Assert.NotNull(deletedArg);
            Assert.Equal(existing.CoinId, deletedArg!.CoinId);
            transRepoMock.Verify(r => r.AddAsync(It.IsAny<Transaction>()), Times.Once);
            unitOfWorkMock.Verify(u => u.CompleteAsync(), Times.Once);
        }
    }
}