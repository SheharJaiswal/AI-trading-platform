using AiTrading.Domain;

namespace AiTrading.Domain.Tests;

public class TechnicalAnalysisTests
{
    [Fact]
    public void Sma_Uses_Last_Period()
    {
        var result = TechnicalAnalysis.Sma([1m, 2m, 3m, 4m, 5m], 3);
        Assert.Equal(4m, result);
    }

    [Fact]
    public void Rsi_Is_One_Hundred_When_No_Losses()
    {
        var result = TechnicalAnalysis.Rsi([1m, 2m, 3m, 4m], 2);
        Assert.Equal(100m, result);
    }

    [Fact]
    public void Doji_Is_Detected()
    {
        var candles = new[] { new Candle(DateTimeOffset.UtcNow, 100m, 110m, 90m, 100.5m, 1000) };
        var result = CandlestickAnalysis.Detect(candles);
        Assert.Contains(result, x => x.Name == "Doji");
    }
}
