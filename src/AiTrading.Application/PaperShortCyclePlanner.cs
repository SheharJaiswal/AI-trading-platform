using AiTrading.Domain;

namespace AiTrading.Application;

/// <summary>Builds a deterministic paper-short lifecycle from an explicit bearish recommendation and risk inputs.</summary>
public sealed record PaperShortCyclePlan(
    Symbol Symbol,
    Recommendation Recommendation,
    PaperShortPosition Position,
    ShortRiskGateResult Risk);

public static class PaperShortCyclePlanner
{
    public static PaperShortCyclePlan? Plan(
        Symbol symbol,
        Recommendation recommendation,
        int quantity,
        decimal entryPrice,
        decimal currentPrice,
        decimal stopLoss,
        decimal targetPrice)
    {
        if (recommendation.Action != RecommendationAction.Sell)
            return null;

        var risk = PaperShortRiskGate.Validate(quantity, entryPrice, stopLoss, targetPrice);
        if (!risk.Approved)
            return new PaperShortCyclePlan(
                symbol,
                recommendation,
                new PaperShortPosition(symbol.Value, quantity, entryPrice, currentPrice, stopLoss, targetPrice, PaperShortLifecycleStatus.NoTrade),
                risk);

        var position = PaperShortLifecycleEngine.Signal(
            symbol.Value, quantity, entryPrice, stopLoss, targetPrice).Mark(currentPrice);

        return new PaperShortCyclePlan(symbol, recommendation, position, risk);
    }
}
