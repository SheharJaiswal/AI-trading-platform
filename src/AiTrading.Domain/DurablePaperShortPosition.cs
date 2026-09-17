namespace AiTrading.Domain;

/// <summary>Durable paper-only short position state. Quantity is the remaining open quantity.</summary>
public sealed record DurablePaperShortPosition(
    Guid Id,
    Guid PortfolioId,
    Symbol Symbol,
    int OriginalQuantity,
    int RemainingQuantity,
    decimal AverageEntryPrice,
    decimal? StopLoss,
    decimal? TargetPrice,
    decimal? LastCoverPrice,
    decimal RealizedPnl,
    string State,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public static DurablePaperShortPosition Open(
        Guid id,
        Guid portfolioId,
        Symbol symbol,
        int quantity,
        decimal entryPrice,
        DateTimeOffset now,
        decimal? stopLoss = null,
        decimal? targetPrice = null) =>
        new(id, portfolioId, symbol, quantity, quantity, entryPrice, stopLoss, targetPrice, null, 0m, "SHORT_OPEN", now, now, 0);
}
