namespace AiTrading.Domain;

public readonly record struct Symbol(string Value, string? InstrumentToken = null)
{
    public override string ToString() => Value;
}

public sealed record Candle(DateTimeOffset Timestamp, decimal Open, decimal High, decimal Low, decimal Close, long Volume);
public sealed record MarketQuote(Symbol Symbol, string Exchange, string InstrumentToken, DateTimeOffset Timestamp, decimal Open, decimal High, decimal Low, decimal Close, decimal LastTradedPrice, long Volume, string Source);

public enum RecommendationAction { Buy, Hold, Sell, NoDecision }
public enum RiskDecision { Approved, RiskBlocked, InsufficientData }
public enum OrderSide { Buy, Sell }
public enum AlertSeverity { Info, Warning, High, Critical }

public sealed record TechnicalSnapshot(decimal? Sma20, decimal? Rsi14, decimal DailyChangePercent);
public sealed record CandlestickPattern(string Name, bool Bullish);
public sealed record Recommendation(Symbol Symbol, RecommendationAction Action, decimal ReferencePrice, decimal? ExpectedReturn, decimal Confidence, int HorizonDays, IReadOnlyList<string> SupportingSignals, IReadOnlyList<string> RiskFactors, DateTimeOffset GeneratedAt, string StrategyVersion);
public sealed record RiskResult(RiskDecision Decision, string? Reason);
public sealed record PaperOrder(Guid Id, Symbol Symbol, OrderSide Side, int Quantity, decimal LimitPrice, DateTimeOffset CreatedAt);
public sealed record Fill(Guid OrderId, Symbol Symbol, OrderSide Side, int Quantity, decimal Price, DateTimeOffset Timestamp, string Source);
public sealed record Position(Guid Id, Symbol Symbol, int Quantity, decimal AverageEntryPrice, decimal? StopLoss);
public sealed record Portfolio(decimal Cash, IReadOnlyList<Position> Positions, decimal UnrealizedPnl, decimal RealizedPnl);
public sealed record Alert(string Key, AlertSeverity Severity, string Message, DateTimeOffset CreatedAt, Symbol? Symbol);

public static class TechnicalAnalysis
{
    public static decimal? Sma(IReadOnlyList<decimal> values, int period)
    {
        if (period <= 0 || values.Count < period) return null;
        return values.TakeLast(period).Average();
    }

    public static decimal? Rsi(IReadOnlyList<decimal> closes, int period)
    {
        if (period <= 0 || closes.Count <= period) return null;
        var gains = 0m;
        var losses = 0m;
        for (var i = closes.Count - period; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            if (change >= 0) gains += change; else losses -= change;
        }
        if (losses == 0) return 100m;
        var rs = (gains / period) / (losses / period);
        return 100m - (100m / (1m + rs));
    }

    public static TechnicalSnapshot Snapshot(IReadOnlyList<Candle> candles)
    {
        var closes = candles.Select(x => x.Close).ToArray();
        var previous = closes.Length >= 2 ? closes[^2] : closes.LastOrDefault();
        var dailyChange = previous == 0 ? 0 : ((closes[^1] - previous) / previous) * 100m;
        return new TechnicalSnapshot(Sma(closes, 20), Rsi(closes, 14), dailyChange);
    }
}

public static class CandlestickAnalysis
{
    public static IReadOnlyList<CandlestickPattern> Detect(IReadOnlyList<Candle> candles)
    {
        if (candles.Count == 0) return [];
        var current = candles[^1];
        var body = Math.Abs(current.Close - current.Open);
        var range = current.High - current.Low;
        if (range <= 0) return [];
        var upper = current.High - Math.Max(current.Open, current.Close);
        var lower = Math.Min(current.Open, current.Close) - current.Low;
        var results = new List<CandlestickPattern>();
        if (body <= range * 0.1m) results.Add(new("Doji", current.Close >= current.Open));
        if (lower >= body * 2 && upper <= body) results.Add(new("Hammer", current.Close >= current.Open));
        if (upper >= body * 2 && lower <= body) results.Add(new("Shooting Star", false));
        if (candles.Count >= 2)
        {
            var previous = candles[^2];
            var previousBearish = previous.Close < previous.Open;
            var previousBullish = previous.Close > previous.Open;
            var currentBullish = current.Close > current.Open;
            var currentBearish = current.Close < current.Open;
            if (previousBearish && currentBullish && current.Open <= previous.Close && current.Close >= previous.Open)
                results.Add(new("Bullish Engulfing", true));
            if (previousBullish && currentBearish && current.Open >= previous.Close && current.Close <= previous.Open)
                results.Add(new("Bearish Engulfing", false));
        }
        return results;
    }
}
