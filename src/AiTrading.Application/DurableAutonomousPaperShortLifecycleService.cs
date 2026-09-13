using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record DurableAutonomousPaperShortLifecycleResult(
    PaperShortExecutionResult Execution,
    DurableShortPositionState? Position);

/// <summary>
/// Completes the paper-short open lifecycle after a confirmed paper fill.
/// Position identity is derived from the filled order so retries cannot create a second position.
/// </summary>
public sealed class DurableAutonomousPaperShortLifecycleService(
    IAutonomousPaperShortExecutionCoordinator coordinator,
    DurablePaperShortCoverService positions)
{
    public async Task<DurableAutonomousPaperShortLifecycleResult> ExecuteAsync(
        Guid sessionId,
        Guid portfolioId,
        Symbol symbol,
        Recommendation recommendation,
        MarketQuote quote,
        int quantity,
        CancellationToken cancellationToken)
    {
        var execution = await coordinator.ExecuteAsync(sessionId, symbol, recommendation, quote, quantity, cancellationToken);
        if (execution.Fill is null)
            return new(execution, null);

        var fill = execution.Fill;
        var existing = await positions.GetAsync(fill.OrderId, cancellationToken);
        if (existing is not null)
        {
            ValidateExistingPosition(existing, portfolioId, fill);
            var replayStatus = execution.Status == "already-executed" ? "already-positioned" : "executed-and-positioned";
            return new(execution with { Status = replayStatus }, existing);
        }

        var position = await positions.OpenAsync(
            fill.OrderId,
            portfolioId,
            fill.Symbol,
            fill.Quantity,
            fill.FillPrice,
            fill.FilledAt,
            cancellationToken);

        var status = execution.Status == "already-executed" ? "already-positioned" : "executed-and-positioned";
        return new(execution with { Status = status }, position);
    }

    private static void ValidateExistingPosition(DurableShortPositionState existing, Guid portfolioId, FillState fill)
    {
        if (existing.PortfolioId != portfolioId || existing.Symbol != fill.Symbol ||
            existing.OriginalQuantity != fill.Quantity || existing.AverageEntryPrice != fill.FillPrice)
            throw new InvalidOperationException("Short position identity conflicts with the confirmed fill.");
    }
}
