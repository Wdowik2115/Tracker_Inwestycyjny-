using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;
using Investe.Application.Services;
using Investe.Application.Interfaces.Services;
using Investe.Application.DTOs;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Investe.Infrastructure.Persistence.Repositories;
using Investe.Domain.Entities;

namespace Serwer.Tests.Application.Services
{
    public class WatchlistServiceTests
    {
        private static (Mock<IUnitOfWork>, Mock<IWatchlistRepository>, Mock<ICoinPriceService>, WatchlistService)
            BuildSut()
        {
            var uow = new Mock<IUnitOfWork>();
            var watchlistRepo = new Mock<IWatchlistRepository>();
            var priceService = new Mock<ICoinPriceService>();

            uow.Setup(u => u.Watchlist).Returns(watchlistRepo.Object);

            var svc = new WatchlistService(uow.Object, priceService.Object);
            return (uow, watchlistRepo, priceService, svc);
        }

        [Fact]
        public async Task GetWatchlistAsync_ReturnsMappedItemsWithPrices()
        {
            var userId = Guid.NewGuid();
            var (uow, watchlistRepo, priceService, svc) = BuildSut();

            var items = new List<WatchlistItem>
            {
                new WatchlistItem { Id = Guid.NewGuid(), UserId = userId, CoinId = "bitcoin", Symbol = "BTC", AddedAt = DateTime.UtcNow },
                new WatchlistItem { Id = Guid.NewGuid(), UserId = userId, CoinId = "ethereum", Symbol = "ETH", AddedAt = DateTime.UtcNow }
            };

            var prices = new Dictionary<string, decimal>
            {
                { "BTC", 60000m },
                { "ETH", 3000m }
            };

            watchlistRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(items);
            priceService.Setup(p => p.GetCurrentPricesAsync(It.IsAny<IEnumerable<string>>())).ReturnsAsync(prices);

            var result = await svc.GetWatchlistAsync(userId);

            Assert.Equal(2, result.Count());
            Assert.Contains(result, i => i.Symbol == "BTC" && i.CurrentPrice == 60000m);
            Assert.Contains(result, i => i.Symbol == "ETH" && i.CurrentPrice == 3000m);
        }

        [Fact]
        public async Task AddToWatchlistAsync_AddsNewItemWhenNotExists()
        {
            var userId = Guid.NewGuid();
            var (uow, watchlistRepo, priceService, svc) = BuildSut();

            var dto = new AddToWatchlistDto { CoinId = "solana", Symbol = "SOL" };

            watchlistRepo.Setup(r => r.GetByUserAndCoinAsync(userId, "solana")).ReturnsAsync((WatchlistItem?)null);
            priceService.Setup(p => p.GetCurrentPriceAsync("SOL")).ReturnsAsync(150m);

            var result = await svc.AddToWatchlistAsync(userId, dto);

            watchlistRepo.Verify(r => r.AddAsync(It.Is<WatchlistItem>(i => i.CoinId == "solana" && i.UserId == userId)), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
            Assert.Equal("SOL", result.Symbol);
            Assert.Equal(150m, result.CurrentPrice);
        }

        [Fact]
        public async Task AddToWatchlistAsync_ReturnsExistingWhenAlreadyOnWatchlist()
        {
            var userId = Guid.NewGuid();
            var (uow, watchlistRepo, priceService, svc) = BuildSut();

            var existing = new WatchlistItem { Id = Guid.NewGuid(), UserId = userId, CoinId = "bitcoin", Symbol = "BTC" };
            var dto = new AddToWatchlistDto { CoinId = "bitcoin", Symbol = "BTC" };

            watchlistRepo.Setup(r => r.GetByUserAndCoinAsync(userId, "bitcoin")).ReturnsAsync(existing);
            priceService.Setup(p => p.GetCurrentPriceAsync("BTC")).ReturnsAsync(60000m);

            var result = await svc.AddToWatchlistAsync(userId, dto);

            watchlistRepo.Verify(r => r.AddAsync(It.IsAny<WatchlistItem>()), Times.Never);
            Assert.Equal(existing.Id, result.Id);
        }

        [Fact]
        public async Task RemoveFromWatchlistAsync_DeletesItemWhenOwnedByUser()
        {
            var userId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var (uow, watchlistRepo, _, svc) = BuildSut();

            var item = new WatchlistItem { Id = itemId, UserId = userId };
            watchlistRepo.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);

            await svc.RemoveFromWatchlistAsync(userId, itemId);

            watchlistRepo.Verify(r => r.DeleteAsync(item), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task RemoveFromWatchlistAsync_DoesNothingWhenNotOwnedByUser()
        {
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var itemId = Guid.NewGuid();
            var (uow, watchlistRepo, _, svc) = BuildSut();

            var item = new WatchlistItem { Id = itemId, UserId = otherUserId };
            watchlistRepo.Setup(r => r.GetByIdAsync(itemId)).ReturnsAsync(item);

            await svc.RemoveFromWatchlistAsync(userId, itemId);

            watchlistRepo.Verify(r => r.DeleteAsync(It.IsAny<WatchlistItem>()), Times.Never);
        }

        [Fact]
        public async Task IsOnWatchlistAsync_ReturnsTrueWhenExists()
        {
            var userId = Guid.NewGuid();
            var (uow, watchlistRepo, _, svc) = BuildSut();

            watchlistRepo.Setup(r => r.GetByUserAndCoinAsync(userId, "bitcoin")).ReturnsAsync(new WatchlistItem());

            var result = await svc.IsOnWatchlistAsync(userId, "bitcoin");

            Assert.True(result);
        }

        [Fact]
        public async Task IsOnWatchlistAsync_ReturnsFalseWhenNotExists()
        {
            var userId = Guid.NewGuid();
            var (uow, watchlistRepo, _, svc) = BuildSut();

            watchlistRepo.Setup(r => r.GetByUserAndCoinAsync(userId, "bitcoin")).ReturnsAsync((WatchlistItem?)null);

            var result = await svc.IsOnWatchlistAsync(userId, "bitcoin");

            Assert.False(result);
        }
    }
}
