using AiTrading.Application;
using AiTrading.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AiTrading.Infrastructure.Persistence;

internal static class PersistenceMapping
{
    public static PortfolioState ToState(this PortfolioRecord r) => new(r.Id, r.Cash, r.RealizedPnl, r.UpdatedAt, r.Version);
    public static PositionState ToState(this PositionRecord r) => new(r.Id, r.PortfolioId, new Symbol(r.Symbol, r.InstrumentToken), r.InstrumentToken, r.Quantity, r.AverageEntryPrice, r.CurrentMarketPrice, r.StopLoss, r.OpenedAt, r.UpdatedAt);
    public static OrderState ToState(this OrderRecord r) => new(r.Id, new Symbol(r.Symbol, r.InstrumentToken), r.InstrumentToken, Enum.Parse<OrderSide>(r.Side, true), r.Quantity, r.LimitPrice, r.StrategyVersion, r.CreatedAt, r.ExecutionMode, r.Status);
    public static FillState ToState(this FillRecord r) => new(r.Id, r.OrderId, new Symbol(r.Symbol), Enum.Parse<OrderSide>(r.Side, true), r.Quantity, r.FillPrice, r.FilledAt, r.ExecutionProvider);
    public static AlertState ToState(this AlertRecord r) => new(r.Id, r.PositionId, string.IsNullOrWhiteSpace(r.Symbol) ? null : new Symbol(r.Symbol), r.Rule, Enum.Parse<AlertSeverity>(r.Severity, true), r.Message, r.EvaluationBucket, r.CreatedAt);
    public static MarketDataSnapshotState ToState(this MarketDataSnapshotRecord r) => new(r.Id, new Symbol(r.Symbol, r.InstrumentToken), r.InstrumentToken, r.Provider, r.Exchange, r.ProviderTimestamp, r.ReceivedAt, r.Open, r.High, r.Low, r.Close, r.LastTradedPrice, r.Volume);
}

public sealed class EfPortfolioRepository(TradingDbContext db) : IPortfolioRepository
{
    public async Task<PortfolioState?> GetAsync(Guid id, CancellationToken ct) => await db.Portfolios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) is { } r ? r.ToState() : null;
    public async Task<IReadOnlyList<PositionState>> GetOpenPositionsAsync(Guid portfolioId, CancellationToken ct)
    {
        var records = await db.Positions.AsNoTracking().Where(x => x.PortfolioId == portfolioId && x.Quantity > 0).OrderBy(x => x.Symbol).ToListAsync(ct);
        return records.Select(x => x.ToState()).ToList();
    }
    public async Task SaveAsync(PortfolioState p, long expectedVersion, CancellationToken ct)
    {
        var r = await db.Portfolios.SingleOrDefaultAsync(x => x.Id == p.Id, ct);
        if (r is null)
        {
            if (expectedVersion != 0) throw new DbUpdateConcurrencyException($"Portfolio {p.Id} does not exist.");
            db.Portfolios.Add(new PortfolioRecord { Id = p.Id, Cash = p.Cash, RealizedPnl = p.RealizedPnl, UpdatedAt = p.UpdatedAt, Version = 1 });
            return;
        }
        if (r.Version != expectedVersion) throw new DbUpdateConcurrencyException($"Portfolio {p.Id} version conflict. Expected {expectedVersion}, actual {r.Version}.");
        r.Cash = p.Cash; r.RealizedPnl = p.RealizedPnl; r.UpdatedAt = p.UpdatedAt; r.Version = checked(expectedVersion + 1);
    }
    public async Task SavePositionAsync(PositionState p, CancellationToken ct)
    {
        var r = await db.Positions.SingleOrDefaultAsync(x => x.Id == p.Id, ct);
        if (r is null) { r = new PositionRecord { Id = p.Id }; db.Positions.Add(r); }
        r.PortfolioId = p.PortfolioId; r.Symbol = p.Symbol.Value; r.InstrumentToken = p.InstrumentToken ?? p.Symbol.InstrumentToken; r.Quantity = p.Quantity; r.AverageEntryPrice = p.AverageEntryPrice; r.CurrentMarketPrice = p.CurrentMarketPrice; r.StopLoss = p.StopLoss; r.OpenedAt = p.OpenedAt; r.UpdatedAt = p.UpdatedAt;
    }
    public async Task DeletePositionAsync(Guid id, CancellationToken ct) { var r = await db.Positions.SingleOrDefaultAsync(x => x.Id == id, ct); if (r is not null) db.Positions.Remove(r); }
}

