using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurablePaperTradingPortfolioIntegrityTests
{
    [Fact]
    public async Task Concurrent_Idempotency_Key_With_Different_Portfolio_Is_Rejected()
    {
        var firstPortfolioId = Guid.NewGuid();
        var secondPortfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(firstPortfolioId);
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var first = Task.Run(() => service.ExecuteAsync(firstPortfolioId, orderId, "shared-portfolio", symbol, 1, CancellationToken.None));
        await execution.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = service.ExecuteAsync(secondPortfolioId, orderId, "shared-portfolio", symbol, 1, CancellationToken.None);
        execution.Release.TrySetResult(true);
        await first;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () => await second);
        Assert.Equal("The idempotency key is already associated with a different portfolio.", error.Message);
        Assert.Equal(1, execution.CallCount);
    }

    [Fact]
    public async Task Concurrent_Idempotency_Key_With_Same_Portfolio_Still_Replays()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var first = Task.Run(() => service.ExecuteAsync(portfolioId, orderId, "shared-same-portfolio", symbol, 1, CancellationToken.None));
        await execution.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = service.ExecuteAsync(portfolioId, orderId, "shared-same-portfolio", symbol, 1, CancellationToken.None);
        execution.Release.TrySetResult(true);
        var results = await Task.WhenAll(first, second);

        Assert.Equal(1, execution.CallCount);
        Assert.Equal(results[0].Fill, results[1].Fill);
    }

    private static DurablePaperTradingService CreateService(FakeUnitOfWork unitOfWork, IPaperExecutionProvider execution) =>
        new(new RecommendationService(new FakeMarketData()), new RiskEngine(), execution, new FakeUnitOfWorkFactory(unitOfWork), 10_000m);

    private sealed class BlockingExecution : IPaperExecutionProvider
    {
        public int CallCount { get; private set; }
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<Fill> ExecuteAsync(PaperOrder order, CancellationToken cancellationToken)
        {
            CallCount++;
            Started.TrySetResult(true);
            await Release.Task.WaitAsync(cancellationToken);
            return new Fill(order.Id, order.Symbol, order.Side, order.Quantity, order.LimitPrice, DateTimeOffset.UtcNow, "paper-test");
        }
    }

    private sealed class FakeMarketData : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken)
        {
            var now = DateTimeOffset.UtcNow;
            return Task.FromResult(new MarketQuote(symbol, "NSE", symbol.InstrumentToken ?? "123", now, 100, 101, 99, 101, 101, 1000, "test"));
        }

        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
        {
            var start = to.AddMinutes(-19);
            var closes = new[] { 100m,100m,100m,100m,100m,100m,100m,100m,100m,100m,99m,99m,99m,99m,99m,99m,99m,99m,99m,101m };
            var candles = closes.Select((close, index) => new Candle(start.AddMinutes(index), close, close + 1, close - 1, close, 1000)).ToArray();
            return Task.FromResult<IReadOnlyList<Candle>>(candles);
        }
    }

    private sealed class FakeUnitOfWorkFactory(FakeUnitOfWork unitOfWork) : ITradingUnitOfWorkFactory
    {
        public Task<ITradingUnitOfWork> CreateAsync(CancellationToken cancellationToken) => Task.FromResult<ITradingUnitOfWork>(unitOfWork);
    }

    private sealed class FakeUnitOfWork(Guid portfolioId) : ITradingUnitOfWork
    {
        public FakePortfolioRepository PortfolioStore { get; } = new(portfolioId);
        public FakeOrderRepository OrdersStore { get; } = new();
        public IPortfolioRepository Portfolios => PortfolioStore;
        public IOrderRepository Orders => OrdersStore;
        public IDurableShortPositionRepository DurableShortPositions { get; } = new NoOpDurableShortPositionRepository();
        public IPaperTradingEventAuditRepository PaperTradingEventAudits { get; } = new NoOpPaperTradingEventAuditRepository();
        public IAlertRepository Alerts { get; } = new NoOpAlertRepository();
        public IMarketDataSnapshotRepository MarketDataSnapshots { get; } = new NoOpMarketDataRepository();
        public IHistoricalCandleRepository HistoricalCandles { get; } = new NoOpHistoricalCandleRepository();
        public IBacktestRunRepository BacktestRuns { get; } = new NoOpBacktestRunRepository();
        public IBacktestAuditRepository BacktestAudit { get; } = new NoOpBacktestAuditRepository();
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakePortfolioRepository(Guid portfolioId) : IPortfolioRepository
    {
        private readonly PortfolioState portfolio = new(portfolioId, 10_000m, 0m, DateTimeOffset.UtcNow, 0, 10_000m);
        public Task<PortfolioState?> GetAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<PortfolioState?>(id == portfolio.Id ? portfolio : null);
        public Task<IReadOnlyList<PositionState>> GetOpenPositionsAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PositionState>>([]);
        public Task SaveAsync(PortfolioState state, long expectedVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SavePositionAsync(PositionState position, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeletePositionAsync(Guid positionId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly Dictionary<Guid, (OrderState Order, FillState? Fill)> orders = [];
        public Task<OrderState?> GetAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult(orders.TryGetValue(orderId, out var value) ? value.Order : null);
        public Task<OrderState?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(orders.Values.Select(x => x.Order).SingleOrDefault(x => x.IdempotencyKey == idempotencyKey));
        public Task<IReadOnlyList<OrderState>> GetByIdempotencyPrefixAsync(string prefix, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OrderState>>([]);
        public Task<IReadOnlyList<FillState>> GetFillsByOrderIdsAsync(IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FillState>>([]);
        public Task AddAsync(OrderState order, CancellationToken cancellationToken) { orders[order.Id] = (order, null); return Task.CompletedTask; }
        public Task<FillState?> GetFillByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult(orders.TryGetValue(orderId, out var value) ? value.Fill : null);
        public Task AddFillAsync(FillState fill, CancellationToken cancellationToken) { if (orders.TryGetValue(fill.OrderId, out var value)) orders[fill.OrderId] = (value.Order, fill); return Task.CompletedTask; }
    }

    private sealed class NoOpDurableShortPositionRepository : IDurableShortPositionRepository
    {
        public Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult<DurableShortPositionState?>(null);
        public Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DurableShortCoverState>>([]);
        public Task AddAsync(DurableShortPositionState position, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }
}
