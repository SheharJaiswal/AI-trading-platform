using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class PaperShortCyclePlannerTests
{
    private static Recommendation SellRecommendation => new(
        RecommendationAction.Sell,
        0.9m,
        -0.04m,
        ["PRICE_BELOW_SMA20", "BEARISH_CANDLE"]);

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
}
