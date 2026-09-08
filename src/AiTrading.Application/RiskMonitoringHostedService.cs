namespace AiTrading.Application;

public sealed class RiskMonitoringHostedService(ILogger<RiskMonitoringHostedService> logger, RiskMonitor monitor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = Guid.NewGuid().ToString("N")
            });

            try
            {
                await monitor.CheckOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background risk monitoring iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
