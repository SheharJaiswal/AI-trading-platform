namespace AiTrading.Application;

/// <summary>Pure accounting rules for paper short positions. No broker or portfolio mutation occurs here.</summary>
public static class PaperShortAccounting
{
    public static decimal RealizedPnl(decimal entryPrice, decimal coverPrice, int quantity)
    {
        ValidatePrice(entryPrice, nameof(entryPrice));
        ValidatePrice(coverPrice, nameof(coverPrice));
        ValidateQuantity(quantity);
        return (entryPrice - coverPrice) * quantity;
    }

    public static ShortCoverResult Cover(decimal entryPrice, decimal coverPrice, int openQuantity, int coverQuantity)
    {
        ValidatePrice(entryPrice, nameof(entryPrice));
        ValidatePrice(coverPrice, nameof(coverPrice));
        ValidateQuantity(openQuantity);
        if (coverQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(coverQuantity), "Cover quantity must be positive.");
        if (coverQuantity > openQuantity) throw new ArgumentOutOfRangeException(nameof(coverQuantity), "Cover quantity cannot exceed the open short quantity.");

        return new(
            coverQuantity,
            openQuantity - coverQuantity,
            RealizedPnl(entryPrice, coverPrice, coverQuantity),
            openQuantity == coverQuantity);
    }

    private static void ValidatePrice(decimal value, string parameterName)
    {
        if (value <= 0) throw new ArgumentOutOfRangeException(parameterName, "Price must be positive.");
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
    }
}

public sealed record ShortCoverResult(
    int CoveredQuantity,
    int RemainingQuantity,
    decimal RealizedPnl,
    bool FullyClosed);
