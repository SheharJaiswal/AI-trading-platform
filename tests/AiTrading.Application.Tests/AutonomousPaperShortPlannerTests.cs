using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class AutonomousPaperShortPlannerTests
{
    [Fact]
    public void Plan_RejectsNonSellRecommendation()
    {
        var recommendation = new Recommendation(new Symbol("DEMO"), RecommendationAction.Buy, 100m, null, .6m, 1, [], [], DateTimeOffset.UtcNow, "baseline-v1");

        var result = AutonomousPaperShortPlanner.Plan(Guid.NewGuid(), new Symbol("DEMO"), recommendation, 1, 100m, 99m, 105m, 95m);

        Assert.Null(result);
    }

    [Fact]
    public void Plan_RejectsInvalidShortRiskWithoutCreatingExecutionIdentity()
    {
        var recommendation = new Recommendation(new Symbol("DEMO"), RecommendationAction.Sell, 100m, null, .6m, 1, ["PRICE_BELOW_SMA20"], [], DateTimeOffset.UtcNow, "baseline-v1");

        var result = AutonomousPaperShortPlanner.Plan(Guid.NewGuid(), new Symbol("DEMO"), recommendation, 1, 100m, 99m, 95m, 105m);

        Assert.NotNull(result);
        Assert.False(result.Cycle.Risk.Approved);
        Assert.Contains("SHORT_STOP_MUST_BE_ABOVE_ENTRY", result.Cycle.Risk.Reason);
        Assert.Equal(default, result.ExecutionRequest);
    }

    [Fact]
    public void Plan_CreatesDeterministicPaperOnlyShortRequest()
    {
        var sessionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var generatedAt = DateTimeOffset.Parse("2026-09-14T00:00:00+00:00");
        var recommendation = new Recommendation(new Symbol("DEMO", "123"), RecommendationAction.Sell, 100m, null, .6m, 1, ["PRICE_BELOW_SMA20", "RSI_NOT_OVERSOLD"], [], generatedAt, "baseline-v1");

        var first = AutonomousPaperShortPlanner.Plan(sessionId, recommendation.Symbol, recommendation, 5, 100m, 99m, 105m, 94m);
        var second = AutonomousPaperShortPlanner.Plan(sessionId, recommendation.Symbol, recommendation, 5, 100m, 99m, 105m, 94m);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.True(first.Cycle.Risk.Approved);
        Assert.Equal(OrderSide.Sell, first.ExecutionRequest.OrderId == Guid.Empty ? OrderSide.Buy : OrderSide.Sell);
        Assert.Equal(first.ExecutionRequest.OrderId, second.ExecutionRequest.OrderId);
        Assert.Equal(first.ExecutionRequest.IdempotencyKey, second.ExecutionRequest.IdempotencyKey);
        Assert.Equal("baseline-v1", first.ExecutionRequest.StrategyVersion);
        Assert.Equal(100m, first.ExecutionRequest.EntryPrice);
        Assert.Equal(105m, first.ExecutionRequest.StopLoss);
        Assert.Equal(94m, first.ExecutionRequest.TargetPrice);
    }
}
