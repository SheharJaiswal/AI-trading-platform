namespace AiTrading.Application;

public sealed record ShortRiskGateResult(bool Approved, string? Reason);

public static class PaperShortRiskGate
{
    public static ShortRiskGateResult Validate(
        int quantity,
        decimal entryPrice,
        decimal stopLoss,
        decimal targetPrice)
    {
        if (quantity <= 0)
            return new(false, "INVALID_QUANTITY");

        if (entryPrice <= 0)
            return new(false, "INVALID_ENTRY_PRICE");

        if (stopLoss <= entryPrice)
            return new(false, "SHORT_STOP_MUST_BE_ABOVE_ENTRY");

        if (targetPrice >= entryPrice)
            return new(false, "SHORT_TARGET_MUST_BE_BELOW_ENTRY");

        return new(true, null);
    }

    public static string? EvaluateExit(decimal currentPrice, decimal stopLoss, decimal targetPrice)
    {
        if (currentPrice >= stopLoss)
            return "SHORT_STOPPED";

        if (currentPrice <= targetPrice)
            return "SHORT_TARGET_HIT";

        return null;
    }

    public static decimal UnrealizedPnl(decimal entryPrice, decimal currentPrice, int quantity) =>
        (entryPrice - currentPrice) * quantity;
}
