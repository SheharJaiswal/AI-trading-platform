using AiTrading.Domain;
using System.Text.Json;

namespace AiTrading.Application;

public sealed record BacktestRunState(Guid Id, Symbol Symbol, string Interval, DateTimeOffset Start, DateTimeOffset End, BacktestConfiguration Configuration, BacktestResult Result, DateTimeOffset CreatedAt);

public interface IBacktestRunRepository
{
    Task AddAsync(BacktestRunState run, CancellationToken cancellationToken);
    Task<BacktestRunState?> GetAsync(Guid id, CancellationToken cancellationToken);
}

public static class BacktestRunSerialization
{
    public static string Serialize(BacktestResult result) => JsonSerializer.Serialize(result);
    public static BacktestResult Deserialize(string json) => JsonSerializer.Deserialize<BacktestResult>(json) ?? throw new InvalidOperationException("Persisted backtest result is invalid.");
}
