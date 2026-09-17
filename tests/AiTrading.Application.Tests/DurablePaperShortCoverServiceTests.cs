using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurablePaperShortCoverServiceTests
{
    [Fact]
    public async Task Open_then_full_cover_persists_closed_state()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);
        var now = DateTimeOffset.UtcNow;
        var opened = await service.OpenAsync(Guid.NewGuid(), new Symbol("TEST"), 5, 100m, now, CancellationToken.None);

        var covered = await service.CoverAsync(opened.Id, "cover-1", 92m, 5, opened.Version, now.AddMinutes(1), CancellationToken.None);

        Assert.Equal(0, covered.RemainingQuantity);
        Assert.Equal(40m, covered.RealizedPnl);
        Assert.Equal("SHORT_CLOSED", covered.State);
        Assert.Equal(1, covered.Version);
        Assert.Equal("cover-1", repository.LastKey);
    }

    [Fact]
    public async Task Partial_cover_preserves_remaining_quantity_and_accumulates_pnl()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);
        var opened = await service.OpenAsync(Guid.NewGuid(), new Symbol("TEST"), 10, 100m, DateTimeOffset.UtcNow, CancellationToken.None);

        var covered = await service.CoverAsync(opened.Id, "cover-1", 96m, 4, opened.Version, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(6, covered.RemainingQuantity);
        Assert.Equal(16m, covered.RealizedPnl);
        Assert.Equal("SHORT_PARTIALLY_COVERED", covered.State);
    }

    [Fact]
    public async Task Open_replay_returns_existing_position_without_duplicate_add()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);
        var positionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var first = await service.OpenAsync(positionId, Guid.NewGuid(), new Symbol("TEST"), 3, 100m, now, CancellationToken.None);
        var replay = await service.OpenAsync(positionId, first.PortfolioId, first.Symbol, first.OriginalQuantity, first.AverageEntryPrice, now.AddSeconds(1), CancellationToken.None);

        Assert.Equal(first, replay);
        Assert.Equal(1, repository.AddCount);
    }

    [Fact]
    public async Task Open_persists_short_stop_and_target_levels()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);

        var opened = await service.OpenAsync(Guid.NewGuid(), Guid.NewGuid(), new Symbol("TEST"), 3, 100m, DateTimeOffset.UtcNow, 105m, 95m, CancellationToken.None);

        Assert.Equal(105m, opened.StopLoss);
        Assert.Equal(95m, opened.TargetPrice);
    }

    [Fact]
    public async Task Open_rejects_partial_short_risk_levels()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() => service.OpenAsync(Guid.NewGuid(), Guid.NewGuid(), new Symbol("TEST"), 3, 100m, DateTimeOffset.UtcNow, 105m, null, CancellationToken.None));
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task Open_replay_rejects_changed_short_risk_levels()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);
        var positionId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var first = await service.OpenAsync(positionId, portfolioId, new Symbol("TEST"), 3, 100m, now, 105m, 95m, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenAsync(positionId, first.PortfolioId, first.Symbol, first.OriginalQuantity, first.AverageEntryPrice, now.AddSeconds(1), 106m, 95m, CancellationToken.None));
    }

    [Fact]
    public async Task Open_replay_keeps_legacy_position_without_risk_levels_unchanged()
    {
        var repository = new FakeRepository
        {
            SeedPosition = new DurableShortPositionState(Guid.NewGuid(), Guid.NewGuid(), new Symbol("TEST"), 3, 3, 100m, null, null, null, 0m, "SHORT_OPEN", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0)
        };
        var service = new DurablePaperShortCoverService(repository);

        var replay = await service.OpenAsync(repository.SeedPosition.Id, repository.SeedPosition.PortfolioId, repository.SeedPosition.Symbol, 3, 100m, DateTimeOffset.UtcNow, 105m, 95m, CancellationToken.None);

        Assert.Equal(repository.SeedPosition, replay);
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task Invalid_cover_values_are_rejected_before_repository_call()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);
        var opened = await service.OpenAsync(Guid.NewGuid(), new Symbol("TEST"), 2, 100m, DateTimeOffset.UtcNow, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CoverAsync(opened.Id, "cover-1", 0m, 1, opened.Version, DateTimeOffset.UtcNow, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CoverAsync(opened.Id, "cover-2", 95m, 0, opened.Version, DateTimeOffset.UtcNow, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CoverAsync(opened.Id, "cover-3", 95m, 1, -1, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Null(repository.LastKey);
    }

    [Fact]
    public async Task Autonomous_lifecycle_creates_position_from_confirmed_paper_fill()
    {
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TEST", "123");
        var fill = new FillState(orderId, orderId, symbol, OrderSide.Sell, 5, 100m, DateTimeOffset.UtcNow, "paper");
        var coordinator = new FakeCoordinator(new(new(RiskDecision.Approved, null), fill, "executed", 105m, 95m));
        var repository = new FakeRepository();
        var service = new DurableAutonomousPaperShortLifecycleService(coordinator, new DurablePaperShortCoverService(repository));
        var portfolioId = Guid.NewGuid();

        var result = await service.ExecuteAsync(Guid.NewGuid(), portfolioId, symbol, null!, null!, 5, CancellationToken.None);

        Assert.NotNull(result.Position);
        Assert.Equal(orderId, result.Position!.Id);
        Assert.Equal(portfolioId, result.Position.PortfolioId);
        Assert.Equal(5, result.Position.RemainingQuantity);
        Assert.Equal(100m, result.Position.AverageEntryPrice);
        Assert.Equal(105m, result.Position.StopLoss);
        Assert.Equal(95m, result.Position.TargetPrice);
        Assert.Equal("executed-and-positioned", result.Execution.Status);
        Assert.Equal(1, repository.AddCount);
    }

    [Fact]
    public async Task Autonomous_lifecycle_replay_reuses_position_identity_without_duplicate_add()
    {
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TEST", "123");
        var fill = new FillState(orderId, orderId, symbol, OrderSide.Sell, 3, 100m, DateTimeOffset.UtcNow, "paper");
        var coordinator = new FakeCoordinator(new(new(RiskDecision.Approved, null), fill, "already-executed", 105m, 95m));
        var repository = new FakeRepository();
        var service = new DurableAutonomousPaperShortLifecycleService(coordinator, new DurablePaperShortCoverService(repository));
        var portfolioId = Guid.NewGuid();

        var first = await service.ExecuteAsync(Guid.NewGuid(), portfolioId, symbol, null!, null!, 3, CancellationToken.None);
        var replay = await service.ExecuteAsync(Guid.NewGuid(), portfolioId, symbol, null!, null!, 3, CancellationToken.None);

        Assert.Equal(first.Position, replay.Position);
        Assert.Equal("already-positioned", replay.Execution.Status);
        Assert.Equal(1, repository.AddCount);
    }

    [Fact]
    public async Task Autonomous_lifecycle_does_not_create_position_without_confirmed_fill()
    {
        var coordinator = new FakeCoordinator(new(new(RiskDecision.RiskBlocked, "blocked"), null, "no-trade"));
        var repository = new FakeRepository();
        var service = new DurableAutonomousPaperShortLifecycleService(coordinator, new DurablePaperShortCoverService(repository));

        var result = await service.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), new Symbol("TEST", "123"), null!, null!, 1, CancellationToken.None);

        Assert.Null(result.Position);
        Assert.Equal("no-trade", result.Execution.Status);
        Assert.Equal(0, repository.AddCount);
    }

    private sealed class FakeCoordinator(PaperShortExecutionResult result) : IAutonomousPaperShortExecutionCoordinator
    {
        public Task<PaperShortExecutionResult> ExecuteAsync(Guid sessionId, Symbol symbol, Recommendation recommendation, MarketQuote quote, int quantity, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class FakeRepository : IDurableShortPositionRepository
    {
        private DurableShortPositionState? position;
        public DurableShortPositionState? SeedPosition { get => position; set => position = value; }
        public string? LastKey { get; private set; }
        public int AddCount { get; private set; }
        public Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult(position?.Id == positionId ? position : null);
        public Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DurableShortCoverState>>([]);
        public Task AddAsync(DurableShortPositionState value, CancellationToken cancellationToken) { position = value; AddCount++; return Task.CompletedTask; }
        public Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken)
        {
            LastKey = idempotencyKey;
            if (position is null) throw new KeyNotFoundException();
            if (position.Version != expectedVersion) throw new InvalidOperationException("stale");
            var pnl = PaperShortAccounting.RealizedPnl(position.AverageEntryPrice, coverPrice, coverQuantity);
            var remaining = position.RemainingQuantity - coverQuantity;
            position = position with { RemainingQuantity = remaining, LastCoverPrice = coverPrice, RealizedPnl = position.RealizedPnl + pnl, State = remaining == 0 ? "SHORT_CLOSED" : "SHORT_PARTIALLY_COVERED", UpdatedAt = now, Version = position.Version + 1 };
            return Task.FromResult(position);
        }
    }
}
