using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurableShortRecoveryQueryServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsPositionCoversAndReconciliation()
    {
        var position = new DurableShortPositionState(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), new Symbol("DEMO", "123"), 5, 2, 100m, 104m, null, -12m, "SHORT_PARTIALLY_COVERED", DateTimeOffset.Parse("2026-09-14T00:00:00Z"), DateTimeOffset.Parse("2026-09-14T00:01:00Z"), 1);
        var cover = new DurableShortCoverState(
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), position.Id, "cover-1", 104m, 3, -12m, 1, DateTimeOffset.Parse("2026-09-14T00:01:00Z"));
        var service = new DurableShortRecoveryQueryService(new FakeUnitOfWorkFactory(position, cover));

        var result = await service.GetAsync(position.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(position.Id, result.Position.Id);
        Assert.Single(result.Covers);
        Assert.Equal(cover.RealizedPnl, result.Covers[0].RealizedPnl);
        Assert.True(result.Reconciliation.IsConsistent);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenPositionDoesNotExist()
    {
        var result = await new DurableShortRecoveryQueryService(new FakeUnitOfWorkFactory(null, null)).GetAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.Null(result);
    }

    private sealed class FakeUnitOfWorkFactory(DurableShortPositionState? position, DurableShortCoverState? cover) : ITradingUnitOfWorkFactory
    {
        public Task<ITradingUnitOfWork> CreateAsync(CancellationToken cancellationToken) => Task.FromResult<ITradingUnitOfWork>(new FakeUnitOfWork(position, cover));
    }

    private sealed class FakeUnitOfWork(DurableShortPositionState? position, DurableShortCoverState? cover) : ITradingUnitOfWork
    {
        public IPortfolioRepository Portfolios => throw new NotSupportedException();
        public IOrderRepository Orders => throw new NotSupportedException();
        public IDurableShortPositionRepository DurableShortPositions { get; } = new FakeRepository(position, cover);
        public IPaperTradingEventAuditRepository PaperTradingEventAudits => throw new NotSupportedException();
        public IAlertRepository Alerts => throw new NotSupportedException();
        public IMarketDataSnapshotRepository MarketDataSnapshots => throw new NotSupportedException();
        public IHistoricalCandleRepository HistoricalCandles => throw new NotSupportedException();
        public IBacktestRunRepository BacktestRuns => throw new NotSupportedException();
        public IBacktestAuditRepository BacktestAudit => throw new NotSupportedException();
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRepository(DurableShortPositionState? position, DurableShortCoverState? cover) : IDurableShortPositionRepository
    {
        public Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken ct) => Task.FromResult(position?.Id == positionId ? position : null);
        public Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken ct) => Task.FromResult<IReadOnlyList<DurableShortCoverState>>(cover is not null && cover.PositionId == positionId ? [cover] : []);
        public Task AddAsync(DurableShortPositionState position, CancellationToken ct) => throw new NotSupportedException();
        public Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken ct) => throw new NotSupportedException();
    }
}
