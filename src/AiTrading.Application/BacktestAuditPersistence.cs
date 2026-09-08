namespace AiTrading.Application;

public sealed record BacktestTradeAuditState(Guid Id, Guid BacktestRunId, DateTimeOffset Timestamp, string Side, int Quantity, decimal Price, decimal Fee, string RiskDecision, string? RiskReason);
public sealed record BacktestRiskAuditState(Guid Id, Guid BacktestRunId, DateTimeOffset Timestamp, string Decision, string? Reason, decimal AvailableCash, int RequestedQuantity);

public interface IBacktestAuditRepository
{
    Task AddAsync(Guid runId, IReadOnlyList<BacktestTradeAuditState> trades, IReadOnlyList<BacktestRiskAuditState> riskEvents, CancellationToken cancellationToken);
    Task<(IReadOnlyList<BacktestTradeAuditState> Trades, IReadOnlyList<BacktestRiskAuditState> RiskEvents)> GetAsync(Guid runId, CancellationToken cancellationToken);
}
