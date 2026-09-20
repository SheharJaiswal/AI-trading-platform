using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class PaperTradingSessionExecutionTests
{
    [Fact]
    public async Task Event_Requires_Running_Session()
    {
        var session = CreateSession(PaperTradingSessionStatus.Paused);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(new StubSessionService(session), paperTrades);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, "evt-1"), CancellationToken.None));
        Assert.Null(paperTrades.LastRequest);
    }

    [Fact]
    public async Task Event_Rejects_Symbol_Outside_Configured_Universe()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(new StubSessionService(session), paperTrades);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, new Symbol("RELIANCE", "789"), 1, "evt-1"), CancellationToken.None));
        Assert.Null(paperTrades.LastRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("x")]
    public async Task Event_Rejects_Invalid_Event_Id(string eventId)
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(new StubSessionService(session), paperTrades);

        if (eventId == "x")
        {
            var request = new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, new string('x', 129));
            await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessAsync(request, CancellationToken.None));
        }
        else
        {
            await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessAsync(
                new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, eventId), CancellationToken.None));
        }
        Assert.Null(paperTrades.LastRequest);
    }

    [Fact]
    public async Task Event_Rejects_Nonpositive_Quantity()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(new StubSessionService(session), paperTrades);

        await Assert.ThrowsAsync<ArgumentException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 0, "evt-qty"), CancellationToken.None));
        Assert.Null(paperTrades.LastRequest);
    }

    [Fact]
    public async Task Risk_Blocked_Event_Returns_No_Fill_And_Still_Remains_Paper_Only()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var paperTrades = new CapturingPaperTradeService(new RiskResult(RiskDecision.RiskBlocked, "risk-limit"), null);
        var service = new PaperTradingSessionExecutionService(new StubSessionService(session), paperTrades);

        var response = await service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, "evt-risk"), CancellationToken.None);

        Assert.Equal(RiskDecision.RiskBlocked, response.Risk.Decision);
        Assert.Equal("risk-limit", response.Risk.Reason);
        Assert.Null(response.Fill);
        Assert.Equal("PAPER_ONLY", response.ExecutionMode);
        Assert.NotNull(paperTrades.LastRequest);
    }

    [Fact]
    public async Task Same_Event_Produces_Deterministic_Order_And_Idempotency_Identity()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(new StubSessionService(session), paperTrades);
        var request = new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 2, "evt-42");

        var first = await service.ProcessAsync(request, CancellationToken.None);
        var firstCapture = paperTrades.LastRequest!.Value;
        var second = await service.ProcessAsync(request, CancellationToken.None);
        var secondCapture = paperTrades.LastRequest!.Value;

        Assert.Equal("PAPER_ONLY", first.ExecutionMode);
        Assert.Equal(firstCapture.OrderId, secondCapture.OrderId);
        Assert.Equal(firstCapture.IdempotencyKey, secondCapture.IdempotencyKey);
        Assert.Equal(first.EventId, second.EventId);
        Assert.Equal(RiskDecision.Approved, first.Risk.Decision);
    }

    [Fact]
    public async Task Persisted_Event_Replay_Does_Not_Execute_Paper_Order_Again_And_Returns_Persisted_Fill()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var paperTrades = new CapturingPaperTradeService();
        var audit = new InMemoryEventAuditRepository();
        var service = new PaperTradingSessionExecutionService(
            new StubSessionService(session),
            paperTrades,
            new FakeUnitOfWorkFactory(audit));
        var request = new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, "evt-replay");

        var first = await service.ProcessAsync(request, CancellationToken.None);
        var callsAfterFirst = paperTrades.CallCount;
        var replay = await service.ProcessAsync(request, CancellationToken.None);

        Assert.Equal(1, callsAfterFirst);
        Assert.Equal(1, paperTrades.CallCount);
        Assert.Equal(first.Risk.Decision, replay.Risk.Decision);
        Assert.Equal(first.Risk.Reason, replay.Risk.Reason);
        Assert.NotNull(replay.Fill);
        Assert.Equal(first.Fill!.Quantity, replay.Fill.Quantity);
        Assert.Equal(first.Fill!.FillPrice, replay.Fill.FillPrice);
        Assert.Equal("PAPER_ONLY", replay.ExecutionMode);
    }

    [Fact]
    public async Task Persisted_Replay_With_Mismatched_Symbol_Fails_Closed_Without_Reexecution()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var audit = new InMemoryEventAuditRepository();
        await audit.AddAsync(new PaperTradingEventAuditState(
            Guid.NewGuid(), session.Id, "evt-mismatch-symbol", Guid.NewGuid(), session.Configuration.Symbols[0], 1,
            RiskDecision.Approved.ToString(), "approved", 100m, DateTimeOffset.UtcNow), CancellationToken.None);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(
            new StubSessionService(session), paperTrades, new FakeUnitOfWorkFactory(audit));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, new Symbol("INFY", "456"), 1, "evt-mismatch-symbol"), CancellationToken.None));

        Assert.Equal("Persisted paper event audit does not match the replay request.", error.Message);
        Assert.Equal(0, paperTrades.CallCount);
    }

    [Fact]
    public async Task Persisted_Replay_With_Mismatched_Quantity_Fails_Closed_Without_Reexecution()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var audit = new InMemoryEventAuditRepository();
        await audit.AddAsync(new PaperTradingEventAuditState(
            Guid.NewGuid(), session.Id, "evt-mismatch-quantity", Guid.NewGuid(), session.Configuration.Symbols[0], 1,
            RiskDecision.Approved.ToString(), "approved", 100m, DateTimeOffset.UtcNow), CancellationToken.None);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(
            new StubSessionService(session), paperTrades, new FakeUnitOfWorkFactory(audit));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 2, "evt-mismatch-quantity"), CancellationToken.None));

        Assert.Equal("Persisted paper event audit does not match the replay request.", error.Message);
        Assert.Equal(0, paperTrades.CallCount);
    }

    [Theory]
    [InlineData("symbol")]
    [InlineData("quantity")]
    [InlineData("order")]
    [InlineData("price")]
    public async Task Persisted_Replay_With_Inconsistent_Fill_Fails_Closed(string mismatch)
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var orderId = Guid.NewGuid();
        var audit = new InMemoryEventAuditRepository();
        await audit.AddAsync(new PaperTradingEventAuditState(
            Guid.NewGuid(), session.Id, "evt-corrupt-fill", orderId, session.Configuration.Symbols[0], 2,
            RiskDecision.Approved.ToString(), "approved", 100m, DateTimeOffset.UtcNow), CancellationToken.None);
        var persistedFill = new FillState(
            Guid.NewGuid(),
            mismatch == "order" ? Guid.NewGuid() : orderId,
            mismatch == "symbol" ? new Symbol("INFY", "456") : session.Configuration.Symbols[0],
            OrderSide.Buy,
            mismatch == "quantity" ? 1 : 2,
            mismatch == "price" ? 101m : 100m,
            DateTimeOffset.UtcNow,
            "paper-test");
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(
            new StubSessionService(session), paperTrades, new FakeUnitOfWorkFactory(audit, persistedFill));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 2, "evt-corrupt-fill"), CancellationToken.None));

        Assert.Equal("Persisted paper event audit fill does not match the persisted audit state.", error.Message);
        Assert.Equal(0, paperTrades.CallCount);
    }

    [Fact]
    public async Task Persisted_Replay_With_NonPositive_Fill_Price_Fails_Closed()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var orderId = Guid.NewGuid();
        var audit = new InMemoryEventAuditRepository();
        await audit.AddAsync(new PaperTradingEventAuditState(
            Guid.NewGuid(), session.Id, "evt-invalid-price", orderId, session.Configuration.Symbols[0], 1,
            RiskDecision.Approved.ToString(), "approved", 0m, DateTimeOffset.UtcNow), CancellationToken.None);
        var persistedFill = new FillState(
            orderId, orderId, session.Configuration.Symbols[0], OrderSide.Buy, 1, 0m,
            DateTimeOffset.UtcNow, "paper-test");
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(
            new StubSessionService(session), paperTrades, new FakeUnitOfWorkFactory(audit, persistedFill));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, "evt-invalid-price"), CancellationToken.None));

        Assert.Equal("Persisted paper event audit fill has an invalid price; execution state requires reconciliation.", error.Message);
        Assert.Equal(0, paperTrades.CallCount);
    }

    [Fact]
    public async Task Persisted_Approved_Event_Without_Fill_Fails_Closed()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var audit = new InMemoryEventAuditRepository();
        await audit.AddAsync(new PaperTradingEventAuditState(
            Guid.NewGuid(), session.Id, "evt-corrupt-approved", Guid.NewGuid(), session.Configuration.Symbols[0], 1,
            RiskDecision.Approved.ToString(), "approved", 100m, DateTimeOffset.UtcNow), CancellationToken.None);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(
            new StubSessionService(session),
            paperTrades,
            new FakeUnitOfWorkFactory(audit, returnFill: false));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, "evt-corrupt-approved"), CancellationToken.None));

        Assert.Equal("Persisted paper event audit is approved but its paper fill is missing.", error.Message);
        Assert.Equal(0, paperTrades.CallCount);
    }

    [Fact]
    public async Task Persisted_Blocked_Event_With_Fill_Fails_Closed()
    {
        var session = CreateSession(PaperTradingSessionStatus.Running);
        var audit = new InMemoryEventAuditRepository();
        await audit.AddAsync(new PaperTradingEventAuditState(
            Guid.NewGuid(), session.Id, "evt-corrupt-blocked", Guid.NewGuid(), session.Configuration.Symbols[0], 1,
            RiskDecision.RiskBlocked.ToString(), "risk-limit", null, DateTimeOffset.UtcNow), CancellationToken.None);
        var paperTrades = new CapturingPaperTradeService();
        var service = new PaperTradingSessionExecutionService(
            new StubSessionService(session),
            paperTrades,
            new FakeUnitOfWorkFactory(audit, returnFill: true));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, "evt-corrupt-blocked"), CancellationToken.None));

        Assert.Equal("Persisted paper event audit is blocked but has a paper fill.", error.Message);
        Assert.Equal(0, paperTrades.CallCount);
    }

    private static PaperTradingSessionState CreateSession(PaperTradingSessionStatus status) =>
        new(
            Guid.NewGuid(),
            new PaperTradingSessionConfiguration([new Symbol("TCS", "123"), new Symbol("INFY", "456")], "1m", "baseline-v1", 10_000m),
            status,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

    private sealed class StubSessionService(PaperTradingSessionState session) : IPaperTradingSessionService
    {
        public Task<PaperTradingSessionState> CreateAsync(PaperTradingSessionConfiguration configuration, CancellationToken ct) => Task.FromResult(session);
        public Task<PaperTradingSessionState?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<PaperTradingSessionState?>(id == session.Id ? session : null);
        public Task<PaperTradingSessionState?> TransitionAsync(Guid id, PaperTradingSessionStatus target, CancellationToken ct) => Task.FromResult<PaperTradingSessionState?>(session with { Status = target });
    }

    private sealed class CapturingPaperTradeService : IPaperTradeService
    {
        private readonly RiskResult risk;
        private readonly FillState? fill;
        public (Guid OrderId, string IdempotencyKey)? LastRequest { get; private set; }
        public int CallCount { get; private set; }

        public CapturingPaperTradeService() : this(new RiskResult(RiskDecision.Approved, null), null) { }

        public CapturingPaperTradeService(RiskResult risk, FillState? fill)
        {
            this.risk = risk;
            this.fill = fill ?? (risk.Decision == RiskDecision.Approved ? new FillState(Guid.NewGuid(), Guid.Empty, new Symbol("TCS", "123"), OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper-test") : null);
        }

        public Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
        {
            CallCount++;
            LastRequest = (orderId, idempotencyKey);
            var resolvedFill = fill is null ? null : fill with { OrderId = orderId, Symbol = symbol, Quantity = quantity };
            return Task.FromResult<(RiskResult Risk, FillState? Fill)>((risk, resolvedFill));
        }
    }

    private sealed class InMemoryEventAuditRepository : IPaperTradingEventAuditRepository
    {
        private readonly List<PaperTradingEventAuditState> audits = [];
        public Task AddAsync(PaperTradingEventAuditState audit, CancellationToken cancellationToken)
        {
            audits.Add(audit);
            return Task.CompletedTask;
        }
        public Task<PaperTradingEventAuditState?> GetBySessionAndEventAsync(Guid sessionId, string eventId, CancellationToken cancellationToken) =>
            Task.FromResult(audits.FirstOrDefault(x => x.SessionId == sessionId && x.EventId == eventId));
        public Task<IReadOnlyList<PaperTradingEventAuditState>> GetBySessionAsync(Guid sessionId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<PaperTradingEventAuditState>>(audits.Where(x => x.SessionId == sessionId).ToArray());
    }

    private sealed class FakeUnitOfWorkFactory(InMemoryEventAuditRepository audit, FillState? persistedFill = null, bool returnFill = true) : ITradingUnitOfWorkFactory
    {
        public FakeUnitOfWorkFactory(InMemoryEventAuditRepository audit, bool returnFill) : this(audit, null, returnFill) { }
        public Task<ITradingUnitOfWork> CreateAsync(CancellationToken cancellationToken) => Task.FromResult<ITradingUnitOfWork>(new FakeUnitOfWork(audit, persistedFill, returnFill));
    }

    private sealed class FakeUnitOfWork(InMemoryEventAuditRepository audit, FillState? persistedFill, bool returnFill) : ITradingUnitOfWork
    {
        public IPortfolioRepository Portfolios => throw new NotSupportedException();
        public IOrderRepository Orders => new ReplayOrderRepository(persistedFill, returnFill);
        public IDurableShortPositionRepository DurableShortPositions => throw new NotSupportedException();
        public IPaperTradingEventAuditRepository PaperTradingEventAudits => audit;
        public IAlertRepository Alerts => throw new NotSupportedException();
        public IMarketDataSnapshotRepository MarketDataSnapshots => throw new NotSupportedException();
        public IHistoricalCandleRepository HistoricalCandles => throw new NotSupportedException();
        public IBacktestRunRepository BacktestRuns => throw new NotSupportedException();
        public IBacktestAuditRepository BacktestAudit => throw new NotSupportedException();
        public ILiveOrderStateRepository LiveOrders => throw new NotSupportedException();
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class ReplayOrderRepository(FillState? persistedFill, bool returnFill) : IOrderRepository
    {
        public Task<OrderState?> GetAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult<OrderState?>(null);
        public Task<OrderState?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) => Task.FromResult<OrderState?>(null);
        public Task<IReadOnlyList<OrderState>> GetByIdempotencyPrefixAsync(string prefix, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OrderState>>([]);
        public Task<IReadOnlyList<FillState>> GetFillsByOrderIdsAsync(IReadOnlyList<Guid> orderIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<FillState>>([]);
        public Task AddAsync(OrderState order, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<FillState?> GetFillByOrderIdAsync(Guid orderId, CancellationToken cancellationToken) =>
            Task.FromResult<FillState?>(persistedFill ?? (returnFill ? new FillState(Guid.NewGuid(), orderId, new Symbol("TCS", "123"), OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper-test") : null));
        public Task AddFillAsync(FillState fill, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
