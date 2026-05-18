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
using Microsoft.Extensions.Logging;

namespace Serwer.Tests.Application.Services
{
    public class PriceAlertServiceTests
    {
        private static (Mock<IUnitOfWork>, Mock<IPriceAlertRepository>, Mock<ICoinPriceService>, Mock<ILogger<PriceAlertService>>, PriceAlertService)
            BuildSut()
        {
            var uow = new Mock<IUnitOfWork>();
            var alertRepo = new Mock<IPriceAlertRepository>();
            var priceService = new Mock<ICoinPriceService>();
            var logger = new Mock<ILogger<PriceAlertService>>();

            uow.Setup(u => u.PriceAlerts).Returns(alertRepo.Object);

            var svc = new PriceAlertService(uow.Object, priceService.Object, logger.Object);
            return (uow, alertRepo, priceService, logger, svc);
        }

        #region CreateAlertAsync Tests

        [Fact]
        public async Task CreateAlertAsync_PersistsAlertWithCorrectFields()
        {
            var userId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var dto = new CreateAlertDto
            {
                Symbol = "btc",
                TargetPrice = 70000m,
                Direction = AlertDirection.Above
            };

            PriceAlert? saved = null;
            alertRepo.Setup(r => r.AddAsync(It.IsAny<PriceAlert>()))
                .Callback<PriceAlert>(a => saved = a)
                .ReturnsAsync((PriceAlert a) => a);

            var result = await svc.CreateAlertAsync(userId, dto);

            Assert.NotNull(saved);
            Assert.Equal(userId, saved!.UserId);
            Assert.Equal("BTC", saved.Symbol);
            Assert.Equal("btc", saved.CoinId);
            Assert.Equal(70000m, saved.TargetPrice);
            Assert.Equal(AlertDirection.Above, saved.Direction);
            Assert.False(saved.IsTriggered);
            Assert.Null(saved.TriggeredAt);
            Assert.NotEqual(default, saved.CreatedAt);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAlertAsync_SymbolConvertedCorrectly()
        {
            var userId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var dto = new CreateAlertDto
            {
                Symbol = "eTh",
                TargetPrice = 3000m,
                Direction = AlertDirection.Below
            };

            PriceAlert? saved = null;
            alertRepo.Setup(r => r.AddAsync(It.IsAny<PriceAlert>()))
                .Callback<PriceAlert>(a => saved = a)
                .ReturnsAsync((PriceAlert a) => a);

            await svc.CreateAlertAsync(userId, dto);

            Assert.Equal("ETH", saved!.Symbol); // Uppercase
            Assert.Equal("eth", saved.CoinId);  // Lowercase
        }

        [Fact]
        public async Task CreateAlertAsync_ReturnsAlertDto()
        {
            var userId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var dto = new CreateAlertDto
            {
                Symbol = "SOL",
                TargetPrice = 150m,
                Direction = AlertDirection.Above
            };

            PriceAlert? saved = null;
            alertRepo.Setup(r => r.AddAsync(It.IsAny<PriceAlert>()))
                .Callback<PriceAlert>(a => saved = a)
                .ReturnsAsync((PriceAlert a) => a);

            var result = await svc.CreateAlertAsync(userId, dto);

            Assert.NotNull(result);
            Assert.Equal("SOL", result.Symbol);
            Assert.Equal(150m, result.TargetPrice);
            Assert.False(result.IsTriggered);
        }

        #endregion

        #region GetUserAlertsAsync Tests

        [Fact]
        public async Task GetUserAlertsAsync_ReturnsAllUserAlerts()
        {
            var userId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alerts = new List<PriceAlert>
            {
                new PriceAlert { Id = Guid.NewGuid(), UserId = userId, Symbol = "BTC", TargetPrice = 50000m, Direction = AlertDirection.Above },
                new PriceAlert { Id = Guid.NewGuid(), UserId = userId, Symbol = "ETH", TargetPrice = 3000m, Direction = AlertDirection.Below }
            };

            alertRepo.Setup(r => r.GetAlertsByUserIdAsync(userId)).ReturnsAsync(alerts);

            var result = await svc.GetUserAlertsAsync(userId);

            Assert.NotNull(result);
            var list = result.ToList();
            Assert.Equal(2, list.Count);
            Assert.Contains(list, a => a.Symbol == "BTC");
            Assert.Contains(list, a => a.Symbol == "ETH");
        }

        [Fact]
        public async Task GetUserAlertsAsync_ReturnsEmptyListWhenNoAlerts()
        {
            var userId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            alertRepo.Setup(r => r.GetAlertsByUserIdAsync(userId)).ReturnsAsync(new List<PriceAlert>());

            var result = await svc.GetUserAlertsAsync(userId);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region GetAlertByIdAsync Tests

        [Fact]
        public async Task GetAlertByIdAsync_ReturnsAlertWhenUserOwnsIt()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = userId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false,
                CreatedAt = DateTime.UtcNow
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            var result = await svc.GetAlertByIdAsync(userId, alertId);

            Assert.NotNull(result);
            Assert.Equal("BTC", result.Symbol);
            Assert.Equal(50000m, result.TargetPrice);
        }

        [Fact]
        public async Task GetAlertByIdAsync_ThrowsUnauthorizedAccessException_WhenUserDoesntOwnAlert()
        {
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = otherUserId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.GetAlertByIdAsync(userId, alertId));
        }

