using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record MonitoringCheckSummary(int PositionCount, int SuccessCount, int FailureCount);

public sealed class DurableRiskMonitor(
    IMarketDataProvider marketData,
    ITradingUnitOfWorkFactory unitOfWorkFactory,
    Guid portfolioId,
    IAlertDelivery alertDelivery,
    PortfolioRiskMonitor? portfolioRiskMonitor = null,
    IMonitoringFailureSink? failureSink = null)
{
    private readonly PortfolioRiskMonitor _portfolioRiskMonitor = portfolioRiskMonitor ?? new PortfolioRiskMonitor(new PortfolioRiskMonitoringOptions());
    private readonly IMonitoringFailureSink _failureSink = failureSink ?? new NoopMonitoringFailureSink();

    public DurableRiskMonitor(
        IMarketDataProvider marketData,
        ITradingUnitOfWorkFactory unitOfWorkFactory,
        Guid portfolioId,
        IAlertDelivery alertDelivery,
        IMonitoringFailureSink failureSink)
        : this(marketData, unitOfWorkFactory, portfolioId, alertDelivery, null, failureSink)
    {
    }

    public async Task<MonitoringCheckSummary> CheckOnceAsync(CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var portfolioState = await unitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken);
        if (portfolioState is null)
            return new MonitoringCheckSummary(0, 0, 0);

        var positions = await unitOfWork.Portfolios.GetOpenPositionsAsync(portfolioId, cancellationToken);
        var changed = false;
        var newAlerts = new List<Alert>();
        var prices = new Dictionary<Symbol, decimal>();
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
                _failureSink.Record(new MonitoringFailure("MARKET_DATA", position.Symbol.Value, "QUOTE_PROVIDER_FAILURE", ex.Message, DateTimeOffset.UtcNow));
                continue;
            }

            successCount++;
            prices[position.Symbol] = quote.LastTradedPrice;
            var receivedAt = DateTimeOffset.UtcNow;
            var updated = position with { CurrentMarketPrice = quote.LastTradedPrice, UpdatedAt = receivedAt };
            await unitOfWork.Portfolios.SavePositionAsync(updated, cancellationToken);
            await unitOfWork.MarketDataSnapshots.AddAsync(new MarketDataSnapshotState(Guid.NewGuid(), quote.Symbol, quote.InstrumentToken, quote.Source, quote.Exchange, quote.Timestamp, receivedAt, quote.Open, quote.High, quote.Low, quote.Close, quote.LastTradedPrice, quote.Volume), cancellationToken);
            changed = true;

            if (position.StopLoss is not null && quote.LastTradedPrice <= position.StopLoss)
            {
                var bucket = receivedAt.ToUnixTimeSeconds() / 60;
                var alert = new AlertState(Guid.NewGuid(), position.Id, position.Symbol, "STOP_LOSS", AlertSeverity.High, $"Stop loss breached for {position.Symbol} at {quote.LastTradedPrice}.", bucket.ToString(), receivedAt);
                if (await unitOfWork.Alerts.TryAddAsync(alert, cancellationToken))
                {
                    changed = true;
                    newAlerts.Add(new Alert($"{position.Id}:STOP_LOSS:{bucket}", AlertSeverity.High, alert.Message, receivedAt, position.Symbol));
                }
            }
        }

        var domainPositions = positions.Select(position => new Position(position.Id, position.Symbol, position.Quantity, position.AverageEntryPrice, position.StopLoss)).ToArray();
        var unrealizedPnl = domainPositions.Sum(position =>
        {
            var price = prices.GetValueOrDefault(position.Symbol, position.AverageEntryPrice);
            return (price - position.AverageEntryPrice) * position.Quantity;
        });
        var portfolio = new Portfolio(portfolioState.Cash, domainPositions, unrealizedPnl, portfolioState.RealizedPnl);
        var riskEvents = _portfolioRiskMonitor.Evaluate(portfolio, prices, 0m);
        var riskBucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
        foreach (var riskEvent in riskEvents)
        {
            var alert = new AlertState(Guid.NewGuid(), null, riskEvent.Symbol, riskEvent.Type, riskEvent.Severity, riskEvent.Message, riskEvent.Symbol is null ? riskBucket.ToString() : $"{riskBucket}:{riskEvent.Symbol.Value}", riskEvent.Timestamp);
            if (await unitOfWork.Alerts.TryAddAsync(alert, cancellationToken))
            {
                changed = true;
                newAlerts.Add(new Alert($"PORTFOLIO:{riskEvent.Type}:{riskBucket}:{riskEvent.Symbol?.Value ?? "PORTFOLIO"}", riskEvent.Severity, riskEvent.Message, riskEvent.Timestamp, riskEvent.Symbol));
            }
        }

        if (changed)
            await unitOfWork.CommitAsync(cancellationToken);

        foreach (var alert in newAlerts)
        {
            try { await alertDelivery.DeliverAsync(alert, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { }
        }

        return new MonitoringCheckSummary(positions.Count, successCount, failureCount);
    }
}
