using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class AutonomousPaperShortExecutionCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_RejectsNonSellWithoutCallingExecutionBoundary()
    {
        var coordinator = new AutonomousPaperShortExecutionCoordinator(null!, new AutonomousPaperShortExecutionOptions(.05m, .05m, TimeSpan.FromMinutes(5)));
        var recommendation = new Recommendation(new Symbol("DEMO"), RecommendationAction.Buy, 100m, null, .7m, 1, [], [], DateTimeOffset.UtcNow, "baseline-v1");
        var result = await coordinator.ExecuteAsync(Guid.NewGuid(), new Symbol("DEMO"), recommendation, Quote(new Symbol("DEMO"), DateTimeOffset.UtcNow), 1, CancellationToken.None);
        Assert.Equal(RiskDecision.RiskBlocked, result.Risk.Decision);
        Assert.Equal("SHORT_REQUIRES_BEARISH_RECOMMENDATION", result.Risk.Reason);
        Assert.Null(result.Fill);
        Assert.Equal("no-trade", result.Status);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsStaleQuoteBeforeExecution()
    {
        var coordinator = new AutonomousPaperShortExecutionCoordinator(null!, new AutonomousPaperShortExecutionOptions(.05m, .05m, TimeSpan.FromMinutes(1)));
        var recommendation = SellRecommendation();
        var result = await coordinator.ExecuteAsync(Guid.NewGuid(), recommendation.Symbol, recommendation, Quote(recommendation.Symbol, DateTimeOffset.UtcNow.AddMinutes(-2)), 1, CancellationToken.None);
        Assert.Equal(RiskDecision.RiskBlocked, result.Risk.Decision);
        Assert.Equal("STALE_OR_INVALID_MARKET_DATA", result.Risk.Reason);
        Assert.Null(result.Fill);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsMismatchedQuoteBeforeExecution()
    {
        var coordinator = new AutonomousPaperShortExecutionCoordinator(null!, new AutonomousPaperShortExecutionOptions(.05m, .05m, TimeSpan.FromMinutes(5)));
        var recommendation = SellRecommendation();
        var result = await coordinator.ExecuteAsync(Guid.NewGuid(), recommendation.Symbol, recommendation, Quote(new Symbol("OTHER"), DateTimeOffset.UtcNow), 1, CancellationToken.None);
        Assert.Equal(RiskDecision.RiskBlocked, result.Risk.Decision);
        Assert.Equal("INVALID_SHORT_MARKET_QUOTE", result.Risk.Reason);
        Assert.Null(result.Fill);
    }

    [Fact]
    public void Options_RejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutonomousPaperShortExecutionOptions(0m, .05m, TimeSpan.FromMinutes(5)).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutonomousPaperShortExecutionOptions(.05m, 1m, TimeSpan.FromMinutes(5)).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new AutonomousPaperShortExecutionOptions(.05m, .05m, TimeSpan.Zero).Validate());
    }

    private static Recommendation SellRecommendation() => new(new Symbol("DEMO", "123"), RecommendationAction.Sell, 100m, null, .7m, 1, ["PRICE_BELOW_SMA20"], [], DateTimeOffset.UtcNow, "baseline-v1");
    private static MarketQuote Quote(Symbol symbol, DateTimeOffset timestamp) => new(symbol, "NSE", symbol.InstrumentToken ?? "123", timestamp, 100m, 101m, 99m, 100m, 100m, 1000, "test");
}
