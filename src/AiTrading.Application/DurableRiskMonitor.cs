using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record MonitoringCheckSummary(int PositionCount, int SuccessCount, int FailureCount);

public sealed class DurableRiskMonitor(
    IMarketDataProvider marketData,
    ITradingUnitOfWorkFactory unitOfWorkFactory,
    Guid portfolioId,
    IAlertDelivery alertDelivery,
    IMonitoringFailureSink? failureSink = null)
{
    private readonly IMonitoringFailureSink _failureSink = failureSink ?? new NoopMonitoringFailureSink();

    public async Task<MonitoringCheckSummary> CheckOnceAsync(CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var positions = await unitOfWork.Portfolios.GetOpenPositionsAsync(portfolioId, cancellationToken);
        var changed = false;
        var newAlerts = new List<Alert>();
        var successCount = 0;
        var failureCount = 0;

        foreach (var position in positions)
        {
            MarketQuote quote;
            try
            {
                quote = await marketData.GetQuoteAsync(position.Symbol, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                failureCount++;
                _failureSink.Record(new MonitoringFailure(
                    "MARKET_DATA",
                    position.Symbol.Value,
                    "QUOTE_PROVIDER_FAILURE",
                    ex.Message,
                    DateTimeOffset.UtcNow));
                continue;
            }

            successCount++;
            var receivedAt = DateTimeOffset.UtcNow;
            var updated = position with
            {
                CurrentMarketPrice = quote.LastTradedPrice,
                UpdatedAt = receivedAt
            };
            await unitOfWork.Portfolios.SavePositionAsync(updated, cancellationToken);
            await unitOfWork.MarketDataSnapshots.AddAsync(
                new MarketDataSnapshotState(
                    Guid.NewGuid(), quote.Symbol, quote.InstrumentToken, quote.Source, quote.Exchange,
                    quote.Timestamp, receivedAt, quote.Open, quote.High, quote.Low, quote.Close,
                    quote.LastTradedPrice, quote.Volume), cancellationToken);
            changed = true;

            if (position.StopLoss is not null && quote.LastTradedPrice <= position.StopLoss)
            {
                var bucket = receivedAt.ToUnixTimeSeconds() / 60;
                var alert = new AlertState(
                    Guid.NewGuid(), position.Id, position.Symbol, "STOP_LOSS", AlertSeverity.High,
                    $"Stop loss breached for {position.Symbol} at {quote.LastTradedPrice}.", bucket.ToString(), receivedAt);
                if (await unitOfWork.Alerts.TryAddAsync(alert, cancellationToken))
                {
                    changed = true;
                    newAlerts.Add(new Alert($"{position.Id}:STOP_LOSS:{bucket}", AlertSeverity.High, alert.Message, receivedAt, position.Symbol));
                }
            }
        }

        if (changed)
            await unitOfWork.CommitAsync(cancellationToken);

        foreach (var alert in newAlerts)
        {
            try
            {
                await alertDelivery.DeliverAsync(alert, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                // Delivery is advisory to durable monitoring. A channel failure must not
                // invalidate the committed alert or stop subsequent monitoring iterations.
            }
        }

        return new MonitoringCheckSummary(positions.Count, successCount, failureCount);
    }
}
