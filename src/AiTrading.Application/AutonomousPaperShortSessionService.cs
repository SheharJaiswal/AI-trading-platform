using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record AutonomousPaperShortSessionRequest(
    int Quantity,
    decimal StopLossPercent,
    decimal TargetPercent,
    int MaxMarketDataAgeSeconds);

public sealed record AutonomousPaperShortSessionResult(
    Guid SessionId,
    Symbol Symbol,
    Recommendation Recommendation,
    MarketQuote Quote,
    PaperShortExecutionResult Execution,
    DurableShortPositionState? Position);

/// <summary>
/// Runs one explicitly configured autonomous paper-short attempt for a running paper session.
/// The caller supplies risk percentages; this service never invents trading policy.
/// </summary>
public sealed class AutonomousPaperShortSessionService(
    IPaperTradingSessionService sessions,
    RecommendationService recommendations,
    IMarketDataProvider marketData,
    DurablePaperShortExecutionService execution,
    DurablePaperShortCoverService positions)
{
    public async Task<AutonomousPaperShortSessionResult> RunAsync(
        Guid sessionId,
        Guid portfolioId,
        AutonomousPaperShortSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0) throw new ArgumentOutOfRangeException(nameof(request.Quantity), "Quantity must be positive.");
        if (request.MaxMarketDataAgeSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(request.MaxMarketDataAgeSeconds), "Maximum market-data age must be positive.");

        var options = new AutonomousPaperShortExecutionOptions(
            request.StopLossPercent,
            request.TargetPercent,
            TimeSpan.FromSeconds(request.MaxMarketDataAgeSeconds));
        options.Validate();

        var session = await sessions.GetAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Paper trading session {sessionId} does not exist.");
        if (session.Status != PaperTradingSessionStatus.Running)
            throw new InvalidOperationException($"Autonomous short cycle requires a running paper session; current state is {session.Status}.");

        var symbol = session.Configuration.Symbols.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(symbol.Value))
            throw new InvalidOperationException("Paper session has no symbols configured.");

        // Fetch one quote snapshot and reuse it for recommendation generation and short gating.
        // This avoids racing two quote reads and executing against a different price than the signal saw.
        var quote = await marketData.GetQuoteAsync(symbol, cancellationToken);
        var recommendation = await recommendations.GetRecommendationAsync(symbol, quote, cancellationToken);

        var coordinator = new AutonomousPaperShortExecutionCoordinator(execution, options);
        var lifecycle = new DurableAutonomousPaperShortLifecycleService(coordinator, positions);
        var result = await lifecycle.ExecuteAsync(sessionId, portfolioId, symbol, recommendation, quote, request.Quantity, cancellationToken);
        return new(sessionId, symbol, recommendation, quote, result.Execution, result.Position);
    }
}
