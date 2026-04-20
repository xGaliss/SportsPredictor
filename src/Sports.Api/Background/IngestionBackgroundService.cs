using Microsoft.Extensions.Options;
using Sports.Api.Models;
using Sports.Api.Services;

namespace Sports.Api.Background;

public class IngestionBackgroundService : BackgroundService
{
    private readonly ILogger<IngestionBackgroundService> _logger;
    private readonly IngestionOptions _options;
    private readonly SyncOrchestrator _syncOrchestrator;

    public IngestionBackgroundService(
        SyncOrchestrator syncOrchestrator,
        IOptions<IngestionOptions> options,
        ILogger<IngestionBackgroundService> logger)
    {
        _syncOrchestrator = syncOrchestrator;
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
            await _syncOrchestrator.RunCycleAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fallo en ciclo de ingesta.");
        }
    }
}
