using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurablePaperTradingFillIntegrityTests
{
    [Theory]
    [InlineData("symbol")]
    [InlineData("quantity")]
    [InlineData("side")]
    [InlineData("order")]
    public async Task Persisted_Idempotent_Fill_Inconsistency_Fails_Closed(string mismatch)
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var persistedOrder = new OrderState(orderId, "integrity-check", symbol, "123", OrderSide.Buy, 2, 100m, "baseline-v1", DateTimeOffset.UtcNow, "paper", "filled");
        var fillOrderId = mismatch == "order" ? Guid.NewGuid() : orderId;
        var fillSymbol = mismatch == "symbol" ? new Symbol("INFY", "456") : symbol;
        var fillQuantity = mismatch == "quantity" ? 1 : 2;
        var fillSide = mismatch == "side" ? OrderSide.Sell : OrderSide.Buy;
        var persistedFill = new FillState(Guid.NewGuid(), fillOrderId, fillSymbol, fillSide, fillQuantity, 100m, DateTimeOffset.UtcNow, "paper");
        var unitOfWork = new FakeUnitOfWork(portfolioId, persistedOrder, persistedFill);
        var execution = new CountingExecution();
        var service = CreateService(unitOfWork, execution);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, "integrity-check", symbol, 2, CancellationToken.None));

        Assert.Equal($"Order {orderId} has an inconsistent paper fill; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, execution.CallCount);
    }

    [Fact]
    public async Task Persisted_Idempotent_Fill_With_Matching_Identity_Is_Replayed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var order = new OrderState(orderId, "integrity-valid", symbol, "123", OrderSide.Buy, 2, 100m, "baseline-v1", DateTimeOffset.UtcNow, "paper", "filled");
        var fill = new FillState(Guid.NewGuid(), orderId, symbol, OrderSide.Buy, 2, 101m, DateTimeOffset.UtcNow, "paper");
        var unitOfWork = new FakeUnitOfWork(portfolioId, order, fill);
        var execution = new CountingExecution();
        var result = await CreateService(unitOfWork, execution).ExecuteAsync(portfolioId, orderId, "integrity-valid", symbol, 2, CancellationToken.None);

        Assert.Equal(fill, result.Fill);
        Assert.Equal(RiskDecision.Approved, result.Risk.Decision);
        Assert.Equal(0, execution.CallCount);
    }

    private static DurablePaperTradingService CreateService(FakeUnitOfWork unitOfWork, IPaperExecutionProvider execution) =>
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

    private sealed class FakeUnitOfWork(Guid portfolioId, OrderState order, FillState fill) : ITradingUnitOfWork
    {
        public IPortfolioRepository Portfolios { get; } = new FakePortfolioRepository(portfolioId);
        public IOrderRepository Orders { get; } = new FakeOrderRepository(order, fill);
        public IDurableShortPositionRepository DurableShortPositions { get; } = new NoOpDurableShortPositionRepository();
        public IPaperTradingEventAuditRepository PaperTradingEventAudits { get; } = new NoOpPaperTradingEventAuditRepository();
        public IAlertRepository Alerts { get; } = new NoOpAlertRepository();
        public IMarketDataSnapshotRepository MarketDataSnapshots { get; } = new NoOpMarketDataRepository();
        public IHistoricalCandleRepository HistoricalCandles { get; } = new NoOpHistoricalCandleRepository();
        public IBacktestRunRepository BacktestRuns { get; } = new NoOpBacktestRunRepository();
        public IBacktestAuditRepository BacktestAudit { get; } = new NoOpBacktestAuditRepository();
        public ILiveOrderStateRepository LiveOrders => throw new NotSupportedException();
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

    private sealed class FakeOrderRepository(OrderState order, FillState fill) : IOrderRepository
    {
        public Task<OrderState?> GetAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult<OrderState?>(orderId == order.Id ? order : null);
        public Task<OrderState?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult<OrderState?>(idempotencyKey == order.IdempotencyKey ? order : null);
        public Task<IReadOnlyList<OrderState>> GetByIdempotencyPrefixAsync(string prefix, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OrderState>>([]);
        public Task<IReadOnlyList<FillState>> GetFillsByOrderIdsAsync(IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FillState>>([]);
        public Task AddAsync(OrderState order, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<FillState?> GetFillByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult<FillState?>(orderId == order.Id ? fill : null);
        public Task AddFillAsync(FillState fill, CancellationToken cancellationToken) => Task.CompletedTask;
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
        public Task<(IReadOnlyList<BacktestTradeAuditState> Trades, IReadOnlyList<BacktestRiskAuditState> RiskEvents)> GetAsync(Guid runId, CancellationToken cancellationToken) => Task.FromResult<(IReadOnlyList<BacktestTradeAuditState>, IReadOnlyList<BacktestRiskAuditState>)>(([], []));
    }
}
