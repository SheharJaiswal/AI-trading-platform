using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class PaperTradingSessionExecutionConcurrencyTests
{
    [Fact]
    public async Task Concurrent_Identical_Events_Preserve_Deterministic_Paper_Idempotency()
    {
        var session = new PaperTradingSessionState(
            Guid.NewGuid(),
            new PaperTradingSessionConfiguration([new Symbol("TCS", "123")], "1m", "baseline-v1", 10_000m),
            PaperTradingSessionStatus.Running,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        var paperTrades = new BlockingIdempotentPaperTradeService();
        var service = new PaperTradingSessionExecutionService(new StubSessionService(session), paperTrades);
        var request = new PaperTradingEventRequest(session.Id, session.Configuration.Symbols[0], 1, "concurrent-event");

        var first = Task.Run(() => service.ProcessAsync(request, CancellationToken.None));
        await paperTrades.FirstExecutionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var second = service.ProcessAsync(request, CancellationToken.None);
        paperTrades.ReleaseFirstExecution();

        var results = await Task.WhenAll(first, second);

        Assert.Equal(2, paperTrades.CallCount);
        Assert.Equal(results[0].Risk.Decision, results[1].Risk.Decision);
        Assert.Equal(results[0].Fill, results[1].Fill);
        Assert.Equal("PAPER_ONLY", results[0].ExecutionMode);
        Assert.Equal("PAPER_ONLY", results[1].ExecutionMode);
        Assert.Equal(paperTrades.DeterministicOrderId, results[0].Fill!.OrderId);
        Assert.Equal(results[0].Fill.OrderId, results[1].Fill.OrderId);
    }

    private sealed class StubSessionService(PaperTradingSessionState session) : IPaperTradingSessionService
    {
        public Task<PaperTradingSessionState> CreateAsync(PaperTradingSessionConfiguration configuration, CancellationToken ct) => Task.FromResult(session);
        public Task<PaperTradingSessionState?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<PaperTradingSessionState?>(id == session.Id ? session : null);
        public Task<PaperTradingSessionState?> TransitionAsync(Guid id, PaperTradingSessionStatus target, CancellationToken ct) => Task.FromResult<PaperTradingSessionState?>(session with { Status = target });
    }

    private sealed class BlockingIdempotentPaperTradeService : IPaperTradeService
    {
        public TaskCompletionSource<bool> FirstExecutionStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Guid DeterministicOrderId { get; private set; }
        public int CallCount { get; private set; }
        private readonly TaskCompletionSource<bool> release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly FillState fill = new(Guid.Empty, Guid.Empty, new Symbol("TCS", "123"), OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "paper-test");

        public async Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
        {
            CallCount++;
            DeterministicOrderId = orderId;
            var resolved = fill with { OrderId = orderId, Symbol = symbol, Quantity = quantity };
            if (CallCount == 1)
            {
                FirstExecutionStarted.TrySetResult(true);
                await release.Task.WaitAsync(cancellationToken);
            }
            return (new RiskResult(RiskDecision.Approved, null), resolved);
        }

        public void ReleaseFirstExecution() => release.TrySetResult(true);
    }
}
