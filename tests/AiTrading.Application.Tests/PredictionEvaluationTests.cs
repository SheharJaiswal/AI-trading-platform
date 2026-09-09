using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class PredictionEvaluationTests
{
 [Fact]
 public async Task Metrics_calculate_accuracy_wins_losses_and_drawdown_without_lookahead()
 {
  var repository=new FakeEvaluationRepository(); var service=new PredictionEvaluationService(repository); var t=DateTimeOffset.Parse("2026-01-01T00:00:00Z");
  var p1=await service.RecordPredictionAsync("TCS","1h",.02m,.7m,.8m,"model-1",t,CancellationToken.None);
  var p2=await service.RecordPredictionAsync("TCS","1h",-.01m,.3m,.7m,"model-1",t.AddHours(1),CancellationToken.None);
  await service.RecordOutcomeAsync(new PredictionOutcome(p1.Id,.03m,t.AddHours(1)),CancellationToken.None);
  await service.RecordOutcomeAsync(new PredictionOutcome(p2.Id,-.02m,t.AddHours(2)),CancellationToken.None);
  var metrics=await service.GetMetricsAsync("TCS",CancellationToken.None);
  Assert.Equal(2,metrics.Evaluated); Assert.Equal(1,metrics.Wins); Assert.Equal(1,metrics.Losses); Assert.Equal(1m,metrics.DirectionalAccuracy); Assert.Equal(.01m,metrics.CumulativeReturn); Assert.Equal(.01m,metrics.UnitPnl);
 }

 [Fact]
 public async Task Outcome_before_prediction_data_is_rejected()
 {
  var repository=new FakeEvaluationRepository(); var service=new PredictionEvaluationService(repository); var t=DateTimeOffset.Parse("2026-01-01T00:00:00Z");
  var prediction=await service.RecordPredictionAsync("TCS","1h",.01m,.6m,.7m,"model-1",t,CancellationToken.None);
  await Assert.ThrowsAsync<InvalidOperationException>(()=>service.RecordOutcomeAsync(new PredictionOutcome(prediction.Id,.01m,t.AddMinutes(-1)),CancellationToken.None));
 }

 private sealed class FakeEvaluationRepository : IPredictionEvaluationRepository
 {
  private readonly List<PredictionRecord> predictions=[]; private readonly List<PredictionOutcome> outcomes=[];
  public Task AddPredictionAsync(PredictionRecord prediction,CancellationToken _) { predictions.Add(prediction); return Task.CompletedTask; }
  public Task<PredictionRecord?> GetPredictionAsync(Guid id,CancellationToken _) => Task.FromResult(predictions.SingleOrDefault(x=>x.Id==id));
  public Task AddOutcomeAsync(PredictionOutcome outcome,CancellationToken _) { if(outcomes.Any(x=>x.PredictionId==outcome.PredictionId)) throw new InvalidOperationException(); outcomes.Add(outcome); return Task.CompletedTask; }
  public Task<IReadOnlyList<PredictionEvaluation>> GetEvaluationsAsync(string? symbol,CancellationToken _)
  { var q=predictions.Join(outcomes,p=>p.Id,o=>o.PredictionId,(p,o)=>new PredictionEvaluation(p.Id,p.Symbol,p.Horizon,p.ExpectedReturn,o.ActualReturn,Math.Sign(p.ExpectedReturn)==Math.Sign(o.ActualReturn),p.DataTimestamp,o.OutcomeTimestamp,p.ModelVersion)); if(!string.IsNullOrWhiteSpace(symbol)) q=q.Where(x=>x.Symbol==symbol.ToUpperInvariant()); return Task.FromResult<IReadOnlyList<PredictionEvaluation>>(q.OrderBy(x=>x.OutcomeTimestamp).ToList()); }
 }
}
