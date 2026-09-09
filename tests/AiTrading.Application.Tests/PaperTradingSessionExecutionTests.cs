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
            new PaperTradingEventRequest(session.Id, new Symbol("INFY", "456"), 1, "evt-1"), CancellationToken.None));
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
        var paperTrades = new CapturingPaperTradeService(new RiskResult(RiskDecision.Blocked, "risk-limit"), null);
        var service = new PaperTradingSessionExecutionService(new StubSessionService(session), paperTrades);

        var response = await service.ProcessAsync(
            new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, "evt-risk"), CancellationToken.None);

        Assert.Equal(RiskDecision.Blocked, response.Risk.Decision);
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

    private static PaperTradingSessionState CreateSession(PaperTradingSessionStatus status) =>
        new(
            Guid.NewGuid(),
            new PaperTradingSessionConfiguration([new Symbol("TCS", "123")], "1m", "baseline-v1", 10_000m),
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

        public CapturingPaperTradeService() : this(new RiskResult(RiskDecision.Approved, null), null) { }

        public CapturingPaperTradeService(RiskResult risk, FillState? fill)
        {
            this.risk = risk;
            this.fill = fill ?? (risk.Decision == RiskDecision.Approved ? new FillState(Guid.NewGuid(), Guid.Empty, new Symbol("TCS", "123"), OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper-test") : null);
        }

        public Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
        {
            LastRequest = (orderId, idempotencyKey);
            var resolvedFill = fill is null ? null : fill with { OrderId = orderId, Symbol = symbol, Quantity = quantity };
            return Task.FromResult<(RiskResult Risk, FillState? Fill)>((risk, resolvedFill));
        }
    }
}
