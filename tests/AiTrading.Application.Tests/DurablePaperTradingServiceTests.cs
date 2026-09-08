using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurablePaperTradingServiceTests
{
    [Fact]
    public async Task Duplicate_Idempotency_Key_Returns_Existing_Fill_Without_Executing_Again()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var fill = new FillState(Guid.NewGuid(), orderId, symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper");
        var unitOfWork = new FakeUnitOfWork(new PortfolioState(portfolioId, 10_000m, 0m, DateTimeOffset.UtcNow, 1));
        unitOfWork.OrdersStore.Add(new OrderState(orderId, "request-123", symbol, "123", OrderSide.Buy, 1, 100m, "baseline-v1", DateTimeOffset.UtcNow, "paper", "filled"), fill);
        var execution = new CountingExecution();
        var service = CreateService(unitOfWork, execution);

        var result = await service.ExecuteAsync(portfolioId, orderId, "request-123", symbol, 1, CancellationToken.None);

        Assert.Equal(RiskDecision.Approved, result.Risk.Decision);
        Assert.Equal(fill, result.Fill);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Reusing_Idempotency_Key_For_Different_Order_Is_Rejected()
    {
        var portfolioId = Guid.NewGuid();
        var firstOrderId = Guid.NewGuid();
        var secondOrderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(new PortfolioState(portfolioId, 10_000m, 0m, DateTimeOffset.UtcNow, 1));
        unitOfWork.OrdersStore.Add(
            new OrderState(firstOrderId, "request-123", symbol, "123", OrderSide.Buy, 1, 100m, "baseline-v1", DateTimeOffset.UtcNow, "paper", "filled"),
            new FillState(Guid.NewGuid(), firstOrderId, symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(unitOfWork, new CountingExecution()).ExecuteAsync(
            portfolioId, secondOrderId, "request-123", symbol, 1, CancellationToken.None));
    }

    [Fact]
    public async Task Existing_Order_Without_Fill_Is_Not_Silently_Approved()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var unitOfWork = new FakeUnitOfWork(new PortfolioState(portfolioId, 10_000m, 0m, DateTimeOffset.UtcNow, 1));
        unitOfWork.OrdersStore.Add(
            new OrderState(orderId, "request-123", symbol, "123", OrderSide.Buy, 1, 100m, "baseline-v1", DateTimeOffset.UtcNow, "paper", "created"),
            null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateService(unitOfWork, new CountingExecution()).ExecuteAsync(
            portfolioId, orderId, "request-123", symbol, 1, CancellationToken.None));
    }

    private static DurablePaperTradingService CreateService(FakeUnitOfWork unitOfWork, CountingExecution execution) =>
        new(new RecommendationService(new FakeMarketData()), new RiskEngine(), execution, new FakeUnitOfWorkFactory(unitOfWork), 10_000m);

    private sealed class CountingExecution : IPaperExecutionProvider
    {
        public int CallCount { get; private set; }
        public Task<Fill> ExecuteAsync(PaperOrder order, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new Fill(order.Id, order.Symbol, order.Side, order.Quantity, order.LimitPrice, DateTimeOffset.UtcNow, "paper-test"));
        }
    }

    private sealed class FakeMarketData : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken) =>
            Task.FromResult(new MarketQuote(symbol, "NSE", symbol.InstrumentToken ?? "123", DateTimeOffset.UtcNow, 100, 101, 99, 100, 100, 1000, "test"));

        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Candle>>([]);
    }

    private sealed class FakeUnitOfWorkFactory(FakeUnitOfWork unitOfWork) : ITradingUnitOfWorkFactory
    {
        public Task<ITradingUnitOfWork> CreateAsync(CancellationToken cancellationToken) => Task.FromResult<ITradingUnitOfWork>(unitOfWork);
    }

    private sealed class FakeUnitOfWork(PortfolioState portfolio) : ITradingUnitOfWork
    {
        public FakePortfolioRepository PortfolioStore { get; } = new(portfolio);
        public FakeOrderRepository OrdersStore { get; } = new();
        public IPortfolioRepository Portfolios => PortfolioStore;
        public IOrderRepository Orders => OrdersStore;
        public IAlertRepository Alerts { get; } = new NoOpAlertRepository();
        public IMarketDataSnapshotRepository MarketDataSnapshots { get; } = new NoOpMarketDataRepository();
        public IHistoricalCandleRepository HistoricalCandles { get; } = new NoOpHistoricalCandleRepository();
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakePortfolioRepository(PortfolioState portfolio) : IPortfolioRepository
    {
        public Task<PortfolioState?> GetAsync(Guid portfolioId, CancellationToken cancellationToken) => Task.FromResult(portfolio.Id == portfolioId ? portfolio : null);
        public Task<IReadOnlyList<PositionState>> GetOpenPositionsAsync(Guid portfolioId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PositionState>>([]);
        public Task SaveAsync(PortfolioState portfolio, long expectedVersion, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SavePositionAsync(PositionState position, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeletePositionAsync(Guid positionId, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly Dictionary<Guid, (OrderState Order, FillState? Fill)> orders = [];
        public void Add(OrderState order, FillState? fill) => orders[order.Id] = (order, fill);
        public Task<OrderState?> GetAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult(orders.TryGetValue(orderId, out var value) ? value.Order : null);
        public Task<OrderState?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult(orders.Values.Select(x => x.Order).SingleOrDefault(x => x.IdempotencyKey == idempotencyKey));
        public Task AddAsync(OrderState order, CancellationToken cancellationToken) { orders[order.Id] = (order, null); return Task.CompletedTask; }
        public Task<FillState?> GetFillByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult(orders.TryGetValue(orderId, out var value) ? value.Fill : null);
        public Task AddFillAsync(FillState fill, CancellationToken cancellationToken) { var order = orders[fill.OrderId].Order; orders[fill.OrderId] = (order, fill); return Task.CompletedTask; }
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
}