        [Fact]
        public async Task GetAlertByIdAsync_ThrowsKeyNotFoundException_WhenAlertDoesntExist()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync((PriceAlert?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.GetAlertByIdAsync(userId, alertId));
        }

        #endregion

        #region UpdateAlertAsync Tests

        [Fact]
        public async Task UpdateAlertAsync_UpdatesTargetPriceWhenProvided()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = userId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false,
                CreatedAt = DateTime.UtcNow
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            PriceAlert? updated = null;
            alertRepo.Setup(r => r.UpdateAsync(It.IsAny<PriceAlert>()))
                .Callback<PriceAlert>(a => updated = a)
                .Returns(Task.CompletedTask);

            var dto = new UpdateAlertDto { TargetPrice = 55000m };

            var result = await svc.UpdateAlertAsync(userId, alertId, dto);

            Assert.NotNull(updated);
            Assert.Equal(55000m, updated!.TargetPrice);
            Assert.Equal(AlertDirection.Above, updated.Direction); // Unchanged
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAlertAsync_UpdatesDirectionWhenProvided()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = userId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false,
                CreatedAt = DateTime.UtcNow
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            PriceAlert? updated = null;
            alertRepo.Setup(r => r.UpdateAsync(It.IsAny<PriceAlert>()))
                .Callback<PriceAlert>(a => updated = a)
                .Returns(Task.CompletedTask);

            var dto = new UpdateAlertDto { Direction = AlertDirection.Below };

            var result = await svc.UpdateAlertAsync(userId, alertId, dto);

            Assert.NotNull(updated);
            Assert.Equal(AlertDirection.Below, updated!.Direction);
            Assert.Equal(50000m, updated.TargetPrice); // Unchanged
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAlertAsync_UpdatesBothWhenBothProvided()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = userId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false,
                CreatedAt = DateTime.UtcNow
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            PriceAlert? updated = null;
            alertRepo.Setup(r => r.UpdateAsync(It.IsAny<PriceAlert>()))
                .Callback<PriceAlert>(a => updated = a)
                .Returns(Task.CompletedTask);

            var dto = new UpdateAlertDto { TargetPrice = 60000m, Direction = AlertDirection.Below };

            var result = await svc.UpdateAlertAsync(userId, alertId, dto);

            Assert.NotNull(updated);
            Assert.Equal(60000m, updated!.TargetPrice);
            Assert.Equal(AlertDirection.Below, updated.Direction);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAlertAsync_ThrowsUnauthorizedAccessException_WhenUserDoesntOwnAlert()
        {
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = otherUserId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            var dto = new UpdateAlertDto { TargetPrice = 55000m };

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.UpdateAlertAsync(userId, alertId, dto));
        }

        [Fact]
        public async Task UpdateAlertAsync_ThrowsInvalidOperationException_WhenAlertIsTriggered()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = userId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = true,
                TriggeredAt = DateTime.UtcNow
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            var dto = new UpdateAlertDto { TargetPrice = 55000m };

