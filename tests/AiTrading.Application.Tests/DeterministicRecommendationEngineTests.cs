using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DeterministicRecommendationEngineTests
{
    [Fact]
    public void Evaluate_ReturnsSell_WhenBearishSignalsDominate()
    {
        var engine = new DeterministicRecommendationEngine();
        var start = DateTimeOffset.UtcNow.AddMinutes(-19);
        var closes = new[] { 100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m, 101m, 100m, 99m, 98m, 95m };
        var candles = closes.Select((close, index) =>
        {
            var timestamp = start.AddMinutes(index);
            if (index == closes.Length - 1)
                return new Candle(timestamp, 96m, 100m, 94m, close, 1_000);
            return new Candle(timestamp, close, close + 0.5m, close - 0.5m, close, 1_000);
        }).ToArray();

        var result = engine.Evaluate(new Symbol("DEMO"), candles, start.AddMinutes(19));

        Assert.Equal(RecommendationAction.Sell, result.Action);
        Assert.Contains("PRICE_BELOW_SMA20", result.SupportingSignals);
        Assert.Contains("RSI_NOT_OVERSOLD", result.SupportingSignals);
        Assert.Contains("BEARISH_SHOOTING_STAR", result.SupportingSignals);
    }
}
