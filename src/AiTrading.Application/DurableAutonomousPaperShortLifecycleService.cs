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
    AutonomousPaperShortExecutionCoordinator coordinator,
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

        var position = await positions.OpenAsync(
            execution.Fill.OrderId,
            portfolioId,
            execution.Fill.Symbol,
            execution.Fill.Quantity,
            execution.Fill.Price,
            execution.Fill.Timestamp,
            cancellationToken);

        var status = execution.Status == "already-executed" ? "already-positioned" : "executed-and-positioned";
        return new(execution with { Status = status }, position);
    }
}
