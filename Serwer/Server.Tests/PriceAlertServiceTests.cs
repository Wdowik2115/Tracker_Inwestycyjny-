using System;
using System.Collections.Generic;
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
        private static (Mock<IUnitOfWork>, Mock<IPriceAlertRepository>, Mock<ICoinPriceService>, PriceAlertService)
            BuildSut()
        {
            var uow = new Mock<IUnitOfWork>();
            var alertRepo = new Mock<IPriceAlertRepository>();
            var priceService = new Mock<ICoinPriceService>();
            var logger = new Mock<ILogger<PriceAlertService>>();

            uow.Setup(u => u.PriceAlerts).Returns(alertRepo.Object);

            var svc = new PriceAlertService(uow.Object, priceService.Object, logger.Object);
            return (uow, alertRepo, priceService, svc);
        }

        [Fact]
        public async Task CreateAlertAsync_PersistsAlertWithCorrectFields()
        {
            var userId = Guid.NewGuid();
            var (uow, alertRepo, _, svc) = BuildSut();

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

            await svc.CreateAlertAsync(userId, dto);

            Assert.NotNull(saved);
            Assert.Equal(userId, saved!.UserId);
            Assert.Equal("BTC", saved.Symbol);
            Assert.Equal(70000m, saved.TargetPrice);
            Assert.Equal(AlertDirection.Above, saved.Direction);
            Assert.False(saved.IsTriggered);
            uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckAndTriggerAlertsAsync_TriggersAboveAlert_WhenPriceExceedsThreshold()
        {
            var (uow, alertRepo, priceService, svc) = BuildSut();

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
    }
}
