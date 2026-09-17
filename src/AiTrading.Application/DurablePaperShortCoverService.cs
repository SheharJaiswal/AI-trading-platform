using AiTrading.Domain;

namespace AiTrading.Application;

public sealed class DurablePaperShortCoverService(IDurableShortPositionRepository repository)
{
    public async Task<DurableShortPositionState> OpenAsync(Guid portfolioId, Symbol symbol, int quantity, decimal entryPrice, DateTimeOffset now, CancellationToken ct)
        => await OpenAsync(Guid.NewGuid(), portfolioId, symbol, quantity, entryPrice, now, null, null, ct);

    public async Task<DurableShortPositionState> OpenAsync(Guid positionId, Guid portfolioId, Symbol symbol, int quantity, decimal entryPrice, DateTimeOffset now, CancellationToken ct)
        => await OpenAsync(positionId, portfolioId, symbol, quantity, entryPrice, now, null, null, ct);

    public async Task<DurableShortPositionState> OpenAsync(Guid positionId, Guid portfolioId, Symbol symbol, int quantity, decimal entryPrice, DateTimeOffset now, decimal? stopLoss, decimal? targetPrice, CancellationToken ct)
    {
        if (positionId == Guid.Empty) throw new ArgumentException("PositionId is required.", nameof(positionId));
        if (portfolioId == Guid.Empty) throw new ArgumentException("PortfolioId is required.", nameof(portfolioId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (entryPrice <= 0) throw new ArgumentOutOfRangeException(nameof(entryPrice), "Entry price must be positive.");
        if (stopLoss is not null && targetPrice is not null && !PaperShortRiskGate.Validate(quantity, entryPrice, stopLoss.Value, targetPrice.Value).Approved)
            throw new ArgumentException("Configured short risk levels are invalid.", nameof(stopLoss));

        var existing = await repository.GetAsync(positionId, ct);
        if (existing is not null)
        {
            if (existing.PortfolioId != portfolioId || existing.Symbol != symbol || existing.OriginalQuantity != quantity || existing.AverageEntryPrice != entryPrice || existing.StopLoss != stopLoss || existing.TargetPrice != targetPrice)
                throw new InvalidOperationException("Position id is already associated with different short-position details.");
            return existing;
        }

        var position = DurablePaperShortPosition.Open(positionId, portfolioId, symbol, quantity, entryPrice, now, stopLoss, targetPrice);
        var state = ToState(position);
        await repository.AddAsync(state, ct);
        return state;
    }

    public async Task<DurableShortPositionState> CoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128) throw new ArgumentException("Idempotency key is required and must be 1-128 characters.", nameof(idempotencyKey));
        if (coverPrice <= 0) throw new ArgumentOutOfRangeException(nameof(coverPrice), "Cover price must be positive.");
        if (coverQuantity <= 0) throw new ArgumentOutOfRangeException(nameof(coverQuantity), "Cover quantity must be positive.");
        if (expectedVersion < 0) throw new ArgumentOutOfRangeException(nameof(expectedVersion), "Expected position version cannot be negative.");
        return await repository.ApplyCoverAsync(positionId, idempotencyKey, coverPrice, coverQuantity, expectedVersion, now, ct);
    }

    private static DurableShortPositionState ToState(DurablePaperShortPosition p) => new(p.Id, p.PortfolioId, p.Symbol, p.OriginalQuantity, p.RemainingQuantity, p.AverageEntryPrice, p.StopLoss, p.TargetPrice, p.LastCoverPrice, p.RealizedPnl, p.State, p.CreatedAt, p.UpdatedAt, p.Version);
}
