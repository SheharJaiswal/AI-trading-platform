using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public class PaperTradingAndMonitoringTests
{
    [Fact]
    public async Task Approved_Buy_Is_Executed_And_Applied_To_Portfolio()
    {
        var symbol = new Symbol("TCS", "123");
        var quote = new MarketQuote(symbol, "NSE", "123", DateTimeOffset.UtcNow, 120, 121, 112, 119, 120, 1000, "test");
        var candles = Enumerable.Range(0, 19).Select(i => new Candle(DateTimeOffset.UtcNow.AddDays(-20 + i), 100 + i, 101 + i, 99 + i, 100 + i, 1000))
            .Append(new Candle(DateTimeOffset.UtcNow.AddDays(-1), 117, 120, 112, 119, 1000)).ToArray();
        var service = new PaperTradingService(new RecommendationService(new FakeMarketData(quote, candles)), new RiskEngine(), new FakeExecution(), new PaperPortfolio(10000m));

        var result = await service.ExecuteAsync(symbol, 1, CancellationToken.None);

        Assert.Equal(RiskDecision.Approved, result.Risk.Decision);
        Assert.NotNull(result.Fill);
    }

    [Fact]
    public async Task Stop_Loss_Alert_Is_Idempotent()
    {
        var symbol = new Symbol("TCS", "123");
        var portfolio = new PaperPortfolio(10000m);
        portfolio.Apply(new Fill(Guid.NewGuid(), symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "test"));
        var position = portfolio.Snapshot().Positions.Single();
        portfolio.SetStopLoss(position.Id, 95m);
        var alerts = new InMemoryAlertStore();
        var monitor = new RiskMonitor(new FakeMarketData(new MarketQuote(symbol, "NSE", "123", DateTimeOffset.UtcNow, 100, 101, 90, 99, 94, 1000, "test"), []), portfolio, alerts);

        await monitor.CheckOnceAsync(CancellationToken.None);
        await monitor.CheckOnceAsync(CancellationToken.None);

        Assert.Single(alerts.GetAll());
        Assert.Equal(AlertSeverity.High, alerts.GetAll()[0].Severity);
    }

    private sealed class FakeMarketData(MarketQuote quote, IReadOnlyList<Candle> candles) : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken) => Task.FromResult(quote);
        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) => Task.FromResult(candles);
    }

    private sealed class FakeExecution : IPaperExecutionProvider
    {
        public Task<Fill> ExecuteAsync(PaperOrder order, CancellationToken cancellationToken) => Task.FromResult(new Fill(order.Id, order.Symbol, order.Side, order.Quantity, order.LimitPrice, DateTimeOffset.UtcNow, "paper-test"));
    }
}
