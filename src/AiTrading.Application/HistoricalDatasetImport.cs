using System.Security.Cryptography;
using System.Text;
using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record HistoricalDatasetImportResult(bool IsSuccess, int ImportedCount, IReadOnlyList<string> Errors)
{
    public static HistoricalDatasetImportResult Success(int count) => new(true, count, []);
}

public interface IHistoricalDatasetImporter
{
    Task<HistoricalDatasetImportResult> ImportCsvAsync(string csv, CancellationToken cancellationToken);
}

public sealed class HistoricalDatasetCsvImporter(ITradingUnitOfWorkFactory unitOfWorkFactory) : IHistoricalDatasetImporter
{
    public async Task<HistoricalDatasetImportResult> ImportCsvAsync(string csv, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var parsed = HistoricalDatasetCsvParser.Parse(csv);
        if (!parsed.IsValid) return new(false, 0, parsed.Errors);

        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var states = parsed.Candles.Select(candle => new HistoricalCandleState(
            DeterministicId(candle), candle.Symbol, candle.Interval, candle.Timestamp,
            candle.Open, candle.High, candle.Low, candle.Close, candle.Volume,
            candle.Source, candle.ReceivedAt)).ToArray();
        await unitOfWork.HistoricalCandles.AddRangeAsync(states, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return HistoricalDatasetImportResult.Success(states.Length);
    }

    private static Guid DeterministicId(HistoricalCandle candle)
    {
        var key = $"{candle.Symbol.Value}|{candle.Symbol.InstrumentToken}|{candle.Interval}|{candle.Timestamp:O}|{candle.Source}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return new Guid(bytes.AsSpan(0, 16));
    }
}
