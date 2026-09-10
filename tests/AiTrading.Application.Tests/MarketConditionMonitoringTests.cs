using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class MarketConditionMonitoringTests
{
    [Fact]
    public void DetectsSuddenPriceMoveUsingOnlyPriorData()
    {
        var symbol = new Symbol("ABC");
        var timestamp = DateTimeOffset.UtcNow;
        var history = new[]
        {
            new Candle(timestamp.AddMinutes(-1), 100m, 101m, 99m, 100m, 1000)
        };
        var current = new MarketQuote(symbol, "NSE", "1", timestamp, 104m, 105m, 103m, 104m, 104m, 1000, "test");

        var events = new MarketConditionDetector(new(3m, 2m)).Evaluate(symbol, current, history);

        var item = Assert.Single(events);
        Assert.Equal("SUDDEN_PRICE_MOVE", item.Type);
        Assert.Equal(AlertSeverity.Warning, item.Severity);
    }

    [Fact]
    public void DetectsAbnormalVolumeAgainstRecentHistory()
    {
        var symbol = new Symbol("ABC");
        var timestamp = DateTimeOffset.UtcNow;
        var history = Enumerable.Range(1, 5)
            .Select(i => new Candle(timestamp.AddMinutes(-i), 100m, 101m, 99m, 100m, 1000))
            .ToArray();
        var current = new MarketQuote(symbol, "NSE", "1", timestamp, 100m, 101m, 99m, 100m, 100m, 2500, "test");

        var events = new MarketConditionDetector(new(3m, 2m)).Evaluate(symbol, current, history);

        var item = Assert.Single(events);
        Assert.Equal("ABNORMAL_VOLUME", item.Type);
        Assert.Equal(AlertSeverity.Warning, item.Severity);
    }

    [Fact]
    public void DoesNotUseFutureCandleAsReference()
    {
        var symbol = new Symbol("ABC");
        var timestamp = DateTimeOffset.UtcNow;
        var history = new[]
        {
            new Candle(timestamp.AddMinutes(1), 100m, 101m, 99m, 100m, 1000)
        };
        var current = new MarketQuote(symbol, "NSE", "1", timestamp, 110m, 111m, 109m, 110m, 110m, 1000, "test");

        var events = new MarketConditionDetector(new(3m, 2m)).Evaluate(symbol, current, history);

        Assert.Empty(events);
    }

    [Fact]
    public void RejectsNonPositiveConfiguration()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarketConditionMonitoringOptions(0m, 2m).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new MarketConditionMonitoringOptions(3m, 0m).Validate());
    }
}
