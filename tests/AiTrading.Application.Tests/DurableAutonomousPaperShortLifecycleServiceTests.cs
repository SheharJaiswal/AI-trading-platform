using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurableAutonomousPaperShortLifecycleServiceTests
{
    [Fact]
    public async Task ExecuteAsync_creates_position_only_after_confirmed_fill_and_uses_fill_identity()
    {
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TEST", "123");
        var fill = new FillState(orderId, orderId, symbol, OrderSide.Sell, 3, 98m, DateTimeOffset.UtcNow, "paper");
        var coordinator = new FakeCoordinator(new PaperShortExecutionResult(new(RiskDecision.Approved, null), fill, "executed"));
        var repository = new FakeRepository();
        var service = new DurableAutonomousPaperShortLifecycleService(coordinator, new DurablePaperShortCoverService(repository));

        var result = await service.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), symbol, SellRecommendation(symbol), Quote(symbol), 3, CancellationToken.None);

        Assert.Equal("executed-and-positioned", result.Execution.Status);
        Assert.NotNull(result.Position);
        Assert.Equal(orderId, result.Position!.Id);
        Assert.Equal(orderId, repository.Added!.Id);
        Assert.Equal(1, repository.AddCount);
    }

    [Fact]
    public async Task ExecuteAsync_does_not_create_position_without_fill()
    {
        var coordinator = new FakeCoordinator(new PaperShortExecutionResult(new(RiskDecision.RiskBlocked, "STALE_OR_INVALID_MARKET_DATA"), null, "no-trade"));
        var repository = new FakeRepository();
        var service = new DurableAutonomousPaperShortLifecycleService(coordinator, new DurablePaperShortCoverService(repository));

        var symbol = new Symbol("TEST", "123");
        var result = await service.ExecuteAsync(Guid.NewGuid(), Guid.NewGuid(), symbol, SellRecommendation(symbol), Quote(symbol), 1, CancellationToken.None);

        Assert.Equal("no-trade", result.Execution.Status);
        Assert.Null(result.Position);
        Assert.Equal(0, repository.AddCount);
    }

    private static Recommendation SellRecommendation(Symbol symbol) => new(symbol, RecommendationAction.Sell, 100m, null, .8m, 1, ["bearish"], [], DateTimeOffset.UtcNow, "baseline-v1");
    private static MarketQuote Quote(Symbol symbol) => new(symbol, "NSE", symbol.InstrumentToken!, DateTimeOffset.UtcNow, 100m, 101m, 99m, 100m, 100m, 1000, "test");

    private sealed class FakeCoordinator(PaperShortExecutionResult result) : IAutonomousPaperShortExecutionCoordinator
    {
        public Task<PaperShortExecutionResult> ExecuteAsync(Guid sessionId, Symbol symbol, Recommendation recommendation, MarketQuote quote, int quantity, CancellationToken cancellationToken)
            => Task.FromResult(result);
    }

    private sealed class FakeRepository : IDurableShortPositionRepository
    {
        public DurableShortPositionState? Added { get; private set; }
        public int AddCount { get; private set; }
        public Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken ct) => Task.FromResult<DurableShortPositionState?>(null);
        public Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken ct) => Task.FromResult<IReadOnlyList<DurableShortCoverState>>([]);
        public Task AddAsync(DurableShortPositionState position, CancellationToken ct) { Added = position; AddCount++; return Task.CompletedTask; }
        public Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
