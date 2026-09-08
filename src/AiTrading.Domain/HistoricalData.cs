namespace AiTrading.Domain;

public sealed record HistoricalCandle(
    Symbol Symbol,
    string Interval,
    DateTimeOffset Timestamp,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    long Volume,
    string Source,
    DateTimeOffset ReceivedAt)
{
    public Candle ToCandle() => new(Timestamp, Open, High, Low, Close, Volume);
}

public sealed record HistoricalDataValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    int CandleCount)
{
    public static HistoricalDataValidationResult Valid(int count) => new(true, [], count);
}

public static class HistoricalDataValidator
{
    public static HistoricalDataValidationResult Validate(IReadOnlyList<HistoricalCandle> candles)
    {
        if (candles.Count == 0) return new(false, ["At least one historical candle is required."], 0);
        var errors = new List<string>();
        for (var i = 0; i < candles.Count; i++)
        {
            var c = candles[i];
            if (string.IsNullOrWhiteSpace(c.Symbol.Value)) errors.Add($"Candle {i} has no symbol.");
            if (string.IsNullOrWhiteSpace(c.Interval)) errors.Add($"Candle {i} has no interval.");
            if (string.IsNullOrWhiteSpace(c.Source)) errors.Add($"Candle {i} has no source.");
            if (c.Open <= 0 || c.High <= 0 || c.Low <= 0 || c.Close <= 0) errors.Add($"Candle {i} contains a non-positive price.");
            if (c.High < Math.Max(c.Open, c.Close) || c.Low > Math.Min(c.Open, c.Close) || c.High < c.Low) errors.Add($"Candle {i} has invalid OHLC bounds.");
            if (c.Volume < 0) errors.Add($"Candle {i} has negative volume.");
            if (i > 0)
            {
                var previous = candles[i - 1];
                if (c.Symbol != previous.Symbol || !string.Equals(c.Interval, previous.Interval, StringComparison.OrdinalIgnoreCase)) errors.Add($"Candle {i} changes symbol or interval within the dataset.");
                if (c.Timestamp <= previous.Timestamp) errors.Add($"Candle {i} is not strictly after the previous timestamp.");
            }
        }
        return errors.Count == 0 ? HistoricalDataValidationResult.Valid(candles.Count) : new(false, errors, candles.Count);
    }
}
