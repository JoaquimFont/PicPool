using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PicPool.Infrastructure.Services;

namespace PicPool.Api.BackgroundServices
{
    public class MantenimentSalesBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MantenimentSalesBackgroundService> _logger;

        private readonly TimeSpan _interval = TimeSpan.FromHours(12);

        public MantenimentSalesBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MantenimentSalesBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var servei = scope.ServiceProvider
                        .GetRequiredService<ServeiMantenimentSales>();

                    await servei.ExecutarMantenimentAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // L'aplicació s'està aturant.
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error executant el manteniment automàtic de sales."
                    );
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}