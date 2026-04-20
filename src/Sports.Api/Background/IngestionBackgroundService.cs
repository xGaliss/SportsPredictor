using Microsoft.Extensions.Options;
using Sports.Api.Models;
using Sports.Api.Services;

namespace Sports.Api.Background;

public class IngestionBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IngestionBackgroundService> _logger;
    private readonly IngestionOptions _options;

    public IngestionBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<IngestionOptions> options,
        ILogger<IngestionBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Ingesta desactivada por configuración.");
            return;
        }

        if (_options.RunOnStartup)
        {
            await RunCycleAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
            var predictionService = scope.ServiceProvider.GetRequiredService<PredictionService>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            await seedService.SeedTeamsAsync(cancellationToken);
            await seedService.SeedGamesByDateAsync(today, cancellationToken);
            await predictionService.CalculatePredictionsForDateAsync(today, cancellationToken);
            _logger.LogInformation("Ciclo de ingesta completado para {Date}", today);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo en ciclo de ingesta.");
        }
    }
}
