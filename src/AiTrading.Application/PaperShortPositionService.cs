using AiTrading.Domain;

namespace AiTrading.Application;

/// <summary>Pure state transition helper for durable paper short positions.</summary>
public static class PaperShortPositionService
{
    public static PaperShortPosition Cover(
        PaperShortPosition position,
        decimal coverPrice,
        int coverQuantity,
        DateTimeOffset now)
    {
        if (position.RemainingQuantity <= 0)
            throw new InvalidOperationException("The paper short position is already closed.");
        if (coverPrice <= 0)
            throw new ArgumentOutOfRangeException(nameof(coverPrice), "Cover price must be positive.");
        if (coverQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(coverQuantity), "Cover quantity must be positive.");
        if (coverQuantity > position.RemainingQuantity)
            throw new InvalidOperationException("Cover quantity cannot exceed the remaining short quantity.");

        var realized = PaperShortAccounting.RealizedPnl(
            position.AverageEntryPrice, coverPrice, coverQuantity);
        var remaining = position.RemainingQuantity - coverQuantity;
        var state = remaining == 0 ? "SHORT_CLOSED" : "SHORT_PARTIALLY_COVERED";

        return position with
        {
            RemainingQuantity = remaining,
            LastCoverPrice = coverPrice,
            RealizedPnl = position.RealizedPnl + realized,
            State = state,
            UpdatedAt = now,
            Version = position.Version + 1
        };
    }
}
