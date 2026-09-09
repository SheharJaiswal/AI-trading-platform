using AiTrading.Application;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Infrastructure.Persistence;

public sealed class EfPredictionEvaluationRepository(IDbContextFactory<TradingDbContext> contextFactory) : IPredictionEvaluationRepository
{
    public async Task AddPredictionAsync(PredictionRecord prediction, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.Predictions.Add(new PredictionRecordEntity { Id = prediction.Id, Symbol = prediction.Symbol, Horizon = prediction.Horizon, ExpectedReturn = prediction.ExpectedReturn, ProbabilityPositive = prediction.ProbabilityPositive, Confidence = prediction.Confidence, ModelVersion = prediction.ModelVersion, DataTimestamp = prediction.DataTimestamp, RecordedAt = prediction.RecordedAt });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PredictionRecord?> GetPredictionAsync(Guid predictionId, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Predictions.AsNoTracking().Where(x => x.Id == predictionId).Select(x => new PredictionRecord(x.Id, x.Symbol, x.Horizon, x.ExpectedReturn, x.ProbabilityPositive, x.Confidence, x.ModelVersion, x.DataTimestamp, x.RecordedAt)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task AddOutcomeAsync(PredictionOutcome outcome, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.Predictions.AnyAsync(x => x.Id == outcome.PredictionId, cancellationToken)) throw new KeyNotFoundException($"Prediction {outcome.PredictionId} does not exist.");
        if (await db.PredictionOutcomes.AnyAsync(x => x.PredictionId == outcome.PredictionId, cancellationToken)) throw new InvalidOperationException($"Prediction {outcome.PredictionId} already has an outcome.");
        db.PredictionOutcomes.Add(new PredictionOutcomeEntity { PredictionId = outcome.PredictionId, ActualReturn = outcome.ActualReturn, OutcomeTimestamp = outcome.OutcomeTimestamp });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PredictionEvaluation>> GetEvaluationsAsync(string? symbol, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Predictions.AsNoTracking().Join(db.PredictionOutcomes.AsNoTracking(), prediction => prediction.Id, outcome => outcome.PredictionId, (prediction, outcome) => new { prediction, outcome });
        if (!string.IsNullOrWhiteSpace(symbol)) query = query.Where(x => x.prediction.Symbol == symbol.Trim().ToUpperInvariant());
        return await query.OrderBy(x => x.outcome.OutcomeTimestamp).Select(x => new PredictionEvaluation(x.prediction.Id, x.prediction.Symbol, x.prediction.Horizon, x.prediction.ExpectedReturn, x.outcome.ActualReturn, Math.Sign(x.prediction.ExpectedReturn) == Math.Sign(x.outcome.ActualReturn), x.prediction.DataTimestamp, x.outcome.OutcomeTimestamp, x.prediction.ModelVersion)).ToListAsync(cancellationToken);
    }
}

public sealed class PredictionRecordEntity { public Guid Id { get; set; } public string Symbol { get; set; } = ""; public string Horizon { get; set; } = ""; public decimal ExpectedReturn { get; set; } public decimal ProbabilityPositive { get; set; } public decimal Confidence { get; set; } public string ModelVersion { get; set; } = ""; public DateTimeOffset DataTimestamp { get; set; } public DateTimeOffset RecordedAt { get; set; } }
public sealed class PredictionOutcomeEntity { public Guid PredictionId { get; set; } public decimal ActualReturn { get; set; } public DateTimeOffset OutcomeTimestamp { get; set; } }
