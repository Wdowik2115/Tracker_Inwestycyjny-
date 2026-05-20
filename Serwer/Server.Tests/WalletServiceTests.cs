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
using Investe.Application.DTOs;
using Investe.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Serwer.Tests.Application.Services
{
    public class WalletServiceTests
    {
        private static (
            Mock<IUnitOfWork> uow,
            Mock<IWalletRepository> walletRepo,
            Mock<IAssetRepository> assetRepo,
            Mock<ITransactionRepository> transRepo,
            Mock<ICoinPriceService> priceService,
            WalletService sut)
            BuildSut()
        {
            var uow = new Mock<IUnitOfWork>();
            var walletRepo = new Mock<IWalletRepository>();
            var assetRepo = new Mock<IAssetRepository>();
            var transRepo = new Mock<ITransactionRepository>();
            var priceService = new Mock<ICoinPriceService>();
            var logger = new Mock<ILogger<WalletService>>();

            uow.Setup(u => u.Wallets).Returns(walletRepo.Object);
            uow.Setup(u => u.Assets).Returns(assetRepo.Object);
            uow.Setup(u => u.Transactions).Returns(transRepo.Object);

            var sut = new WalletService(uow.Object, priceService.Object, logger.Object);
            return (uow, walletRepo, assetRepo, transRepo, priceService, sut);
        }

        // ── CreateWalletAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task CreateWalletAsync_PersistsWalletAndReturnsDto()
        {
            var (uow, walletRepo, _, _, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var dto = new CreateWalletDto { Name = "My Wallet", Description = "desc" };

            Wallet? added = null;
            walletRepo.Setup(r => r.AddAsync(It.IsAny<Wallet>()))
                .Callback<Wallet>(w => added = w)
                .Returns(Task.CompletedTask);

            var result = await sut.CreateWalletAsync(userId, dto);

            Assert.NotNull(added);
            Assert.Equal(userId, added!.UserId);
            Assert.Equal("My Wallet", added.Name);
            Assert.Equal("desc", added.Description);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
            Assert.Equal("My Wallet", result.Name);
            Assert.Equal("desc", result.Description);
        }

        [Fact]
        public async Task CreateWalletAsync_NullDescription_DefaultsToEmptyString()
        {
            var (uow, walletRepo, _, _, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var dto = new CreateWalletDto { Name = "Wallet", Description = null };

            Wallet? added = null;
            walletRepo.Setup(r => r.AddAsync(It.IsAny<Wallet>()))
                .Callback<Wallet>(w => added = w)
                .Returns(Task.CompletedTask);

            await sut.CreateWalletAsync(userId, dto);

            Assert.Equal(string.Empty, added!.Description);
        }

        // ── GetUserWalletsAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task GetUserWalletsAsync_NoWallets_ReturnsEmpty()
        {
            var (_, walletRepo, assetRepo, _, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();

            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId))
                .ReturnsAsync(new List<Wallet>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());

            var result = await sut.GetUserWalletsAsync(userId);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetUserWalletsAsync_CalculatesTotalValueAndPnl()
        {
            var (_, walletRepo, assetRepo, _, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();

            var wallet = new Wallet { Id = walletId, UserId = userId, Name = "W1" };
            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId))
                .ReturnsAsync(new List<Wallet> { wallet });

            // 2 BTC bought at avg 30_000, current price 40_000
            var asset = new Asset { WalletId = walletId, Symbol = "BTC", Quantity = 2m, AverageBuyPrice = 30_000m };
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset> { asset });

            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    { ["BTC"] = 40_000m });

            var results = (await sut.GetUserWalletsAsync(userId)).ToList();

            Assert.Single(results);
            var dto = results[0];
            Assert.Equal(80_000m, dto.TotalValue);        // 2 * 40_000
            Assert.Equal(20_000m, dto.Pnl);               // 80_000 - 60_000
            Assert.Equal(1, dto.AssetCount);
            // PnlPercent ≈ 33.33...
            Assert.True(dto.PnlPercent > 33m && dto.PnlPercent < 34m);
        }

        [Fact]
        public async Task GetUserWalletsAsync_ZeroCostBasis_PnlPercentIsZero()
        {
            var (_, walletRepo, assetRepo, _, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();

            var wallet = new Wallet { Id = walletId, UserId = userId, Name = "W" };
            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId))
                .ReturnsAsync(new List<Wallet> { wallet });

            var asset = new Asset { WalletId = walletId, Symbol = "ETH", Quantity = 1m, AverageBuyPrice = 0m };
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset> { asset });

            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    { ["ETH"] = 2_000m });

            var results = (await sut.GetUserWalletsAsync(userId)).ToList();

            Assert.Equal(0m, results[0].PnlPercent);
        }

        [Fact]
        public async Task GetUserWalletsAsync_BatchesPriceRequestAcrossAllWallets()
        {
            var (_, walletRepo, assetRepo, _, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var w1 = Guid.NewGuid();
            var w2 = Guid.NewGuid();

            walletRepo.Setup(r => r.GetWalletsByUserIdAsync(userId))
                .ReturnsAsync(new List<Wallet>
                {
                    new() { Id = w1, UserId = userId, Name = "W1" },
                    new() { Id = w2, UserId = userId, Name = "W2" }
                });

            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(w1))
                .ReturnsAsync(new List<Asset> { new() { WalletId = w1, Symbol = "BTC", Quantity = 1m } });
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(w2))
                .ReturnsAsync(new List<Asset> { new() { WalletId = w2, Symbol = "ETH", Quantity = 1m } });

            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());

            await sut.GetUserWalletsAsync(userId);

            // Price service must be called exactly once (batch), not once per wallet
            priceService.Verify(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()), Times.Once);
        }

        // ── GetWalletDetailsAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetWalletDetailsAsync_WalletNotFound_ThrowsKeyNotFoundException()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync((Wallet?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.GetWalletDetailsAsync(userId, walletId));
        }

        [Fact]
        public async Task GetWalletDetailsAsync_WrongUser_ThrowsUnauthorizedAccessException()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            var walletId = Guid.NewGuid();
            var wallet = new Wallet { Id = walletId, UserId = Guid.NewGuid() };

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.GetWalletDetailsAsync(Guid.NewGuid(), walletId));
        }

        [Fact]
        public async Task GetWalletDetailsAsync_NoAssets_ReturnsDtoWithZeros()
        {
            var (_, walletRepo, assetRepo, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var wallet = new Wallet { Id = walletId, UserId = userId, Name = "Empty" };

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset>());
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());

            var result = await sut.GetWalletDetailsAsync(userId, walletId);

            Assert.Equal(0m, result.TotalValue);
            Assert.Equal(0m, result.Pnl);
            Assert.Equal(0m, result.RealizedPnl);
            Assert.Empty(result.Assets);
        }

        [Fact]
        public async Task GetWalletDetailsAsync_CalculatesPositionPnlCorrectly()
        {
            var (_, walletRepo, assetRepo, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var wallet = new Wallet { Id = walletId, UserId = userId, Name = "W" };

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);

            var asset = new Asset
            {
                WalletId = walletId, Symbol = "BTC", Name = "Bitcoin",
                Quantity = 1m, AverageBuyPrice = 50_000m
            };
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset> { asset });

            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
                    { ["BTC"] = 60_000m });

            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>());

            var result = await sut.GetWalletDetailsAsync(userId, walletId);

            var pos = Assert.Single(result.Assets);
            Assert.Equal(60_000m, pos.CurrentPrice);
            Assert.Equal(60_000m, pos.Value);
            Assert.Equal(10_000m, pos.Pnl);
            Assert.Equal(20m, pos.PnlPercent);
            Assert.Equal(60_000m, result.TotalValue);
            Assert.Equal(10_000m, result.Pnl);
        }

        [Fact]
        public async Task GetWalletDetailsAsync_IncludesRealizedPnlFromTransactions()
        {
            var (_, walletRepo, assetRepo, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var wallet = new Wallet { Id = walletId, UserId = userId, Name = "W" };
            var t0 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());

            // Buy 2 BTC @ 10, sell 2 BTC @ 15 → realized = (15-10)*2 = 10
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>
                {
                    new() { Symbol = "BTC", Type = TransactionType.Buy,  Quantity = 2m, PriceAtTime = 10m, ExecutedAt = t0 },
                    new() { Symbol = "BTC", Type = TransactionType.Sell, Quantity = 2m, PriceAtTime = 15m, ExecutedAt = t0.AddDays(1) }
                });

            var result = await sut.GetWalletDetailsAsync(userId, walletId);

            Assert.Equal(10m, result.RealizedPnl);
        }

        // ── UpdateWalletAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task UpdateWalletAsync_WalletNotFound_ThrowsKeyNotFoundException()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            walletRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Wallet?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.UpdateWalletAsync(Guid.NewGuid(), Guid.NewGuid(), new UpdateWalletDto { Name = "X" }));
        }

        [Fact]
        public async Task UpdateWalletAsync_WrongUser_ThrowsUnauthorizedAccessException()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            var walletId = Guid.NewGuid();
            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = Guid.NewGuid() });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.UpdateWalletAsync(Guid.NewGuid(), walletId, new UpdateWalletDto { Name = "X" }));
        }

        [Fact]
        public async Task UpdateWalletAsync_UpdatesNameAndDescriptionAndCompletes()
        {
            var (uow, walletRepo, _, _, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var wallet = new Wallet { Id = walletId, UserId = userId, Name = "Old", Description = "old desc" };

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);
            walletRepo.Setup(r => r.UpdateAsync(It.IsAny<Wallet>())).Returns(Task.CompletedTask);

            var dto = new UpdateWalletDto { Name = "New Name", Description = "new desc" };
            var result = await sut.UpdateWalletAsync(userId, walletId, dto);

            Assert.Equal("New Name", wallet.Name);
            Assert.Equal("new desc", wallet.Description);
            walletRepo.Verify(r => r.UpdateAsync(wallet), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
            Assert.Equal("New Name", result.Name);
        }

        [Fact]
        public async Task UpdateWalletAsync_NullDescription_DefaultsToEmptyString()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var wallet = new Wallet { Id = walletId, UserId = userId, Name = "W", Description = "old" };

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);
            walletRepo.Setup(r => r.UpdateAsync(It.IsAny<Wallet>())).Returns(Task.CompletedTask);

            await sut.UpdateWalletAsync(userId, walletId, new UpdateWalletDto { Name = "W", Description = null });

            Assert.Equal(string.Empty, wallet.Description);
        }

        // ── DeleteWalletAsync ─────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteWalletAsync_WalletNotFound_ThrowsKeyNotFoundException()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            walletRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Wallet?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.DeleteWalletAsync(Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteWalletAsync_WrongUser_ThrowsUnauthorizedAccessException()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            var walletId = Guid.NewGuid();
            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = Guid.NewGuid() });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.DeleteWalletAsync(Guid.NewGuid(), walletId));
        }

        [Fact]
        public async Task DeleteWalletAsync_CallsDeleteAndCompletes()
        {
            var (uow, walletRepo, _, _, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var wallet = new Wallet { Id = walletId, UserId = userId };

            walletRepo.Setup(r => r.GetByIdAsync(walletId)).ReturnsAsync(wallet);
            walletRepo.Setup(r => r.DeleteAsync(wallet)).Returns(Task.CompletedTask);

            await sut.DeleteWalletAsync(userId, walletId);

            walletRepo.Verify(r => r.DeleteAsync(wallet), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        // ── GetWalletHistoryAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetWalletHistoryAsync_WalletNotFound_ThrowsKeyNotFoundException()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            walletRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Wallet?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => sut.GetWalletHistoryAsync(Guid.NewGuid(), Guid.NewGuid(), 30));
        }

        [Fact]
        public async Task GetWalletHistoryAsync_WrongUser_ThrowsUnauthorizedAccessException()
        {
            var (_, walletRepo, _, _, _, sut) = BuildSut();
            var walletId = Guid.NewGuid();
            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = Guid.NewGuid() });

            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => sut.GetWalletHistoryAsync(Guid.NewGuid(), walletId, 30));
        }

        [Fact]
        public async Task GetWalletHistoryAsync_NoTransactions_ReturnsEmptyPoints()
        {
            var (_, walletRepo, _, transRepo, _, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId });
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>());

            var result = await sut.GetWalletHistoryAsync(userId, walletId, 30);

            Assert.Equal(walletId, result.WalletId);
            Assert.Empty(result.Points);
        }

        [Fact]
        public async Task GetWalletHistoryAsync_ReturnsPointsWithCorrectValues()
        {
            var (_, walletRepo, _, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId });

            var buyDate = DateTime.UtcNow.Date.AddDays(-5);

            // Buy 2 BTC five days ago
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>
                {
                    new() { Symbol = "BTC", Type = TransactionType.Buy, Quantity = 2m, PriceAtTime = 1m, ExecutedAt = buyDate }
                });

            // Price history: one point on buyDate at 100
            priceService.Setup(p => p.GetPriceHistoryAsync("BTC", It.IsAny<int>()))
                .ReturnsAsync(new List<HistoryPointDto>
                {
                    new() { Date = buyDate, Value = 100m }
                });

            var result = await sut.GetWalletHistoryAsync(userId, walletId, 30);

            Assert.Single(result.Points);
            Assert.Equal(200m, result.Points[0].Value); // 2 BTC * 100
        }

        [Fact]
        public async Task GetWalletHistoryAsync_ExcludesPointsBeforeBuyDate()
        {
            var (_, walletRepo, _, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId });

            var buyDate = DateTime.UtcNow.Date.AddDays(-2);
            var beforeBuy = buyDate.AddDays(-1);

            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>
                {
                    new() { Symbol = "BTC", Type = TransactionType.Buy, Quantity = 1m, PriceAtTime = 1m, ExecutedAt = buyDate }
                });

            priceService.Setup(p => p.GetPriceHistoryAsync("BTC", It.IsAny<int>()))
                .ReturnsAsync(new List<HistoryPointDto>
                {
                    new() { Date = beforeBuy, Value = 90m },
                    new() { Date = buyDate,   Value = 100m }
                });

            var result = await sut.GetWalletHistoryAsync(userId, walletId, 30);

            // beforeBuy point: no holdings yet → value = 0 → filtered out
            Assert.Single(result.Points);
            Assert.Equal(buyDate, result.Points[0].Date);
        }

        [Fact]
        public async Task GetWalletHistoryAsync_SellReducesHoldingsOnSubsequentDates()
        {
            var (_, walletRepo, _, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId });

            var day1 = DateTime.UtcNow.Date.AddDays(-3);
            var day2 = day1.AddDays(1);
            var day3 = day2.AddDays(1);

            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>
                {
                    new() { Symbol = "BTC", Type = TransactionType.Buy,  Quantity = 2m, PriceAtTime = 1m, ExecutedAt = day1 },
                    new() { Symbol = "BTC", Type = TransactionType.Sell, Quantity = 1m, PriceAtTime = 1m, ExecutedAt = day2 }
                });

            priceService.Setup(p => p.GetPriceHistoryAsync("BTC", It.IsAny<int>()))
                .ReturnsAsync(new List<HistoryPointDto>
                {
                    new() { Date = day1, Value = 100m },
                    new() { Date = day2, Value = 100m },
                    new() { Date = day3, Value = 100m }
                });

            var result = await sut.GetWalletHistoryAsync(userId, walletId, 30);

            var points = result.Points.OrderBy(p => p.Date).ToList();
            Assert.Equal(200m, points[0].Value); // day1: 2 BTC * 100
            Assert.Equal(100m, points[1].Value); // day2: 1 BTC * 100 (after sell)
            Assert.Equal(100m, points[2].Value); // day3: 1 BTC still
        }

        // ── CalculateRealizedPnl (via GetWalletDetailsAsync) ─────────────────────

        [Fact]
        public async Task RealizedPnl_MultipleBuysWeightedAverageThenSell_ComputesCorrectly()
        {
            var (_, walletRepo, assetRepo, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var t0 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId, Name = "W" });
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());

            // Buy 1 @ 10, Buy 1 @ 30 → avg cost = 20
            // Sell 1 @ 25 → realized = (25 - 20) * 1 = 5
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>
                {
                    new() { Symbol = "ETH", Type = TransactionType.Buy,  Quantity = 1m, PriceAtTime = 10m, ExecutedAt = t0 },
                    new() { Symbol = "ETH", Type = TransactionType.Buy,  Quantity = 1m, PriceAtTime = 30m, ExecutedAt = t0.AddDays(1) },
                    new() { Symbol = "ETH", Type = TransactionType.Sell, Quantity = 1m, PriceAtTime = 25m, ExecutedAt = t0.AddDays(2) }
                });

            var result = await sut.GetWalletDetailsAsync(userId, walletId);

            Assert.Equal(5m, result.RealizedPnl);
        }

        [Fact]
        public async Task RealizedPnl_MultipleSymbols_SumsRealizedAcrossSymbols()
        {
            var (_, walletRepo, assetRepo, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var t0 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId, Name = "W" });
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());

            // BTC: buy 1 @ 10, sell 1 @ 20 → +10
            // ETH: buy 1 @ 5,  sell 1 @ 3  → -2
            // Total realized = 8
            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>
                {
                    new() { Symbol = "BTC", Type = TransactionType.Buy,  Quantity = 1m, PriceAtTime = 10m, ExecutedAt = t0 },
                    new() { Symbol = "BTC", Type = TransactionType.Sell, Quantity = 1m, PriceAtTime = 20m, ExecutedAt = t0.AddDays(1) },
                    new() { Symbol = "ETH", Type = TransactionType.Buy,  Quantity = 1m, PriceAtTime = 5m,  ExecutedAt = t0 },
                    new() { Symbol = "ETH", Type = TransactionType.Sell, Quantity = 1m, PriceAtTime = 3m,  ExecutedAt = t0.AddDays(1) }
                });

            var result = await sut.GetWalletDetailsAsync(userId, walletId);

            Assert.Equal(8m, result.RealizedPnl);
        }

        [Fact]
        public async Task RealizedPnl_NoSells_IsZero()
        {
            var (_, walletRepo, assetRepo, transRepo, priceService, sut) = BuildSut();
            var userId = Guid.NewGuid();
            var walletId = Guid.NewGuid();
            var t0 = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            walletRepo.Setup(r => r.GetByIdAsync(walletId))
                .ReturnsAsync(new Wallet { Id = walletId, UserId = userId, Name = "W" });
            assetRepo.Setup(r => r.GetAssetsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Asset>());
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>()))
                .ReturnsAsync(new Dictionary<string, decimal>());

            transRepo.Setup(r => r.GetTransactionsByWalletIdAsync(walletId))
                .ReturnsAsync(new List<Transaction>
                {
                    new() { Symbol = "BTC", Type = TransactionType.Buy, Quantity = 3m, PriceAtTime = 20m, ExecutedAt = t0 }
                });

            var result = await sut.GetWalletDetailsAsync(userId, walletId);

            Assert.Equal(0m, result.RealizedPnl);
        }
    }
}
