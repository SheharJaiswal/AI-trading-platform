using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record PortfolioState(
    Guid Id,
    decimal Cash,
    decimal RealizedPnl,
    DateTimeOffset UpdatedAt,
    long Version);

public sealed record PositionState(
    Guid Id,
    Guid PortfolioId,
    Symbol Symbol,
    string? InstrumentToken,
    int Quantity,
    decimal AverageEntryPrice,
    decimal CurrentMarketPrice,
    decimal? StopLoss,
    DateTimeOffset OpenedAt,
    DateTimeOffset UpdatedAt);

public sealed record OrderState(
    Guid Id,
    string IdempotencyKey,
    Symbol Symbol,
    string? InstrumentToken,
    OrderSide Side,
    int Quantity,
    decimal LimitPrice,
    string StrategyVersion,
    DateTimeOffset CreatedAt,
    string ExecutionMode,
    string Status);

public sealed record FillState(
    Guid Id,
    Guid OrderId,
    Symbol Symbol,
    OrderSide Side,
    int Quantity,
    decimal FillPrice,
    DateTimeOffset FilledAt,
    string ExecutionProvider);

public sealed record AlertState(
    Guid Id,
    Guid? PositionId,
    Symbol? Symbol,
    string Rule,
    AlertSeverity Severity,
    string Message,
    string EvaluationBucket,
    DateTimeOffset CreatedAt);

public sealed record MarketDataSnapshotState(
    Guid Id,
    Symbol Symbol,
    string? InstrumentToken,
    string Provider,
    string Exchange,
    DateTimeOffset ProviderTimestamp,
    DateTimeOffset ReceivedAt,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal LastTradedPrice,
    long Volume);

public interface IPortfolioRepository
{
    Task<PortfolioState?> GetAsync(Guid portfolioId, CancellationToken cancellationToken);
    Task<IReadOnlyList<PositionState>> GetOpenPositionsAsync(Guid portfolioId, CancellationToken cancellationToken);
    Task SaveAsync(PortfolioState portfolio, long expectedVersion, CancellationToken cancellationToken);
    Task SavePositionAsync(PositionState position, CancellationToken cancellationToken);
    Task DeletePositionAsync(Guid positionId, CancellationToken cancellationToken);
}

public interface IOrderRepository
{
    Task<OrderState?> GetAsync(Guid orderId, CancellationToken cancellationToken);
    Task<OrderState?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    Task AddAsync(OrderState order, CancellationToken cancellationToken);
    Task<FillState?> GetFillByOrderIdAsync(Guid orderId, CancellationToken cancellationToken);
    Task AddFillAsync(FillState fill, CancellationToken cancellationToken);
}

public interface IAlertRepository
{
    Task<bool> TryAddAsync(AlertState alert, CancellationToken cancellationToken);
    Task<IReadOnlyList<AlertState>> GetAllAsync(CancellationToken cancellationToken);
}

public interface IMarketDataSnapshotRepository
{
    Task AddAsync(MarketDataSnapshotState snapshot, CancellationToken cancellationToken);
    Task<MarketDataSnapshotState?> GetLatestAsync(Symbol symbol, CancellationToken cancellationToken);
}

public interface ITradingUnitOfWork : IAsyncDisposable
{
    IPortfolioRepository Portfolios { get; }
    IOrderRepository Orders { get; }
    IAlertRepository Alerts { get; }
    IMarketDataSnapshotRepository MarketDataSnapshots { get; }
    Task CommitAsync(CancellationToken cancellationToken);
}

public interface ITradingUnitOfWorkFactory
{
    Task<ITradingUnitOfWork> CreateAsync(CancellationToken cancellationToken);
}
