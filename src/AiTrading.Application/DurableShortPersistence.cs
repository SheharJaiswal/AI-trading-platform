using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record DurableShortPositionState(
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
    long Version);

public interface IDurableShortPositionRepository
{
    Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken cancellationToken);
    Task<DurableShortPositionState?> GetByIdempotencyKeyAsync(Guid positionId, string idempotencyKey, CancellationToken cancellationToken);
    Task AddAsync(DurableShortPositionState position, CancellationToken cancellationToken);
    Task SaveAsync(DurableShortPositionState position, long expectedVersion, CancellationToken cancellationToken);
}
