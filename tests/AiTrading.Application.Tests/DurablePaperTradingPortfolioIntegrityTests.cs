using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurablePaperTradingPortfolioIntegrityTests
{
    [Fact]
    public async Task NonPositive_Order_Quantity_Is_Rejected_Before_Execution()
    {
        var service = CreateService(new FakeUnitOfWork(Guid.NewGuid()), new BlockingExecution());

        var error = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), "invalid-quantity", new Symbol("TCS", "123"), 0, CancellationToken.None));

        Assert.Equal("quantity", error.ParamName);
    }

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

    [Fact]
    public async Task Existing_Order_With_Different_Idempotency_Key_Fails_Closed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        unitOfWork.OrdersStore.Seed(
            new OrderState(orderId, "original-key", symbol, symbol.InstrumentToken, OrderSide.Buy, 1, 100m, "strategy", DateTimeOffset.UtcNow, "paper", "filled"),
            new FillState(orderId, orderId, symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper"));
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "different-key", symbol, 1, CancellationToken.None));

        Assert.Equal($"Order {orderId} is already associated with a different idempotency key; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Persisted_Idempotent_Replay_With_NonPositive_Fill_Price_Fails_Closed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        unitOfWork.OrdersStore.Seed(
            new OrderState(orderId, "persisted-invalid-price", symbol, symbol.InstrumentToken, OrderSide.Buy, 1, 100m, "strategy", DateTimeOffset.UtcNow, "paper", "filled"),
            new FillState(orderId, orderId, symbol, OrderSide.Buy, 1, 0m, DateTimeOffset.UtcNow, "paper"));
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "persisted-invalid-price", symbol, 1, CancellationToken.None));

        Assert.Equal($"Order {orderId} has an invalid paper fill price; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Persisted_Idempotent_Replay_With_NonPositive_Order_Quantity_Fails_Closed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        var idempotencyKey = $"persisted-invalid-order-quantity-{Guid.NewGuid():N}";
        unitOfWork.OrdersStore.Seed(
            new OrderState(orderId, idempotencyKey, symbol, symbol.InstrumentToken, OrderSide.Buy, 0, 100m, "strategy", DateTimeOffset.UtcNow, "paper", "filled"),
            new FillState(orderId, orderId, symbol, OrderSide.Buy, 0, 100m, DateTimeOffset.UtcNow, "paper"));
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, idempotencyKey, symbol, 1, CancellationToken.None));

        Assert.Equal($"Order {orderId} has an invalid persisted paper quantity; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Persisted_Idempotent_Replay_With_NonPositive_Order_Limit_Price_Fails_Closed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        unitOfWork.OrdersStore.Seed(
            new OrderState(orderId, "persisted-invalid-order-price", symbol, symbol.InstrumentToken, OrderSide.Buy, 1, 0m, "strategy", DateTimeOffset.UtcNow, "paper", "filled"),
            new FillState(orderId, orderId, symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper"));
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "persisted-invalid-order-price", symbol, 1, CancellationToken.None));

        Assert.Equal($"Order {orderId} has an invalid persisted paper limit price; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Persisted_Idempotent_Replay_With_Default_Fill_Timestamp_Fails_Closed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        unitOfWork.OrdersStore.Seed(
            new OrderState(orderId, "persisted-invalid-timestamp", symbol, symbol.InstrumentToken, OrderSide.Buy, 1, 100m, "strategy", DateTimeOffset.UtcNow, "paper", "filled"),
            new FillState(orderId, orderId, symbol, OrderSide.Buy, 1, 100m, default, "paper"));
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "persisted-invalid-timestamp", symbol, 1, CancellationToken.None));

        Assert.Equal($"Order {orderId} has an invalid paper fill timestamp; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Persisted_Idempotent_Replay_With_NonFilled_Order_Status_Fails_Closed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        unitOfWork.OrdersStore.Seed(
            new OrderState(orderId, "persisted-pending", symbol, symbol.InstrumentToken, OrderSide.Buy, 1, 100m, "strategy", DateTimeOffset.UtcNow, "paper", "pending"),
            new FillState(orderId, orderId, symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper"));
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "persisted-pending", symbol, 1, CancellationToken.None));

        Assert.Equal($"Order {orderId} has an invalid persisted paper execution state; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Persisted_Idempotent_Replay_With_NonPaper_Execution_Mode_Fails_Closed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        unitOfWork.OrdersStore.Seed(
            new OrderState(orderId, "persisted-live", symbol, symbol.InstrumentToken, OrderSide.Buy, 1, 100m, "strategy", DateTimeOffset.UtcNow, "live", "filled"),
            new FillState(orderId, orderId, symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper"));
        var execution = new BlockingExecution();
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "persisted-live", symbol, 1, CancellationToken.None));

        Assert.Equal($"Order {orderId} has an invalid persisted paper execution state; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Provider_Returning_Fill_Before_Order_Creation_Fails_Before_Persistence()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        var execution = new InvalidFillExecution(new Fill(orderId, symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow.AddMinutes(-1), "paper-test"));
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "provider-invalid-timestamp", symbol, 1, CancellationToken.None));

        Assert.Equal($"Paper execution provider returned an inconsistent fill for order {orderId}; execution state requires reconciliation.", error.Message);
        Assert.Equal(1, execution.CallCount);
        Assert.Null(await unitOfWork.OrdersStore.GetAsync(orderId, CancellationToken.None));
    }

    [Fact]
    public async Task Provider_Returning_NonPositive_Fill_Price_Fails_Before_Persistence()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(portfolioId);
        var execution = new InvalidFillExecution(new Fill(orderId, symbol, OrderSide.Buy, 1, 0m, DateTimeOffset.UtcNow, "paper-test"));
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "provider-invalid-price", symbol, 1, CancellationToken.None));

        Assert.Equal($"Paper execution provider returned an inconsistent fill for order {orderId}; execution state requires reconciliation.", error.Message);
        Assert.Equal(1, execution.CallCount);
        Assert.Null(await unitOfWork.OrdersStore.GetAsync(orderId, CancellationToken.None));
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

    private sealed class InvalidFillExecution(Fill fill) : IPaperExecutionProvider
    {
        public int CallCount { get; private set; }

        public Task<Fill> ExecuteAsync(PaperOrder order, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(fill);
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
        public void Seed(OrderState order, FillState fill) => orders[order.Id] = (order, fill);
    }

    private sealed class NoOpDurableShortPositionRepository : IDurableShortPositionRepository
    {
        public Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult<DurableShortPositionState?>(null);
        public Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DurableShortCoverState>>([]);
        public Task AddAsync(DurableShortPositionState position, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken) => throw new InvalidOperationException();
    }

    private sealed class NoOpPaperTradingEventAuditRepository : IPaperTradingEventAuditRepository
    {
        public Task AddAsync(PaperTradingEventAuditState audit, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<PaperTradingEventAuditState?> GetBySessionAndEventAsync(Guid sessionId, string eventId, CancellationToken cancellationToken) => Task.FromResult<PaperTradingEventAuditState?>(null);
        public Task<IReadOnlyList<PaperTradingEventAuditState>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PaperTradingEventAuditState>>([]);
    }

    private sealed class NoOpAlertRepository : IAlertRepository
    {
        public Task<bool> TryAddAsync(AlertState alert, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<IReadOnlyList<AlertState>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<AlertState>>([]);
    }

    private sealed class NoOpMarketDataRepository : IMarketDataSnapshotRepository
    {
        public Task AddAsync(MarketDataSnapshotState snapshot, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<MarketDataSnapshotState?> GetLatestAsync(Symbol symbol, CancellationToken cancellationToken) => Task.FromResult<MarketDataSnapshotState?>(null);
    }

    private sealed class NoOpHistoricalCandleRepository : IHistoricalCandleRepository
    {
        public Task AddRangeAsync(IReadOnlyList<HistoricalCandleState> candles, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<HistoricalCandleState>> GetRangeAsync(Symbol symbol, string interval, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<HistoricalCandleState>>([]);
    }

    private sealed class NoOpBacktestRunRepository : IBacktestRunRepository
    {
        public Task AddAsync(BacktestRunState run, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<BacktestRunState?> GetAsync(Guid runId, CancellationToken cancellationToken) => Task.FromResult<BacktestRunState?>(null);
    }

    private sealed class NoOpBacktestAuditRepository : IBacktestAuditRepository
    {
        public Task AddAsync(Guid runId, IReadOnlyList<BacktestTradeAuditState> trades, IReadOnlyList<BacktestRiskAuditState> riskEvents, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<(IReadOnlyList<BacktestTradeAuditState> Trades, IReadOnlyList<BacktestRiskAuditState> RiskEvents)> GetAsync(Guid runId, CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<BacktestTradeAuditState>, IReadOnlyList<BacktestRiskAuditState>)>(([], []));
    }
}
