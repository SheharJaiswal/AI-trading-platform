using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class HistoricalDataTests
{
    private static HistoricalCandle Candle(DateTimeOffset timestamp, decimal close = 101m) => new(new Symbol("NSE:TEST", "1"), "1d", timestamp, 100m, 102m, 99m, close, 1000, "fixture", timestamp.AddSeconds(1));

    [Fact]
    public void Valid_dataset_is_accepted()
    {
        var result = HistoricalDataValidator.Validate([
            Candle(DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
            Candle(DateTimeOffset.Parse("2026-01-02T00:00:00Z"), 103m)
        ]);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.CandleCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Duplicate_timestamp_is_rejected()
    {
        var timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var result = HistoricalDataValidator.Validate([Candle(timestamp), Candle(timestamp, 103m)]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("not strictly after", StringComparison.Ordinal));
    }

    [Fact]
    public void Invalid_ohlc_bounds_are_rejected()
    {
        var timestamp = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var invalid = new HistoricalCandle(new Symbol("NSE:TEST", "1"), "1d", timestamp, 100m, 98m, 99m, 101m, 1000, "fixture", timestamp.AddSeconds(1));

        var result = HistoricalDataValidator.Validate([invalid]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("invalid OHLC bounds", StringComparison.Ordinal));
    }

    [Fact]
    public void Mixed_symbol_dataset_is_rejected()
    {
        var first = Candle(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var second = Candle(DateTimeOffset.Parse("2026-01-02T00:00:00Z")) with { Symbol = new Symbol("NSE:OTHER", "2") };

        var result = HistoricalDataValidator.Validate([first, second]);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, x => x.Contains("changes symbol or interval", StringComparison.Ordinal));
    }
}