using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Worker;

public sealed class MonitoringWorker(ILogger<MonitoringWorker> logger, IMarketDataProvider marketData, IPortfolio portfolio) : BackgroundService
{
    private readonly HashSet<string> _emittedAlertKeys = [];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(30);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPositionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background risk monitoring iteration failed.");
            }
            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CheckPositionsAsync(CancellationToken cancellationToken)
    {
        foreach (var position in portfolio.Snapshot().Positions)
        {
            if (position.StopLoss is null) continue;
            var quote = await marketData.GetQuoteAsync(position.Symbol, cancellationToken);
            if (quote.Close <= position.StopLoss)
            {
                var key = $"{position.Id}:STOP_LOSS";
                if (_emittedAlertKeys.Add(key))
                    logger.LogWarning("HIGH risk alert {AlertKey}: stop loss breached for {Symbol} at {Price}.", key, position.Symbol, quote.Close);
            }
        }
    }
}
