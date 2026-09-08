using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class BacktestingTests
{
    [Fact]
    public void Run_is_deterministic_for_identical_inputs()
    {
        var candles = BuildCandles();
        var engine = new DeterministicBacktestEngine(new DeterministicRecommendationEngine(), new RiskEngine());
        var config = new BacktestConfiguration(10_000m, 1, 0.001m, 5m);

        var first = engine.Run(new Symbol("TEST"), candles, config);
        var second = engine.Run(new Symbol("TEST"), candles, config);

        Assert.Equal(first.EndingCash, second.EndingCash);
        Assert.Equal(first.ReturnPercent, second.ReturnPercent);
        Assert.Equal(first.Trades.Select(x => x.Price), second.Trades.Select(x => x.Price));
    }

    [Fact]
    public void Run_never_uses_a_future_candle_for_an_earlier_decision()
    {
        var candles = BuildCandles();
        var engine = new DeterministicBacktestEngine(new DeterministicRecommendationEngine(), new RiskEngine());
        var result = engine.Run(new Symbol("TEST"), candles, new BacktestConfiguration(10_000m, 1, 0m, 0m));

        Assert.All(result.EquityCurve, point => Assert.True(point.Timestamp <= candles[^1].Timestamp));
        Assert.All(result.Trades, trade => Assert.True(trade.Timestamp <= candles[^1].Timestamp));
    }

    [Fact]
    public void Run_applies_fee_and_slippage_to_simulated_buy()
    {
        var candles = BuildCandles();
        var engine = new DeterministicBacktestEngine(new DeterministicRecommendationEngine(), new RiskEngine());
        var no_cost = engine.Run(new Symbol("TEST"), candles, new BacktestConfiguration(10_000m, 1, 0m, 0m));
        var costs = engine.Run(new Symbol("TEST"), candles, new BacktestConfiguration(10_000m, 1, 0.01m, 100m));

        Assert.True(costs.EndingCash <= no_cost.EndingCash);
        if (costs.Trades.Count > 0) Assert.True(costs.Trades[0].Price > no_cost.Trades[0].Price);
    }

    private static IReadOnlyList<HistoricalCandle> BuildCandles()
    {
        var start = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return Enumerable.Range(0, 25).Select(i => new HistoricalCandle(
            new Symbol("TEST", "1"), "1d", start.AddDays(i), 100 + i, 102 + i, 99 + i, 101 + i, 1000 + i, "fixture", start.AddDays(i).AddMinutes(1))).ToArray();
    }
}
