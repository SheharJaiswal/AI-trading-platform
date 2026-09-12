namespace AiTrading.Application;

/// <summary>Pure planner for paper-short cover decisions. It does not mutate persistence or call a broker.</summary>
public static class PaperShortCoverPlanner
{
    public static ShortCoverPlan Plan(
        decimal entryPrice,
        decimal currentPrice,
        int openQuantity,
        int coverQuantity)
    {
        if (entryPrice <= 0) throw new ArgumentOutOfRangeException(nameof(entryPrice), "Entry price must be positive.");
        if (currentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(currentPrice), "Current price must be positive.");
        if (openQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(openQuantity), "Open quantity must be positive.");
        if (coverQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(coverQuantity), "Cover quantity must be positive.");
        if (coverQuantity > openQuantity) throw new ArgumentOutOfRangeException(nameof(coverQuantity), "Cover quantity cannot exceed the open short quantity.");

        var accounting = PaperShortAccounting.Cover(entryPrice, currentPrice, openQuantity, coverQuantity);
        return new(
            accounting.CoveredQuantity,
            accounting.RemainingQuantity,
            accounting.RealizedPnl,
            accounting.FullyClosed,
            accounting.FullyClosed ? "SHORT_CLOSED" : "SHORT_PARTIALLY_COVERED");
    }
}

public sealed record ShortCoverPlan(
    int CoveredQuantity,
    int RemainingQuantity,
    decimal RealizedPnl,
    bool FullyClosed,
    string State);
