namespace AiTrading.Application;

/// <summary>Deterministic, paper-only short lifecycle calculations. This class does not place orders.</summary>
public enum PaperShortLifecycleStatus
{
    SignalShort,
    ShortOpen,
    ShortStopped,
    ShortTargetHit,
    ShortClosed,
    NoTrade
}

public sealed record PaperShortPosition(
    string Symbol,
    int Quantity,
    decimal EntryPrice,
    decimal CurrentPrice,
    decimal StopLoss,
    decimal Target,
    PaperShortLifecycleStatus Status)
{
    public decimal UnrealizedPnl => (EntryPrice - CurrentPrice) * Quantity;

    public static PaperShortPosition Open(
        string symbol,
        int quantity,
        decimal entryPrice,
        decimal currentPrice,
        decimal stopLoss,
        decimal target)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol is required.", nameof(symbol));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (entryPrice <= 0 || currentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(entryPrice));
        if (stopLoss <= entryPrice) throw new ArgumentException("A short stop-loss must be above the entry price.", nameof(stopLoss));
        if (target >= entryPrice || target <= 0) throw new ArgumentException("A short target must be below the entry price.", nameof(target));

        return new(symbol, quantity, entryPrice, currentPrice, stopLoss, target, PaperShortLifecycleStatus.ShortOpen);
    }

    public PaperShortPosition Mark(decimal currentPrice)
    {
        if (currentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(currentPrice));
        if (Status is not PaperShortLifecycleStatus.ShortOpen and not PaperShortLifecycleStatus.SignalShort)
            return this with { CurrentPrice = currentPrice };

        var status = currentPrice >= StopLoss
            ? PaperShortLifecycleStatus.ShortStopped
            : currentPrice <= Target
                ? PaperShortLifecycleStatus.ShortTargetHit
                : PaperShortLifecycleStatus.ShortOpen;

        return this with { CurrentPrice = currentPrice, Status = status };
    }

    public PaperShortPosition Close(decimal currentPrice)
    {
        if (currentPrice <= 0) throw new ArgumentOutOfRangeException(nameof(currentPrice));
        return this with { CurrentPrice = currentPrice, Status = PaperShortLifecycleStatus.ShortClosed };
    }
}

public static class PaperShortLifecycleEngine
{
    public static PaperShortPosition Signal(string symbol, int quantity, decimal referencePrice, decimal stopLoss, decimal target)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("Symbol is required.", nameof(symbol));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (referencePrice <= 0) throw new ArgumentOutOfRangeException(nameof(referencePrice));
        if (stopLoss <= referencePrice || target <= 0 || target >= referencePrice)
            return new(symbol, quantity, referencePrice, referencePrice, stopLoss, target, PaperShortLifecycleStatus.NoTrade);

        return new(symbol, quantity, referencePrice, referencePrice, stopLoss, target, PaperShortLifecycleStatus.SignalShort);
    }
}
