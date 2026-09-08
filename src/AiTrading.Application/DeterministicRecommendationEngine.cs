using AiTrading.Domain;

namespace AiTrading.Application;

public sealed class DeterministicRecommendationEngine
{
    public Recommendation Evaluate(Symbol symbol, IReadOnlyList<Candle> candles, DateTimeOffset evaluationTime)
    {
        var available = candles.Where(x => x.Timestamp <= evaluationTime).OrderBy(x => x.Timestamp).ToArray();
        if (available.Length == 0)
            return new(symbol, RecommendationAction.NoDecision, 0, null, 0, 1, [], ["INSUFFICIENT_DATA"], evaluationTime, "baseline-v1");

        var current = available[^1];
        if (available.Length < 20)
            return new(symbol, RecommendationAction.NoDecision, current.Close, null, 0, 1, [], ["INSUFFICIENT_DATA"], evaluationTime, "baseline-v1");

        var technical = TechnicalAnalysis.Snapshot(available);
        var patterns = CandlestickAnalysis.Detect(available);
        var signals = new List<string>();
        if (technical.Sma20 is { } sma && current.Close > sma) signals.Add("PRICE_ABOVE_SMA20");
        if (technical.Rsi14 is { } rsi && rsi < 70) signals.Add("RSI_NOT_OVERBOUGHT");
        signals.AddRange(patterns.Where(x => x.Bullish).Select(x => $"BULLISH_{x.Name.Replace(' ', '_').ToUpperInvariant()}"));
        var bullish = signals.Count >= 2;
        return new(symbol, bullish ? RecommendationAction.Buy : RecommendationAction.Hold, current.Close, null, bullish ? 0.60m : 0.40m, 1, signals, [], evaluationTime, "baseline-v1");
    }
}
