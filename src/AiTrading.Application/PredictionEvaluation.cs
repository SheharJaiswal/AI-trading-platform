namespace AiTrading.Application;

public sealed record PredictionRecord(Guid Id, string Symbol, string Horizon, decimal ExpectedReturn, decimal ProbabilityPositive, decimal Confidence, string ModelVersion, DateTimeOffset DataTimestamp, DateTimeOffset RecordedAt);
public sealed record PredictionOutcome(Guid PredictionId, decimal ActualReturn, DateTimeOffset OutcomeTimestamp);
public sealed record PredictionEvaluation(Guid PredictionId, string Symbol, string Horizon, decimal ExpectedReturn, decimal ActualReturn, bool DirectionCorrect, DateTimeOffset DataTimestamp, DateTimeOffset OutcomeTimestamp, string ModelVersion);
public sealed record EvaluationMetrics(int Total, int Evaluated, int Wins, int Losses, decimal DirectionalAccuracy, decimal CumulativeReturn, decimal MaxDrawdown, decimal UnitPnl);

public interface IPredictionEvaluationRepository
{
    Task AddPredictionAsync(PredictionRecord prediction, CancellationToken cancellationToken);
    Task<PredictionRecord?> GetPredictionAsync(Guid predictionId, CancellationToken cancellationToken);
    Task AddOutcomeAsync(PredictionOutcome outcome, CancellationToken cancellationToken);
    Task<IReadOnlyList<PredictionEvaluation>> GetEvaluationsAsync(string? symbol, CancellationToken cancellationToken);
}

public sealed class PredictionEvaluationService(IPredictionEvaluationRepository repository)
{
    public async Task<PredictionRecord> RecordPredictionAsync(string symbol, string horizon, decimal expectedReturn, decimal probabilityPositive, decimal confidence, string modelVersion, DateTimeOffset dataTimestamp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol)) throw new ArgumentException("symbol is required.");
        if (string.IsNullOrWhiteSpace(horizon)) throw new ArgumentException("horizon is required.");
        if (string.IsNullOrWhiteSpace(modelVersion)) throw new ArgumentException("modelVersion is required.");
        if (probabilityPositive is < 0m or > 1m) throw new ArgumentException("probabilityPositive must be between 0 and 1.");
        if (confidence is < 0m or > 1m) throw new ArgumentException("confidence must be between 0 and 1.");
        var prediction = new PredictionRecord(Guid.NewGuid(), symbol.Trim().ToUpperInvariant(), horizon.Trim(), expectedReturn, probabilityPositive, confidence, modelVersion.Trim(), dataTimestamp, DateTimeOffset.UtcNow);
        await repository.AddPredictionAsync(prediction, cancellationToken);
        return prediction;
    }

    public async Task RecordOutcomeAsync(PredictionOutcome outcome, CancellationToken cancellationToken)
    {
        if (outcome.PredictionId == Guid.Empty) throw new ArgumentException("predictionId is required.");
        var prediction = await repository.GetPredictionAsync(outcome.PredictionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Prediction {outcome.PredictionId} does not exist.");
        if (outcome.OutcomeTimestamp < prediction.DataTimestamp)
            throw new InvalidOperationException("Outcome timestamp cannot precede prediction data timestamp.");
        await repository.AddOutcomeAsync(outcome, cancellationToken);
    }

    public async Task<EvaluationMetrics> GetMetricsAsync(string? symbol, CancellationToken cancellationToken)
    {
        var evaluations = await repository.GetEvaluationsAsync(symbol, cancellationToken);
        var evaluated = evaluations.OrderBy(x => x.OutcomeTimestamp).ToArray();
        var wins = evaluated.Count(x => x.ActualReturn > 0m);
        var losses = evaluated.Count(x => x.ActualReturn < 0m);
        var cumulative = 0m;
        var peak = 0m;
        var maxDrawdown = 0m;
        foreach (var item in evaluated) { cumulative += item.ActualReturn; peak = Math.Max(peak, cumulative); maxDrawdown = Math.Max(maxDrawdown, peak - cumulative); }
        return new EvaluationMetrics(evaluations.Count, evaluated.Length, wins, losses, evaluated.Length == 0 ? 0m : (decimal)evaluated.Count(x => x.DirectionCorrect) / evaluated.Length, cumulative, maxDrawdown, cumulative);
    }
}
