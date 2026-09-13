using AiTrading.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AiTrading.Infrastructure.Persistence;

public sealed class DurableRiskMonitoringHostedService(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IConfiguration configuration,
    ILogger<DurableRiskMonitoringHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = configuration.GetValue<int?>("Monitoring:RiskIntervalSeconds") ?? 60;
        if (intervalSeconds <= 0)
            throw new InvalidOperationException("Monitoring:RiskIntervalSeconds must be positive.");

        var staleAfterSeconds = configuration.GetValue<int?>("Monitoring:RunStaleAfterSeconds") ?? Math.Max(intervalSeconds * 5, 300);
        if (staleAfterSeconds <= 0)
            throw new InvalidOperationException("Monitoring:RunStaleAfterSeconds must be positive.");

        var interval = TimeSpan.FromSeconds(intervalSeconds);
        var staleAfter = TimeSpan.FromSeconds(staleAfterSeconds);
        await RecoverStaleRunsAsync(staleAfter, stoppingToken);

        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<MonitoringRunService>().RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Durable risk monitoring iteration failed; continuing with the next interval.");
            }
        }
    }

    private async Task RecoverStaleRunsAsync(TimeSpan staleAfter, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var recovered = await scope.ServiceProvider.GetRequiredService<MonitoringRunService>().RecoverStaleRunsAsync(staleAfter, cancellationToken);
            if (recovered > 0)
                logger.LogWarning("Recovered {RecoveredMonitoringRuns} stale monitoring runs as failed during startup.", recovered);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to recover stale monitoring runs during startup; monitoring loop will continue.");
        }
    }
}
