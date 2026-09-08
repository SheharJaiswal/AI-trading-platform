using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record BacktestRunRequest(Symbol Symbol, string Interval, DateTimeOffset Start, DateTimeOffset End, BacktestConfiguration Configuration);
public sealed record BacktestRunResponse(string Status, BacktestResult Result, string SimulationLabel);
public interface IBacktestService { Task<BacktestRunResponse> RunAsync(BacktestRunRequest request, CancellationToken cancellationToken); }
public sealed class BacktestService(ITradingUnitOfWorkFactory unitOfWorkFactory, DeterministicBacktestEngine engine) : IBacktestService
{
    public async Task<BacktestRunResponse> RunAsync(BacktestRunRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Symbol.Value)) throw new ArgumentException("Symbol is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Interval)) throw new ArgumentException("Interval is required.", nameof(request));
        if (request.End < request.Start) throw new ArgumentException("End must be on or after Start.", nameof(request));
        if (request.Configuration.StrategyVersion != "baseline-v1") throw new ArgumentException("Unsupported strategy version.", nameof(request));
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var states = await unitOfWork.HistoricalCandles.GetRangeAsync(request.Symbol, request.Interval, request.Start, request.End, cancellationToken);
        var candles = states.Select(x => x.ToDomain()).ToArray();
        if (candles.Length == 0) throw new InvalidOperationException("No historical data is available for the requested range.");
        return new("completed", engine.Run(request.Symbol, candles, request.Configuration), "HISTORICAL_SIMULATION_ONLY");
    }
}
