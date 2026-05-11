using Investe.Application.Interfaces.Services;

namespace Serwer.BackgroundServices
{
    public class PriceAlertBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PriceAlertBackgroundService> _logger;

        public PriceAlertBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PriceAlertBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PriceAlertBackgroundService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var svc = scope.ServiceProvider.GetRequiredService<IPriceAlertService>();
                    await svc.CheckAndTriggerAlertsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking price alerts");
                }

                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
