namespace AiTrading.Domain;

/// <summary>Durable paper-only short position state. Quantity is the remaining open quantity.</summary>
public sealed record DurablePaperShortPosition(
    Guid Id,
    Guid PortfolioId,
    Symbol Symbol,
    int OriginalQuantity,
    int RemainingQuantity,
    decimal AverageEntryPrice,
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
        DateTimeOffset now) =>
        new(id, portfolioId, symbol, quantity, quantity, entryPrice, null, 0m, "SHORT_OPEN", now, now, 0);
}
