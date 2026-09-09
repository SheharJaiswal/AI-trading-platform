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

        var interval = TimeSpan.FromSeconds(intervalSeconds);
        using var timer = new PeriodicTimer(interval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<DurableRiskMonitor>().CheckOnceAsync(stoppingToken);
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
}
