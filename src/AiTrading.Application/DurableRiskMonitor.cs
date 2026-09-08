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
            var updated = position with
            {
                CurrentMarketPrice = quote.LastTradedPrice,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            await unitOfWork.Portfolios.SavePositionAsync(updated, cancellationToken);
            changed = true;

            if (quote.LastTradedPrice <= position.StopLoss)
            {
                var bucket = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 60;
                var alert = new AlertState(
                    Guid.NewGuid(),
                    position.Id,
                    position.Symbol,
                    "STOP_LOSS",
                    AlertSeverity.High,
                    $"Stop loss breached for {position.Symbol} at {quote.LastTradedPrice}.",
                    bucket.ToString(),
                    DateTimeOffset.UtcNow);
                if (await unitOfWork.Alerts.TryAddAsync(alert, cancellationToken))
                    changed = true;
            }
        }

        if (changed)
            await unitOfWork.CommitAsync(cancellationToken);
    }
}
