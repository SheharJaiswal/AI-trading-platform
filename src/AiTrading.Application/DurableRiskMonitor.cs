using AiTrading.Domain;

namespace AiTrading.Application;

public sealed class DurableRiskMonitor(
    IMarketDataProvider marketData,
    ITradingUnitOfWorkFactory unitOfWorkFactory,
    Guid portfolioId)
{
    public async Task CheckOnceAsync(CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var positions = await unitOfWork.Portfolios.GetOpenPositionsAsync(portfolioId, cancellationToken);
        var changed = false;

        foreach (var position in positions)
        {
            if (position.StopLoss is null) continue;

            var quote = await marketData.GetQuoteAsync(position.Symbol, cancellationToken);
            var receivedAt = DateTimeOffset.UtcNow;
            var updated = position with
            {
                CurrentMarketPrice = quote.LastTradedPrice,
                UpdatedAt = receivedAt
            };
            await unitOfWork.Portfolios.SavePositionAsync(updated, cancellationToken);
            await unitOfWork.MarketDataSnapshots.AddAsync(
                new MarketDataSnapshotState(
                    Guid.NewGuid(),
                    quote.Symbol,
                    quote.InstrumentToken,
                    quote.Source,
                    quote.Exchange,
                    quote.Timestamp,
                    receivedAt,
                    quote.Open,
                    quote.High,
                    quote.Low,
                    quote.Close,
                    quote.LastTradedPrice,
                    quote.Volume),
                cancellationToken);
            changed = true;

            if (quote.LastTradedPrice <= position.StopLoss)
            {
                var bucket = receivedAt.ToUnixTimeSeconds() / 60;
                var alert = new AlertState(
                    Guid.NewGuid(),
                    position.Id,
                    position.Symbol,
                    "STOP_LOSS",
                    AlertSeverity.High,
                    $"Stop loss breached for {position.Symbol} at {quote.LastTradedPrice}.",
                    bucket.ToString(),
                    receivedAt);
                if (await unitOfWork.Alerts.TryAddAsync(alert, cancellationToken))
                    changed = true;
            }
        }

        if (changed)
            await unitOfWork.CommitAsync(cancellationToken);
    }
}
