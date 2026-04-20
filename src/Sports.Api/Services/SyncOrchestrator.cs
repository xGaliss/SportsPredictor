using Sports.Api.Helpers;

namespace Sports.Api.Services;

public class SyncOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncOrchestrator> _logger;
    private static DateTime? _lastSuccessfulSyncUtc;

    public SyncOrchestrator(IServiceProvider serviceProvider, ILogger<SyncOrchestrator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public DateTime? LastSuccessfulSyncUtc => _lastSuccessfulSyncUtc;

    public async Task RunCycleAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var seedService = scope.ServiceProvider.GetRequiredService<SeedService>();
        var predictionService = scope.ServiceProvider.GetRequiredService<PredictionService>();

        var today = DateRangeHelper.GetCurrentLocalDate();
        await seedService.SeedTeamsAsync(cancellationToken);
        await seedService.SeedGamesByDateAsync(today, cancellationToken);
        await predictionService.CalculatePredictionsForDateAsync(today, cancellationToken);

        _lastSuccessfulSyncUtc = DateTime.UtcNow;
        _logger.LogInformation("Ciclo de sync completado para {Date}", today);
    }
}
