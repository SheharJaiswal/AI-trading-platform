using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class PaperShortCyclePlannerTests
{
    private static Recommendation SellRecommendation => new(
        new Symbol("TEST"),
        RecommendationAction.Sell,
        100m,
        -0.04m,
        0.9m,
        1,
        ["PRICE_BELOW_SMA20", "BEARISH_CANDLE"],
        [],
        DateTimeOffset.UtcNow,
        "deterministic-v16");

    [Fact]
    public void Plan_NonSellRecommendationDoesNotCreateShort()
    {
        var recommendation = SellRecommendation with { Action = RecommendationAction.Hold };
        var result = PaperShortCyclePlanner.Plan(new Symbol("TEST"), recommendation, 10, 100m, 95m, 105m, 90m);
        Assert.Null(result);
    }

    [Fact]
    public void Plan_ValidSellBuildsOpenShortAtCurrentPrice()
    {
        var result = PaperShortCyclePlanner.Plan(new Symbol("TEST"), SellRecommendation, 10, 100m, 97m, 105m, 90m);
        Assert.NotNull(result);
        Assert.True(result!.Risk.Approved);
        Assert.Equal(PaperShortLifecycleStatus.ShortOpen, result.Position.Status);
        Assert.Equal(30m, result.Position.UnrealizedPnl);
    }

    [Fact]
    public void Plan_RisingPriceStopsShort()
    {
        var result = PaperShortCyclePlanner.Plan(new Symbol("TEST"), SellRecommendation, 10, 100m, 106m, 105m, 90m);
        Assert.NotNull(result);
        Assert.Equal(PaperShortLifecycleStatus.ShortStopped, result!.Position.Status);
    }

    [Fact]
    public void Plan_FallingPriceHitsTarget()
    {
        var result = PaperShortCyclePlanner.Plan(new Symbol("TEST"), SellRecommendation, 10, 100m, 89m, 105m, 90m);
        Assert.NotNull(result);
        Assert.Equal(PaperShortLifecycleStatus.ShortTargetHit, result!.Position.Status);
    }

    [Fact]
    public void Plan_InvalidShortRiskProducesNoTrade()
    {
        var result = PaperShortCyclePlanner.Plan(new Symbol("TEST"), SellRecommendation, 10, 100m, 97m, 95m, 90m);
        Assert.NotNull(result);
        Assert.False(result!.Risk.Approved);
        Assert.Equal(PaperShortLifecycleStatus.NoTrade, result.Position.Status);
    }

    [Fact]
    public void Plan_FreshnessOverloadRejectsStaleMarketData()
    {
        var evaluation = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var result = PaperShortCyclePlanner.Plan(
            new Symbol("TEST"), SellRecommendation, 10, 100m, 97m, 105m, 90m,
            evaluation.AddMinutes(-2), evaluation, TimeSpan.FromMinutes(1));

        Assert.NotNull(result);
        Assert.False(result!.Risk.Approved);
        Assert.Equal("STALE_MARKET_DATA", result.Risk.Reason);
        Assert.Equal(PaperShortLifecycleStatus.NoTrade, result.Position.Status);
    }

    [Fact]
    public void Plan_FreshnessOverloadRejectsFutureMarketData()
    {
        var evaluation = new DateTimeOffset(2026, 9, 12, 0, 0, 0, TimeSpan.Zero);
        var result = PaperShortCyclePlanner.Plan(
            new Symbol("TEST"), SellRecommendation, 10, 100m, 97m, 105m, 90m,
            evaluation.AddSeconds(1), evaluation, TimeSpan.FromMinutes(1));

        Assert.NotNull(result);
        Assert.False(result!.Risk.Approved);
        Assert.Equal("FUTURE_MARKET_DATA", result.Risk.Reason);
        Assert.Equal(PaperShortLifecycleStatus.NoTrade, result.Position.Status);
    }
}