public sealed class EfOrderRepository(TradingDbContext db) : IOrderRepository
{
    public async Task<OrderState?> GetAsync(Guid id, CancellationToken ct) => await db.Orders.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct) is { } r ? r.ToState() : null;
    public Task AddAsync(OrderState o, CancellationToken ct) { db.Orders.Add(new OrderRecord { Id=o.Id, Symbol=o.Symbol.Value, InstrumentToken=o.InstrumentToken ?? o.Symbol.InstrumentToken, Side=o.Side.ToString(), Quantity=o.Quantity, LimitPrice=o.LimitPrice, StrategyVersion=o.StrategyVersion, CreatedAt=o.CreatedAt, ExecutionMode=o.ExecutionMode, Status=o.Status }); return Task.CompletedTask; }
    public async Task<FillState?> GetFillByOrderIdAsync(Guid id, CancellationToken ct) => await db.Fills.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == id, ct) is { } r ? r.ToState() : null;
    public Task AddFillAsync(FillState f, CancellationToken ct) { db.Fills.Add(new FillRecord { Id=f.Id, OrderId=f.OrderId, Symbol=f.Symbol.Value, Side=f.Side.ToString(), Quantity=f.Quantity, FillPrice=f.FillPrice, FilledAt=f.FilledAt, ExecutionProvider=f.ExecutionProvider }); return Task.CompletedTask; }
}

public sealed class EfAlertRepository(TradingDbContext db) : IAlertRepository
{
    public async Task<bool> TryAddAsync(AlertState a, CancellationToken ct)
    {
        if (await db.Alerts.AnyAsync(x => x.PositionId == a.PositionId && x.Rule == a.Rule && x.EvaluationBucket == a.EvaluationBucket, ct)) return false;
        db.Alerts.Add(new AlertRecord { Id=a.Id, PositionId=a.PositionId, Symbol=a.Symbol?.Value, Rule=a.Rule, Severity=a.Severity.ToString(), Message=a.Message, EvaluationBucket=a.EvaluationBucket, CreatedAt=a.CreatedAt });
        return true;
    }
    public async Task<IReadOnlyList<AlertState>> GetAllAsync(CancellationToken ct)
    {
        var records = await db.Alerts.AsNoTracking().OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return records.Select(x => x.ToState()).ToList();
    }
}

public sealed class EfMarketDataSnapshotRepository(TradingDbContext db) : IMarketDataSnapshotRepository
{
    public Task AddAsync(MarketDataSnapshotState s, CancellationToken ct) { db.MarketDataSnapshots.Add(new MarketDataSnapshotRecord { Id=s.Id, Symbol=s.Symbol.Value, InstrumentToken=s.InstrumentToken ?? s.Symbol.InstrumentToken, Provider=s.Provider, Exchange=s.Exchange, ProviderTimestamp=s.ProviderTimestamp, ReceivedAt=s.ReceivedAt, Open=s.Open, High=s.High, Low=s.Low, Close=s.Close, LastTradedPrice=s.LastTradedPrice, Volume=s.Volume }); return Task.CompletedTask; }
    public async Task<MarketDataSnapshotState?> GetLatestAsync(Symbol symbol, CancellationToken ct) => await db.MarketDataSnapshots.AsNoTracking().Where(x => x.Symbol == symbol.Value).OrderByDescending(x => x.ProviderTimestamp).FirstOrDefaultAsync(ct) is { } r ? r.ToState() : null;
}

public sealed class EfTradingUnitOfWork(TradingDbContext db) : ITradingUnitOfWork
{
    private IDbContextTransaction? transaction;
    public IPortfolioRepository Portfolios { get; } = new EfPortfolioRepository(db);
    public IOrderRepository Orders { get; } = new EfOrderRepository(db);
    public IAlertRepository Alerts { get; } = new EfAlertRepository(db);
    public IMarketDataSnapshotRepository MarketDataSnapshots { get; } = new EfMarketDataSnapshotRepository(db);
    public async Task CommitAsync(CancellationToken ct)
    {
        transaction ??= await db.Database.BeginTransactionAsync(ct);
        try { await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); await transaction.DisposeAsync(); transaction = null; }
        catch { await transaction.RollbackAsync(CancellationToken.None); await transaction.DisposeAsync(); transaction = null; throw; }
    }
    public async ValueTask DisposeAsync() { if (transaction is not null) await transaction.DisposeAsync(); await db.DisposeAsync(); }
}

public sealed class EfTradingUnitOfWorkFactory(IDbContextFactory<TradingDbContext> factory) : ITradingUnitOfWorkFactory
{
    public async Task<ITradingUnitOfWork> CreateAsync(CancellationToken ct) => new EfTradingUnitOfWork(await factory.CreateDbContextAsync(ct));
}
