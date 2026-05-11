using Investe.Application.DTOs;
using Investe.Application.Interfaces.Services;
using Investe.Application.Mappings;
using Investe.Domain.Entities;
using Investe.Infrastructure.Persistence.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Investe.Application.Services
{
    public class PriceAlertService : IPriceAlertService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICoinPriceService _priceService;
        private readonly ILogger<PriceAlertService> _logger;

        public PriceAlertService(
            IUnitOfWork unitOfWork,
            ICoinPriceService priceService,
            ILogger<PriceAlertService> logger)
        {
            _unitOfWork = unitOfWork;
            _priceService = priceService;
            _logger = logger;
        }

        /// <summary>Creates a new price alert for the user.</summary>
        public async Task<AlertDto> CreateAlertAsync(Guid userId, CreateAlertDto dto)
        {
            var alert = new PriceAlert
            {
                UserId = userId,
                Symbol = dto.Symbol.ToUpperInvariant(),
                CoinId = dto.Symbol.ToLowerInvariant(),
                TargetPrice = dto.TargetPrice,
                Direction = dto.Direction,
                IsTriggered = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.PriceAlerts.AddAsync(alert);
            await _unitOfWork.CompleteAsync();

            _logger.LogInformation("Alert {AlertId} created for user {UserId}", alert.Id, userId);
            return alert.ToDto();
        }

        /// <summary>Returns all price alerts for the user.</summary>
        public async Task<IEnumerable<AlertDto>> GetUserAlertsAsync(Guid userId)
        {
            var alerts = await _unitOfWork.PriceAlerts.GetAlertsByUserIdAsync(userId);
            return alerts.Select(a => a.ToDto());
        }

        /// <summary>Deletes an alert owned by the user. Throws KeyNotFoundException or UnauthorizedAccessException.</summary>
        public async Task DeleteAlertAsync(Guid userId, Guid alertId)
        {
            var alert = await _unitOfWork.PriceAlerts.GetByIdAsync(alertId)
                ?? throw new KeyNotFoundException($"Alert {alertId} not found.");

            if (alert.UserId != userId)
                throw new UnauthorizedAccessException("Alert does not belong to this user.");

            await _unitOfWork.PriceAlerts.DeleteAsync(alert);
            await _unitOfWork.CompleteAsync();
        }

        /// <summary>Checks all active alerts against current prices and marks triggered ones.</summary>
        public async Task CheckAndTriggerAlertsAsync()
        {
            var activeAlerts = (await _unitOfWork.PriceAlerts.GetActiveAlertsAsync()).ToList();
            if (activeAlerts.Count == 0)
                return;

            // Fetch prices per distinct symbol (one API call per symbol)
            var symbols = activeAlerts.Select(a => a.Symbol).Distinct().ToList();
            var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var symbol in symbols)
                prices[symbol] = await _priceService.GetCurrentPriceAsync(symbol);

            var triggered = new List<PriceAlert>();
            foreach (var alert in activeAlerts)
            {
                if (!prices.TryGetValue(alert.Symbol, out var currentPrice) || currentPrice == 0)
                    continue;

                var shouldTrigger = alert.Direction == AlertDirection.Above
                    ? currentPrice >= alert.TargetPrice
                    : currentPrice <= alert.TargetPrice;

                if (shouldTrigger)
                {
                    alert.IsTriggered = true;
                    alert.TriggeredAt = DateTime.UtcNow;
                    await _unitOfWork.PriceAlerts.UpdateAsync(alert);
                    triggered.Add(alert);
                    _logger.LogInformation("Alert {AlertId} triggered for {Symbol} at {Price}", alert.Id, alert.Symbol, currentPrice);
                }
            }

            if (triggered.Count > 0)
                await _unitOfWork.CompleteAsync();
        }
    }
}
