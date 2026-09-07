using AiTrading.Application;

namespace AiTrading.Worker;

public sealed class MonitoringWorker(ILogger<MonitoringWorker> logger, RiskMonitor monitor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(30);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await monitor.CheckOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background risk monitoring iteration failed.");
            }
            await Task.Delay(interval, stoppingToken);
        }
    }
}
