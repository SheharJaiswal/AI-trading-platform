using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public class RiskEngineTests
{
    [Fact]
    public void Rejects_Order_When_Cash_Is_Insufficient()
    {
        var engine = new RiskEngine();
        var recommendation = new Recommendation(new Symbol("TCS"), RecommendationAction.Buy, 1000m, null, .8m, 1, ["signal"], [], DateTimeOffset.UtcNow, "baseline-v1");
        var result = engine.Evaluate(recommendation, 500m, 1);
        Assert.Equal(RiskDecision.RiskBlocked, result.Decision);
    }

    [Fact]
    public void NoDecision_Cannot_Execute()
    {
        var engine = new RiskEngine();
        var recommendation = new Recommendation(new Symbol("TCS"), RecommendationAction.NoDecision, 1000m, null, 0m, 1, [], ["INSUFFICIENT_DATA"], DateTimeOffset.UtcNow, "baseline-v1");
        var result = engine.Evaluate(recommendation, 10000m, 1);
        Assert.Equal(RiskDecision.InsufficientData, result.Decision);
    }
}

public class PaperPortfolioTests
{
    [Fact]
    public void Buy_Reduces_Cash_And_Creates_Position()
    {
        var portfolio = new PaperPortfolio(1000m);
        var fill = new Fill(Guid.NewGuid(), new Symbol("TCS"), OrderSide.Buy, 2, 100m, DateTimeOffset.UtcNow, "paper");
        portfolio.Apply(fill);
        var snapshot = portfolio.Snapshot();
        Assert.Equal(800m, snapshot.Cash);
        Assert.Single(snapshot.Positions);
        Assert.Equal(2, snapshot.Positions[0].Quantity);
    }
}
