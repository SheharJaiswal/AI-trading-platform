using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record AutonomousPaperShortExecutionOptions(
    decimal StopLossPercent,
    decimal TargetPercent,
    TimeSpan MaxMarketDataAge)
{
    public void Validate()
    {
        if (StopLossPercent <= 0 || StopLossPercent >= 1)
            throw new ArgumentOutOfRangeException(nameof(StopLossPercent), "Stop loss percent must be greater than 0 and less than 1.");
        if (TargetPercent <= 0 || TargetPercent >= 1)
            throw new ArgumentOutOfRangeException(nameof(TargetPercent), "Target percent must be greater than 0 and less than 1.");
        if (MaxMarketDataAge <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(MaxMarketDataAge), "Maximum market-data age must be positive.");
    }
}

/// <summary>
/// Coordinates one autonomous paper-short attempt from an already-fetched quote and recommendation.
/// Freshness and short risk are mandatory gates; this class never routes to a live broker.
/// </summary>
public sealed class AutonomousPaperShortExecutionCoordinator(
    DurablePaperShortExecutionService execution,
    AutonomousPaperShortExecutionOptions options)
{
    public async Task<PaperShortExecutionResult> ExecuteAsync(
        Guid sessionId,
        Symbol symbol,
        Recommendation recommendation,
        MarketQuote quote,
        int quantity,
        CancellationToken cancellationToken)
    {
        options.Validate();

        if (recommendation.Action != RecommendationAction.Sell)
            return NoTrade("SHORT_REQUIRES_BEARISH_RECOMMENDATION");

        var now = DateTimeOffset.UtcNow;
        var age = now - quote.Timestamp;
        if (quote.Timestamp > now || age > options.MaxMarketDataAge)
            return NoTrade("STALE_OR_INVALID_MARKET_DATA");

        if (quote.Symbol != symbol || quote.LastTradedPrice <= 0)
            return NoTrade("INVALID_SHORT_MARKET_QUOTE");

        var entryPrice = quote.LastTradedPrice;
        var stopLoss = entryPrice * (1m + options.StopLossPercent);
        var targetPrice = entryPrice * (1m - options.TargetPercent);
        var plan = AutonomousPaperShortPlanner.Plan(
            sessionId,
            symbol,
            recommendation,
            quantity,
            entryPrice,
            entryPrice,
            stopLoss,
            targetPrice);

        if (plan?.ExecutionRequest is null || !plan.Cycle.Risk.Approved)
            return NoTrade(plan?.Cycle.Risk.Reason ?? "SHORT_PLAN_UNAVAILABLE");

        return await execution.OpenAsync(plan.ExecutionRequest, cancellationToken);
    }

    private static PaperShortExecutionResult NoTrade(string reason) =>
        new(new(RiskDecision.RiskBlocked, reason), null, "no-trade");
}
