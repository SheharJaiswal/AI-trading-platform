using AiTrading.Application;
using AiTrading.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Infrastructure.Persistence;

public sealed class EfDurablePaperShortPositionRepository(TradingDbContext db) : IDurablePaperShortPositionRepository
{
    public async Task<DurablePaperShortPosition?> GetAsync(Guid positionId, CancellationToken ct) =>
        await db.DurablePaperShortPositions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == positionId, ct) is { } r ? r.ToDomain() : null;

    public async Task<DurablePaperShortPosition?> GetByCoverIdempotencyKeyAsync(string key, CancellationToken ct) =>
        await db.DurablePaperShortCoverOperations.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == key, ct) is { } op
            ? await GetAsync(op.PositionId, ct)
            : null;

    public async Task<bool> HasCoverKeyAsync(string key, CancellationToken ct) =>
        await db.DurablePaperShortCoverOperations.AnyAsync(x => x.IdempotencyKey == key, ct);

    public Task SaveCoverAsync(DurablePaperShortPosition position, string idempotencyKey, decimal coverPrice, int coverQuantity, CancellationToken ct)
    {
        var record = db.DurablePaperShortPositions.Single(x => x.Id == position.Id);
        if (record.Version != position.Version - 1)
            throw new DbUpdateConcurrencyException("The durable paper short position version is stale.");

        record.RemainingQuantity = position.RemainingQuantity;
        record.LastCoverPrice = position.LastCoverPrice;
        record.RealizedPnl = position.RealizedPnl;
        record.State = position.State;
        record.UpdatedAt = position.UpdatedAt;
        record.Version = position.Version;
        db.DurablePaperShortCoverOperations.Add(new DurablePaperShortCoverOperationRecord
        {
            Id = Guid.NewGuid(), PositionId = position.Id, IdempotencyKey = idempotencyKey,
            CoverPrice = coverPrice, CoverQuantity = coverQuantity, CreatedAt = position.UpdatedAt
        });
        return Task.CompletedTask;
    }

    public Task AddAsync(DurablePaperShortPosition position, CancellationToken ct)
    {
        db.DurablePaperShortPositions.Add(position.ToRecord());
        return Task.CompletedTask;
    }
}

internal static class DurablePaperShortPersistenceMapping
{
    public static DurablePaperShortPosition ToDomain(this DurablePaperShortPositionRecord r) =>
        new(r.Id, r.PortfolioId, new Symbol(r.Symbol, r.InstrumentToken), r.OriginalQuantity,
            r.RemainingQuantity, r.AverageEntryPrice, r.LastCoverPrice, r.RealizedPnl, r.State,
            r.CreatedAt, r.UpdatedAt, r.Version);

    public static DurablePaperShortPositionRecord ToRecord(this DurablePaperShortPosition p) => new()
    {
        Id = p.Id, PortfolioId = p.PortfolioId, Symbol = p.Symbol.Value, InstrumentToken = p.Symbol.InstrumentToken,
        OriginalQuantity = p.OriginalQuantity, RemainingQuantity = p.RemainingQuantity,
        AverageEntryPrice = p.AverageEntryPrice, LastCoverPrice = p.LastCoverPrice, RealizedPnl = p.RealizedPnl,
        State = p.State, CreatedAt = p.CreatedAt, UpdatedAt = p.UpdatedAt, Version = p.Version
    };
}
