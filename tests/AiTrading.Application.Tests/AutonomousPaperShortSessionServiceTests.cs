using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class AutonomousPaperShortSessionServiceTests
{
    [Fact]
    public async Task RunAsync_rejects_non_running_session_before_market_data_or_execution()
    {
        var symbol = new Symbol("TEST", "123");
        var sessions = new FakeSessions(new PaperTradingSessionState(Guid.NewGuid(), new([symbol], "1m", "baseline-v1", 1_000_000m), PaperTradingSessionStatus.Paused, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        var market = new CountingMarketDataProvider();
        var service = new AutonomousPaperShortSessionService(sessions, new RecommendationService(market), market, null!, null!);

        var request = new AutonomousPaperShortSessionRequest(1, .02m, .05m, 300);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RunAsync(sessions.Session.Id, Guid.NewGuid(), request, CancellationToken.None));
        Assert.Equal(0, market.QuoteCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task RunAsync_rejects_invalid_quantity(int quantity)
    {
        var sessions = new FakeSessions(null);
        var market = new CountingMarketDataProvider();
        var service = new AutonomousPaperShortSessionService(sessions, new RecommendationService(market), market, null!, null!);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.RunAsync(Guid.NewGuid(), Guid.NewGuid(), new(quantity, .02m, .05m, 300), CancellationToken.None));
    }

    private sealed class FakeSessions(PaperTradingSessionState? session) : IPaperTradingSessionService
    {
        public PaperTradingSessionState Session { get; } = session ?? new(Guid.NewGuid(), new([new Symbol("TEST", "123")], "1m", "baseline-v1", 1_000_000m), PaperTradingSessionStatus.Running, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        public Task<PaperTradingSessionState> CreateAsync(PaperTradingSessionConfiguration configuration, CancellationToken ct) => throw new NotSupportedException();
        public Task<PaperTradingSessionState?> GetAsync(Guid id, CancellationToken ct) => Task.FromResult<PaperTradingSessionState?>(id == Session.Id ? Session : null);
        public Task<PaperTradingSessionState?> TransitionAsync(Guid id, PaperTradingSessionStatus target, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class CountingMarketDataProvider : IMarketDataProvider
    {
        public int QuoteCalls { get; private set; }
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken)
        {
            QuoteCalls++;
            return Task.FromResult(new MarketQuote(symbol, "NSE", symbol.InstrumentToken!, DateTimeOffset.UtcNow, 100m, 101m, 99m, 100m, 100m, 1000, "test"));
        }
        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Candle>>([]);
    }
}
