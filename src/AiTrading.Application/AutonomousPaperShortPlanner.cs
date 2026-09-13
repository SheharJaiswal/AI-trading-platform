using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record AutonomousPaperShortPlan(
    Symbol Symbol,
    Recommendation Recommendation,
    PaperShortCyclePlan Cycle,
    PaperShortExecutionRequest? ExecutionRequest);

/// <summary>Builds, but does not route, a deterministic paper-short execution request.</summary>
public static class AutonomousPaperShortPlanner
{
    public static AutonomousPaperShortPlan? Plan(
        Guid sessionId,
        Symbol symbol,
        Recommendation recommendation,
        int quantity,
        decimal entryPrice,
        decimal currentPrice,
        decimal stopLoss,
        decimal targetPrice) => Plan(sessionId, symbol, recommendation, quantity, entryPrice, currentPrice, stopLoss, targetPrice, recommendation.GeneratedAt.ToString("yyyyMMddHHmmss"));

    public static AutonomousPaperShortPlan? Plan(
        Guid sessionId,
        Symbol symbol,
        Recommendation recommendation,
        int quantity,
        decimal entryPrice,
        decimal currentPrice,
        decimal stopLoss,
        decimal targetPrice,
        string cycleKey)
    {
        if (string.IsNullOrWhiteSpace(cycleKey) || cycleKey.Length > 64)
            throw new ArgumentException("Cycle key is required and must be 1-64 characters.", nameof(cycleKey));

        var cycle = PaperShortCyclePlanner.Plan(symbol, recommendation, quantity, entryPrice, currentPrice, stopLoss, targetPrice);
        if (cycle is null || !cycle.Risk.Approved)
            return cycle is null ? null : new AutonomousPaperShortPlan(symbol, recommendation, cycle, null);

        var idempotencyKey = $"session:{sessionId:N}:short:{symbol.Value}:{cycleKey}";
        var orderId = DeterministicGuid(idempotencyKey);
        var request = new PaperShortExecutionRequest(
            orderId,
            idempotencyKey,
            symbol,
            quantity,
            entryPrice,
            stopLoss,
            targetPrice,
            recommendation.StrategyVersion);
        return new AutonomousPaperShortPlan(symbol, recommendation, cycle, request);
    }

    private static Guid DeterministicGuid(string value)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
