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

    [Fact]
    public void Sell_Updates_Cash_And_Realized_Pnl()
    {
        var portfolio = new PaperPortfolio(1000m);
        var symbol = new Symbol("TCS");
        portfolio.Apply(new Fill(Guid.NewGuid(), symbol, OrderSide.Buy, 2, 100m, DateTimeOffset.UtcNow, "paper"));
        portfolio.Apply(new Fill(Guid.NewGuid(), symbol, OrderSide.Sell, 1, 125m, DateTimeOffset.UtcNow, "paper"));

        var snapshot = portfolio.Snapshot();

        Assert.Equal(925m, snapshot.Cash);
        Assert.Equal(25m, snapshot.RealizedPnl);
        Assert.Single(snapshot.Positions);
        Assert.Equal(1, snapshot.Positions[0].Quantity);
    }
}

public class PaperTradingRiskBoundaryTests
{
    [Fact]
    public async Task Blocked_Recommendation_Never_Reaches_Execution()
    {
        var symbol = new Symbol("TCS", "123");
        var quote = new MarketQuote(symbol, "NSE", "123", DateTimeOffset.UtcNow.AddMinutes(-10), 100, 101, 99, 100, 100, 1000, "test");
        var execution = new SpyExecution();
        var portfolio = new PaperPortfolio(10000m);
        var recommendations = new RecommendationService(new FakeMarketData(quote, []), new MarketDataFreshnessOptions(TimeSpan.FromMinutes(5)));
        var service = new PaperTradingService(recommendations, new RiskEngine(), execution, portfolio);

        var result = await service.ExecuteAsync(symbol, 1, CancellationToken.None);

        Assert.NotEqual(RiskDecision.Approved, result.Risk.Decision);
        Assert.Null(result.Fill);
        Assert.False(execution.Called);
    }

    private sealed class FakeMarketData(MarketQuote quote, IReadOnlyList<Candle> candles) : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken) => Task.FromResult(quote);
        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) => Task.FromResult(candles);
    }

    private sealed class SpyExecution : IPaperExecutionProvider
    {
        public bool Called { get; private set; }

        public Task<Fill> ExecuteAsync(PaperOrder order, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(new Fill(order.Id, order.Symbol, order.Side, order.Quantity, order.LimitPrice, DateTimeOffset.UtcNow, "paper-test"));
        }
    }
}
