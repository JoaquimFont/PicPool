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

        /// <summary>
        /// Explicació: inicialitza el servei en segon pla encarregat del manteniment periòdic de sales.
        /// Precondicions: el contenidor ha de proporcionar una factoria d'scopes i un logger vàlids.
        /// Postcondicions: el servei queda preparat per crear scopes i executar el manteniment programat.
        /// </summary>
        public MantenimentSalesBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MantenimentSalesBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Explicació: executa el manteniment de sales de manera periòdica fins que l'aplicació rep una ordre d'aturada.
        /// Precondicions: el token de cancel·lació representa el cicle de vida del servei allotjat.
        /// Postcondicions: mentre no es cancel·li, s'executa el manteniment i es registren els errors sense aturar l'aplicació.
        /// </summary>
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
