using Microsoft.Extensions.Hosting;

namespace InvestPortfolio.Services
{
    // Service d'arrière-plan : simule une variation de prix toutes les 60 secondes
    public class PriceSimulationHostedService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<PriceSimulationHostedService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(1);

        public PriceSimulationHostedService(
            IServiceProvider services,
            ILogger<PriceSimulationHostedService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PriceSimulation: démarrage (intervalle = 1 min)");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Un scope par tick (DbContext a une durée de vie Scoped)
                    using var scope = _services.CreateScope();
                    var portfolioService = scope.ServiceProvider.GetRequiredService<IPortfolioService>();
                    await portfolioService.SimulateAllPricesAsync();
                    _logger.LogInformation("PriceSimulation: prix mis à jour à {time}", DateTime.Now);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "PriceSimulation: erreur pendant la simulation");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