            await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpdateAlertAsync(userId, alertId, dto));
        }

        [Fact]
        public async Task UpdateAlertAsync_ThrowsArgumentException_WhenTargetPriceIsNegative()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = userId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            var dto = new UpdateAlertDto { TargetPrice = -100m };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateAlertAsync(userId, alertId, dto));
        }

        [Fact]
        public async Task UpdateAlertAsync_ThrowsArgumentException_WhenTargetPriceIsZero()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = userId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            var dto = new UpdateAlertDto { TargetPrice = 0m };

            await Assert.ThrowsAsync<ArgumentException>(() => svc.UpdateAlertAsync(userId, alertId, dto));
        }

        [Fact]
        public async Task UpdateAlertAsync_ThrowsKeyNotFoundException_WhenAlertDoesntExist()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync((PriceAlert?)null);

            var dto = new UpdateAlertDto { TargetPrice = 55000m };

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.UpdateAlertAsync(userId, alertId, dto));
        }

        #endregion

        #region DeleteAlertAsync Tests

        [Fact]
        public async Task DeleteAlertAsync_DeletesAlertWhenUserOwnsIt()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = userId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            await svc.DeleteAlertAsync(userId, alertId);

            alertRepo.Verify(r => r.DeleteAsync(alert), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAlertAsync_ThrowsUnauthorizedAccessException_WhenUserDoesntOwnAlert()
        {
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = alertId,
                UserId = otherUserId,
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above
            };

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync(alert);

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => svc.DeleteAlertAsync(userId, alertId));
        }

        [Fact]
        public async Task DeleteAlertAsync_ThrowsKeyNotFoundException_WhenAlertDoesntExist()
        {
            var userId = Guid.NewGuid();
            var alertId = Guid.NewGuid();
            var (uow, alertRepo, _, _, svc) = BuildSut();

            alertRepo.Setup(r => r.GetByIdAsync(alertId)).ReturnsAsync((PriceAlert?)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.DeleteAlertAsync(userId, alertId));
        }

        #endregion

        #region CheckAndTriggerAlertsAsync Tests

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_TriggersAboveAlert_WhenPriceExceedsThreshold()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert });
            priceService.Setup(p => p.GetCurrentPriceAsync("BTC")).ReturnsAsync(65000m);

            await svc.CheckAndTriggerAlertsAsync();

            Assert.True(alert.IsTriggered);
            Assert.NotNull(alert.TriggeredAt);
            alertRepo.Verify(r => r.UpdateAsync(It.Is<PriceAlert>(a => a.IsTriggered)), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_TriggersAboveAlert_WhenPriceEqualsThreshold()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert });
            priceService.Setup(p => p.GetCurrentPriceAsync("BTC")).ReturnsAsync(50000m);

            await svc.CheckAndTriggerAlertsAsync();

            Assert.True(alert.IsTriggered);
            alertRepo.Verify(r => r.UpdateAsync(It.IsAny<PriceAlert>()), Times.Once);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_DoesNotTriggerAboveAlert_WhenPriceBelowThreshold()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert });
            priceService.Setup(p => p.GetCurrentPriceAsync("BTC")).ReturnsAsync(45000m);

            await svc.CheckAndTriggerAlertsAsync();

            Assert.False(alert.IsTriggered);
            alertRepo.Verify(r => r.UpdateAsync(It.IsAny<PriceAlert>()), Times.Never);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_TriggersBelowAlert_WhenPriceBelowThreshold()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "ETH",
                TargetPrice = 3000m,
                Direction = AlertDirection.Below,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert });
            priceService.Setup(p => p.GetCurrentPriceAsync("ETH")).ReturnsAsync(2500m);

            await svc.CheckAndTriggerAlertsAsync();

            Assert.True(alert.IsTriggered);
            Assert.NotNull(alert.TriggeredAt);
            alertRepo.Verify(r => r.UpdateAsync(It.IsAny<PriceAlert>()), Times.Once);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_TriggersBelowAlert_WhenPriceEqualsThreshold()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "ETH",
                TargetPrice = 3000m,
                Direction = AlertDirection.Below,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert });
            priceService.Setup(p => p.GetCurrentPriceAsync("ETH")).ReturnsAsync(3000m);

            await svc.CheckAndTriggerAlertsAsync();

            Assert.True(alert.IsTriggered);
            alertRepo.Verify(r => r.UpdateAsync(It.IsAny<PriceAlert>()), Times.Once);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_DoesNotTriggerBelowAlert_WhenPriceAboveThreshold()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "ETH",
                TargetPrice = 3000m,
                Direction = AlertDirection.Below,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert });
            priceService.Setup(p => p.GetCurrentPriceAsync("ETH")).ReturnsAsync(3500m);

            await svc.CheckAndTriggerAlertsAsync();

            Assert.False(alert.IsTriggered);
            alertRepo.Verify(r => r.UpdateAsync(It.IsAny<PriceAlert>()), Times.Never);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_ProcessesMultipleAlerts()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert1 = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            var alert2 = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "BTC",
                TargetPrice = 40000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert1, alert2 });
            priceService.Setup(p => p.GetCurrentPriceAsync("BTC")).ReturnsAsync(45000m);

            await svc.CheckAndTriggerAlertsAsync();

            Assert.False(alert1.IsTriggered); // 45000 < 50000
            Assert.True(alert2.IsTriggered);  // 45000 >= 40000
            alertRepo.Verify(r => r.UpdateAsync(It.IsAny<PriceAlert>()), Times.Once);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_BatchesPriceRequests()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert1 = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            var alert2 = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "BTC",
                TargetPrice = 40000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert1, alert2 });
            priceService.Setup(p => p.GetCurrentPriceAsync("BTC")).ReturnsAsync(45000m);

            await svc.CheckAndTriggerAlertsAsync();

            // Should only call API once for "BTC" even though there are 2 alerts
            priceService.Verify(p => p.GetCurrentPriceAsync("BTC"), Times.Once);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_SkipsWhenNoActiveAlerts()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert>());

            await svc.CheckAndTriggerAlertsAsync();

            priceService.Verify(p => p.GetCurrentPriceAsync(It.IsAny<string>()), Times.Never);
            uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_SkipsAlertWhenPriceIsZero()
        {
            var (uow, alertRepo, priceService, _, svc) = BuildSut();

            var alert = new PriceAlert
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Symbol = "BTC",
                TargetPrice = 50000m,
                Direction = AlertDirection.Above,
                IsTriggered = false
            };

            alertRepo.Setup(r => r.GetActiveAlertsAsync()).ReturnsAsync(new List<PriceAlert> { alert });
            priceService.Setup(p => p.GetCurrentPriceAsync("BTC")).ReturnsAsync(0m);

            await svc.CheckAndTriggerAlertsAsync();

            Assert.False(alert.IsTriggered);
            alertRepo.Verify(r => r.UpdateAsync(It.IsAny<PriceAlert>()), Times.Never);
        }

        #endregion
    }
}

