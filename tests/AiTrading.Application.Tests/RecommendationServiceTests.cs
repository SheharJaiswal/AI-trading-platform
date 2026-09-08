using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public class RecommendationServiceTests
{
    [Fact]
    public async Task Returns_NoDecision_When_Market_Data_Is_Stale()
    {
        var symbol = new Symbol("TCS", "11536");
        var quote = new MarketQuote(symbol, "NSE", "11536", DateTimeOffset.UtcNow.AddMinutes(-10), 100, 101, 99, 100, 100, 1000, "test");
        var service = new RecommendationService(new FakeMarketData(quote, []), new MarketDataFreshnessOptions(TimeSpan.FromMinutes(5)));

        var result = await service.GetRecommendationAsync(symbol, CancellationToken.None);

        Assert.Equal(RecommendationAction.NoDecision, result.Action);
        Assert.Contains("STALE_OR_INVALID_MARKET_DATA", result.RiskFactors);
    }

    [Fact]
    public async Task Returns_NoDecision_When_History_Is_Insufficient()
    {
        var symbol = new Symbol("TCS", "11536");
        var quote = new MarketQuote(symbol, "NSE", "11536", DateTimeOffset.UtcNow, 100, 101, 99, 100, 100, 1000, "test");
        var candles = Enumerable.Range(0, 10).Select(i => new Candle(DateTimeOffset.UtcNow.AddDays(-i), 100, 101, 99, 100, 1000)).ToArray();
        var service = new RecommendationService(new FakeMarketData(quote, candles));

        var result = await service.GetRecommendationAsync(symbol, CancellationToken.None);

        Assert.Equal(RecommendationAction.NoDecision, result.Action);
        Assert.Contains("INSUFFICIENT_DATA", result.RiskFactors);
    }

    [Fact]
    public async Task Returns_Buy_When_Two_Bullish_Signals_Are_Present()
    {
        var symbol = new Symbol("TCS", "11536");
        var quote = new MarketQuote(symbol, "NSE", "11536", DateTimeOffset.UtcNow, 100, 101, 99, 110, 110, 1000, "test");
        var candles = Enumerable.Range(0, 19)
            .Select(i => new Candle(DateTimeOffset.UtcNow.AddDays(-20 + i), 100, 101, 99, 100, 1000))
            .Append(new Candle(DateTimeOffset.UtcNow.AddDays(-1), 100, 101, 90, 100, 1000))
            .ToArray();
        var service = new RecommendationService(new FakeMarketData(quote, candles));

        var result = await service.GetRecommendationAsync(symbol, CancellationToken.None);

        Assert.Equal(RecommendationAction.Buy, result.Action);
        Assert.Contains("PRICE_ABOVE_SMA20", result.SupportingSignals);
        Assert.Contains("BULLISH_DOJI", result.SupportingSignals);
        Assert.Contains("BULLISH_HAMMER", result.SupportingSignals);
    }

    [Fact]
    public async Task Returns_Hold_When_Buy_Threshold_Is_Not_Met()
    {
        var symbol = new Symbol("TCS", "11536");
        var quote = new MarketQuote(symbol, "NSE", "11536", DateTimeOffset.UtcNow, 100, 101, 99, 90, 90, 1000, "test");
        var candles = Enumerable.Range(0, 20)
            .Select(i => new Candle(DateTimeOffset.UtcNow.AddDays(-20 + i), 100, 101, 99, 100, 1000))
            .ToArray();
        var service = new RecommendationService(new FakeMarketData(quote, candles));

        var result = await service.GetRecommendationAsync(symbol, CancellationToken.None);

        Assert.Equal(RecommendationAction.Hold, result.Action);
    }

    private sealed class FakeMarketData(MarketQuote quote, IReadOnlyList<Candle> candles) : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken) => Task.FromResult(quote);
        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) => Task.FromResult(candles);
    }
}
