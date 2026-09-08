using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class HistoricalDatasetCsvParserTests
{
    private const string Header = "symbol,instrumentToken,interval,timestamp,open,high,low,close,volume,source,receivedAt";

    [Fact]
    public void Parse_valid_dataset_preserves_order_and_provenance()
    {
        var csv = $"{Header}\nINFY,123,1d,2026-01-01T00:00:00+00:00,100,105,99,104,1000,local,2026-01-02T00:00:00+00:00\nINFY,123,1d,2026-01-02T00:00:00+00:00,104,108,103,107,1200,local,2026-01-03T00:00:00+00:00";

        var result = HistoricalDatasetCsvParser.Parse(csv);

        Assert.True(result.IsValid);
        Assert.Equal(2, result.Candles.Count);
        Assert.Equal("local", result.Candles[0].Source);
        Assert.Equal("123", result.Candles[0].Symbol.InstrumentToken);
        Assert.True(result.Candles[0].Timestamp < result.Candles[1].Timestamp);
    }

    [Fact]
    public void Parse_rejects_bad_header()
    {
        var result = HistoricalDatasetCsvParser.Parse("symbol,timestamp\nINFY,2026-01-01T00:00:00+00:00");

        Assert.False(result.IsValid);
        Assert.Contains("CSV header", result.Errors[0]);
    }

    [Fact]
    public void Parse_rejects_duplicate_or_out_of_order_timestamp()
    {
        var csv = $"{Header}\nINFY,123,1d,2026-01-02T00:00:00+00:00,100,105,99,104,1000,local,2026-01-03T00:00:00+00:00\nINFY,123,1d,2026-01-01T00:00:00+00:00,104,108,103,107,1200,local,2026-01-03T00:00:00+00:00";

        var result = HistoricalDatasetCsvParser.Parse(csv);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("strictly after", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_rejects_invalid_numbers()
    {
        var csv = $"{Header}\nINFY,123,1d,2026-01-01T00:00:00+00:00,abc,105,99,104,1000,local,2026-01-02T00:00:00+00:00";

        var result = HistoricalDatasetCsvParser.Parse(csv);

        Assert.False(result.IsValid);
        Assert.Contains("invalid numeric", result.Errors[0]);
    }

    [Fact]
    public void Parse_empty_dataset_is_invalid()
    {
        var result = HistoricalDatasetCsvParser.Parse(Header);

        Assert.False(result.IsValid);
        Assert.Contains("At least one historical candle", result.Errors[0]);
    }
}
