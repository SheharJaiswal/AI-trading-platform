using System.Security.Cryptography;
using System.Text;
using AiTrading.Domain;

namespace AiTrading.Application;

/// <summary>Routes an explicit bearish recommendation through the paper-only short boundary.</summary>
public sealed class AutonomousPaperShortCycleService(
    RecommendationService recommendations,
    DurablePaperShortExecutionService execution)
{
    public async Task<PaperShortExecutionResult> ExecuteAsync(
        Symbol symbol,
        int quantity,
        decimal entryPrice,
        decimal stopLoss,
        decimal targetPrice,
        CancellationToken cancellationToken)
    {
        var recommendation = await recommendations.GetRecommendationAsync(symbol, cancellationToken);
        if (recommendation.Action != RecommendationAction.Sell)
            return new(new(RiskDecision.RiskBlocked, "SHORT_REQUIRES_BEARISH_RECOMMENDATION"), null, "no-trade");

        var plan = PaperShortCyclePlanner.Plan(
            symbol,
            recommendation,
            quantity,
            entryPrice,
            recommendation.ReferencePrice,
            stopLoss,
            targetPrice);
        if (plan is null || !plan.Risk.Approved)
        {
            var reason = plan?.Risk.Reason ?? "SHORT_PLAN_UNAVAILABLE";
            return new(new(RiskDecision.RiskBlocked, reason), null, "no-trade");
        }

        var orderId = DeterministicGuid($"paper-short:{symbol.Value}:{recommendation.GeneratedAt:O}:{quantity}:{entryPrice}:{stopLoss}:{targetPrice}");
        var request = new PaperShortExecutionRequest(
            orderId,
            $"paper-short:{orderId:N}",
            symbol,
            quantity,
            entryPrice,
            stopLoss,
            targetPrice,
            recommendation.StrategyVersion);

        return await execution.OpenAsync(request, cancellationToken);
    }

    private static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
