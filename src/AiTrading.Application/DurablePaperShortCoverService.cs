using AiTrading.Domain;

namespace AiTrading.Application;

public sealed class DurablePaperShortCoverService(IDurableShortPositionRepository repository)
{
    public async Task<DurableShortPositionState> OpenAsync(Guid portfolioId, Symbol symbol, int quantity, decimal entryPrice, DateTimeOffset now, CancellationToken ct)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (entryPrice <= 0) throw new ArgumentOutOfRangeException(nameof(entryPrice), "Entry price must be positive.");
        var position = DurablePaperShortPosition.Open(Guid.NewGuid(), portfolioId, symbol, quantity, entryPrice, now);
        var state = ToState(position);
        await repository.AddAsync(state, ct);
        return state;
    }

    public async Task<DurableShortPositionState> CoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128) throw new ArgumentException("Idempotency key is required and must be 1-128 characters.", nameof(idempotencyKey));
        if (coverPrice <= 0) throw new ArgumentOutOfRangeException(nameof(coverPrice), "Cover price must be positive.");
        if (coverQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(coverQuantity), "Cover quantity must be positive.");
        return await repository.ApplyCoverAsync(positionId, idempotencyKey, coverPrice, coverQuantity, expectedVersion, now, ct);
    }

    private static DurableShortPositionState ToState(DurablePaperShortPosition p) => new(p.Id, p.PortfolioId, p.Symbol, p.OriginalQuantity, p.RemainingQuantity, p.AverageEntryPrice, p.LastCoverPrice, p.RealizedPnl, p.State, p.CreatedAt, p.UpdatedAt, p.Version);
}
