using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record BacktestConfiguration(decimal StartingCash, int QuantityPerTrade, decimal FeeRate, decimal SlippageBasisPoints, string StrategyVersion = "baseline-v1");
public sealed record BacktestTrade(DateTimeOffset Timestamp, OrderSide Side, int Quantity, decimal Price, decimal Fee, RiskDecision RiskDecision, string? RiskReason);
public sealed record BacktestRiskEvent(DateTimeOffset Timestamp, RiskDecision Decision, string? Reason, decimal AvailableCash, int RequestedQuantity);
public sealed record BacktestEquityPoint(DateTimeOffset Timestamp, decimal Cash, decimal PositionValue, decimal Equity);
public sealed record BacktestResult(decimal StartingCash, decimal EndingCash, decimal ReturnPercent, decimal MaxDrawdownPercent, IReadOnlyList<BacktestTrade> Trades, IReadOnlyList<BacktestEquityPoint> EquityCurve, IReadOnlyList<BacktestRiskEvent> RiskEvents)
{
    public bool SimulationOnly => true;
    public int WinCount => 0;
    public int LossCount => 0;
}

public sealed class DeterministicBacktestEngine(DeterministicRecommendationEngine recommendationEngine, RiskEngine riskEngine)
{
    public BacktestResult Run(Symbol symbol, IReadOnlyList<HistoricalCandle> historicalCandles, BacktestConfiguration configuration)
    {
        Validate(configuration);
        var ordered = historicalCandles.Where(x => x.Symbol == symbol).OrderBy(x => x.Timestamp).ToArray();
        if (ordered.Length == 0) return new(configuration.StartingCash, configuration.StartingCash, 0, 0, [], [], []);

        var replayPrefix = new List<Candle>(ordered.Length);
        var cash = configuration.StartingCash;
        var quantity = 0;
        var averageEntry = 0m;
        var trades = new List<BacktestTrade>();
        var riskEvents = new List<BacktestRiskEvent>(ordered.Length);
        var curve = new List<BacktestEquityPoint>(ordered.Length);
        var peak = configuration.StartingCash;
        var maxDrawdown = 0m;

        foreach (var candle in ordered)
        {
            replayPrefix.Add(candle.ToCandle());
            var recommendation = recommendationEngine.Evaluate(symbol, replayPrefix, candle.Timestamp);
            var risk = riskEngine.Evaluate(recommendation, cash, configuration.QuantityPerTrade);
            riskEvents.Add(new(candle.Timestamp, risk.Decision, risk.Reason, cash, configuration.QuantityPerTrade));

            if (risk.Decision == RiskDecision.Approved)
            {
                var fillPrice = candle.Close * (1m + configuration.SlippageBasisPoints / 10000m);
                var gross = fillPrice * configuration.QuantityPerTrade;
                var fee = gross * configuration.FeeRate;
                var total = gross + fee;
                if (total <= cash)
                {
                    var oldValue = averageEntry * quantity;
                    quantity += configuration.QuantityPerTrade;
                    averageEntry = quantity == configuration.QuantityPerTrade ? fillPrice : (oldValue + gross) / quantity;
                    cash -= total;
                    trades.Add(new(candle.Timestamp, OrderSide.Buy, configuration.QuantityPerTrade, fillPrice, fee, risk.Decision, risk.Reason));
                }
            }

            var positionValue = quantity * candle.Close;
            var equity = cash + positionValue;
            peak = Math.Max(peak, equity);
            if (peak > 0) maxDrawdown = Math.Max(maxDrawdown, (peak - equity) / peak * 100m);
            curve.Add(new(candle.Timestamp, cash, positionValue, equity));
        }

        var ending = curve[^1].Equity;
        var returnPercent = (ending - configuration.StartingCash) / configuration.StartingCash * 100m;
        return new(configuration.StartingCash, ending, returnPercent, maxDrawdown, trades, curve, riskEvents);
    }

    private static void Validate(BacktestConfiguration configuration)
    {
        if (configuration.StartingCash <= 0) throw new ArgumentOutOfRangeException(nameof(configuration.StartingCash));
        if (configuration.QuantityPerTrade <= 0) throw new ArgumentOutOfRangeException(nameof(configuration.QuantityPerTrade));
        if (configuration.FeeRate < 0 || configuration.FeeRate > 1) throw new ArgumentOutOfRangeException(nameof(configuration.FeeRate));
        if (configuration.SlippageBasisPoints < 0 || configuration.SlippageBasisPoints > 10000) throw new ArgumentOutOfRangeException(nameof(configuration.SlippageBasisPoints));
        if (!string.Equals(configuration.StrategyVersion, "baseline-v1", StringComparison.Ordinal)) throw new ArgumentException("Unsupported strategy version.", nameof(configuration.StrategyVersion));
    }
}
