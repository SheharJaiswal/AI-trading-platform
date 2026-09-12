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

public sealed record DurableShortCoverState(
    Guid Id,
    Guid PositionId,
    string IdempotencyKey,
    decimal CoverPrice,
    int CoverQuantity,
    decimal RealizedPnl,
    long ResultingPositionVersion,
    DateTimeOffset CreatedAt);

public interface IDurableShortPositionRepository
{
    Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken cancellationToken);
    Task AddAsync(DurableShortPositionState position, CancellationToken cancellationToken);
    Task<DurableShortPositionState> ApplyCoverAsync(
        Guid positionId,
        string idempotencyKey,
        decimal coverPrice,
        int coverQuantity,
        long expectedVersion,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
