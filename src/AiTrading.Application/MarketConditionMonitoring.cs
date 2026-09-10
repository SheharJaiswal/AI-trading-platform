using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record MarketConditionMonitoringOptions(
    decimal SuddenMovePercent = 3m,
    decimal AbnormalVolumeMultiplier = 2m)
{
    public void Validate()
    {
        if (SuddenMovePercent <= 0m) throw new ArgumentOutOfRangeException(nameof(SuddenMovePercent));
        if (AbnormalVolumeMultiplier <= 0m) throw new ArgumentOutOfRangeException(nameof(AbnormalVolumeMultiplier));
    }
}

public sealed record MarketConditionEvent(
    Symbol Symbol,
    string Type,
    AlertSeverity Severity,
    string Message,
    DateTimeOffset Timestamp,
    decimal? CurrentValue,
    decimal? ReferenceValue);

public sealed class MarketConditionDetector(MarketConditionMonitoringOptions options)
{
    public IReadOnlyList<MarketConditionEvent> Evaluate(
        Symbol symbol,
        MarketQuote current,
        IReadOnlyList<Candle> history)
    {
        options.Validate();
        var events = new List<MarketConditionEvent>();
        if (current.Symbol != symbol || current.LastTradedPrice <= 0m)
            return events;

        var previous = history
            .Where(x => x.Timestamp < current.Timestamp && x.Close > 0m)
            .OrderByDescending(x => x.Timestamp)
            .FirstOrDefault();

        if (previous is not null)
        {
            var move = ((current.LastTradedPrice - previous.Close) / previous.Close) * 100m;
            if (Math.Abs(move) >= options.SuddenMovePercent)
            {
                events.Add(new MarketConditionEvent(
                    symbol,
                    "SUDDEN_PRICE_MOVE",
                    Math.Abs(move) >= options.SuddenMovePercent * 2m ? AlertSeverity.High : AlertSeverity.Warning,
                    $"Sudden price move of {move:F2}% detected for {symbol}.",
                    current.Timestamp,
                    current.LastTradedPrice,
                    previous.Close));
            }
        }

        var volumeReference = history
            .Where(x => x.Timestamp < current.Timestamp && x.Volume > 0)
            .OrderByDescending(x => x.Timestamp)
            .Take(20)
            .Select(x => (decimal)x.Volume)
            .ToArray();
        if (current.Volume > 0 && volumeReference.Length > 0)
        {
            var averageVolume = volumeReference.Average();
            if (averageVolume > 0m && current.Volume >= averageVolume * options.AbnormalVolumeMultiplier)
            {
                events.Add(new MarketConditionEvent(
                    symbol,
                    "ABNORMAL_VOLUME",
                    AlertSeverity.Warning,
                    $"Volume is {current.Volume / averageVolume:F2}x the recent average for {symbol}.",
                    current.Timestamp,
                    current.Volume,
                    averageVolume));
            }
        }

        return events;
    }
}
