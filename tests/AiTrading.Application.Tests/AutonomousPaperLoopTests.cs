using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class AutonomousPaperLoopTests
{
    [Fact]
    public async Task RunCycle_DoesNotExecute_WhenThereIsNoActionableOpportunity()
    {
        var sessionRepository = new InMemoryPaperTradingSessionRepository();
        var sessions = new PaperTradingSessionService(sessionRepository);
        var symbol = new Symbol("DEMO");
        var session = await sessions.CreateAsync(new([symbol], "1m", "baseline-v1", 1_000m), CancellationToken.None);
        await sessions.TransitionAsync(session.Id, PaperTradingSessionStatus.Running, CancellationToken.None);

        var provider = new FakeMarketDataProvider();
        var recommendations = new RecommendationService(provider, new MarketDataFreshnessOptions(TimeSpan.FromMinutes(5)));
        var paperTrades = new RecordingPaperTradeService();
        var execution = new PaperTradingSessionExecutionService(sessions, paperTrades);
        var loop = new AutonomousPaperLoopService(sessions, recommendations, execution);

        var result = await loop.RunCycleAsync(session.Id, CancellationToken.None);

        Assert.Equal("PAPER_ONLY", result.ExecutionMode);
        Assert.Null(result.Execution);
        Assert.Equal("NO_ACTIONABLE_OPPORTUNITY", result.NoDecisionReason);
        Assert.Empty(paperTrades.Calls);
    }

    [Fact]
    public async Task RunCycle_RejectsPausedSession_BeforeMarketExecution()
    {
        var sessionRepository = new InMemoryPaperTradingSessionRepository();
        var sessions = new PaperTradingSessionService(sessionRepository);
        var session = await sessions.CreateAsync(new([new Symbol("DEMO")], "1m", "baseline-v1", 1_000m), CancellationToken.None);
        await sessions.TransitionAsync(session.Id, PaperTradingSessionStatus.Running, CancellationToken.None);
        await sessions.TransitionAsync(session.Id, PaperTradingSessionStatus.Paused, CancellationToken.None);

        var recommendations = new RecommendationService(new FakeMarketDataProvider());
        var paperTrades = new RecordingPaperTradeService();
        var execution = new PaperTradingSessionExecutionService(sessions, paperTrades);
        var loop = new AutonomousPaperLoopService(sessions, recommendations, execution);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => loop.RunCycleAsync(session.Id, CancellationToken.None));

        Assert.Contains("requires a running paper session", exception.Message);
        Assert.Empty(paperTrades.Calls);
    }

    private sealed class FakeMarketDataProvider : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken) =>
            Task.FromResult(new MarketQuote(symbol, "DEMO", "DEMO", DateTimeOffset.UtcNow, 100m, 100m, 100m, 100m, 100m, 0, "test"));

        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Candle>>([]);
    }

    private sealed class RecordingPaperTradeService : IPaperTradeService
    {
        public List<(Symbol Symbol, int Quantity)> Calls { get; } = [];
        public Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
        {
            Calls.Add((symbol, quantity));
            return Task.FromResult<(RiskResult, FillState?)>((new(RiskDecision.Approved, null), null));
        }
    }
}
