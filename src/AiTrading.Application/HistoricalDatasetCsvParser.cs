using System.Globalization;
using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record HistoricalCsvParseResult(
    IReadOnlyList<HistoricalCandle> Candles,
    IReadOnlyList<string> Errors)
{
    public bool IsValid => Errors.Count == 0 && Candles.Count > 0;
}

public static class HistoricalDatasetCsvParser
{
    private static readonly string[] Headers = ["symbol", "instrumentToken", "interval", "timestamp", "open", "high", "low", "close", "volume", "source", "receivedAt"];

    public static HistoricalCsvParseResult Parse(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new([], ["CSV content is required."]);
        using var reader = new StringReader(csv);
        var header = reader.ReadLine();
        if (header is null || !string.Equals(header.Trim(), string.Join(',', Headers), StringComparison.OrdinalIgnoreCase))
            return new([], [$"CSV header must be: {string.Join(',', Headers)}"]);

        var candles = new List<HistoricalCandle>();
        var errors = new List<string>();
        var lineNumber = 1;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = line.Split(',', StringSplitOptions.None);
            if (fields.Length != Headers.Length) { errors.Add($"Line {lineNumber} must contain {Headers.Length} fields."); continue; }
            if (!decimal.TryParse(fields[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var open) ||
                !decimal.TryParse(fields[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var high) ||
                !decimal.TryParse(fields[6], NumberStyles.Number, CultureInfo.InvariantCulture, out var low) ||
                !decimal.TryParse(fields[7], NumberStyles.Number, CultureInfo.InvariantCulture, out var close) ||
                !long.TryParse(fields[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var volume) ||
                !DateTimeOffset.TryParse(fields[3], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp) ||
                !DateTimeOffset.TryParse(fields[10], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var receivedAt))
            { errors.Add($"Line {lineNumber} contains an invalid numeric or timestamp value."); continue; }
            candles.Add(new HistoricalCandle(new Symbol(fields[0].Trim(), string.IsNullOrWhiteSpace(fields[1]) ? null : fields[1].Trim()), fields[2].Trim(), timestamp, open, high, low, close, volume, fields[9].Trim(), receivedAt));
        }
        if (errors.Count == 0)
        {
            var validation = HistoricalDataValidator.Validate(candles);
            if (!validation.IsValid) errors.AddRange(validation.Errors);
        }
        return new(candles, errors);
    }
}
