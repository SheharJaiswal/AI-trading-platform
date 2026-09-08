using System.Globalization;
using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record HistoricalDatasetImportResult(
    bool IsSuccess,
    int ImportedCount,
    IReadOnlyList<string> Errors)
{
    public static HistoricalDatasetImportResult Success(int count) => new(true, count, []);
}

public interface IHistoricalDatasetImporter
{
    Task<HistoricalDatasetImportResult> ImportCsvAsync(
        string csv,
        CancellationToken cancellationToken);
}

public sealed class HistoricalDatasetCsvImporter(
    ITradingUnitOfWorkFactory unitOfWorkFactory) : IHistoricalDatasetImporter
{
    private static readonly string[] Headers =
    ["symbol", "instrumentToken", "interval", "timestamp", "open", "high", "low", "close", "volume", "source", "receivedAt"];

    public async Task<HistoricalDatasetImportResult> ImportCsvAsync(
        string csv,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return new(false, 0, ["CSV content is required."]);

        using var reader = new StringReader(csv);
        var header = await reader.ReadLineAsync(cancellationToken);
        if (header is null || !string.Equals(header.Trim(), string.Join(',', Headers), StringComparison.OrdinalIgnoreCase))
            return new(false, 0, [$"CSV header must be: {string.Join(',', Headers)}"]);

        var candles = new List<HistoricalCandle>();
        var errors = new List<string>();
        var lineNumber = 1;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line)) continue;
            var fields = line.Split(',', StringSplitOptions.None);
            if (fields.Length != Headers.Length)
            {
                errors.Add($"Line {lineNumber} must contain {Headers.Length} fields.");
                continue;
            }

            if (!decimal.TryParse(fields[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var open) ||
                !decimal.TryParse(fields[5], NumberStyles.Number, CultureInfo.InvariantCulture, out var high) ||
                !decimal.TryParse(fields[6], NumberStyles.Number, CultureInfo.InvariantCulture, out var low) ||
                !decimal.TryParse(fields[7], NumberStyles.Number, CultureInfo.InvariantCulture, out var close) ||
                !long.TryParse(fields[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var volume) ||
                !DateTimeOffset.TryParse(fields[3], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp) ||
                !DateTimeOffset.TryParse(fields[10], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var receivedAt))
            {
                errors.Add($"Line {lineNumber} contains an invalid numeric or timestamp value.");
                continue;
            }

            candles.Add(new HistoricalCandle(
                new Symbol(fields[0].Trim(), string.IsNullOrWhiteSpace(fields[1]) ? null : fields[1].Trim()),
                fields[2].Trim(), timestamp, open, high, low, close, volume, fields[9].Trim(), receivedAt));
        }

        if (errors.Count > 0) return new(false, 0, errors);
        var validation = HistoricalDataValidator.Validate(candles);
        if (!validation.IsValid) return new(false, 0, validation.Errors);

        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var states = candles.Select((candle, index) => new HistoricalCandleState(
            DeterministicId(candle, index), candle.Symbol, candle.Interval, candle.Timestamp,
            candle.Open, candle.High, candle.Low, candle.Close, candle.Volume,
            candle.Source, candle.ReceivedAt)).ToArray();
        await unitOfWork.HistoricalCandles.AddRangeAsync(states, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return HistoricalDatasetImportResult.Success(candles.Count);
    }

    private static Guid DeterministicId(HistoricalCandle candle, int index) =>
        Guid.NewGuid();
}
