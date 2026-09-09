namespace AiTrading.Application;

public enum MonitoringRunStatus
{
    Running,
    Completed,
    PartiallyFailed,
    Failed
}

public sealed record MonitoringRunState(
    Guid Id,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    MonitoringRunStatus Status,
    int PositionCount,
    int FailureCount);

public sealed record MonitoringRunResult(
    Guid Id,
    MonitoringRunStatus Status,
    int PositionCount,
    int FailureCount,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt);

public interface IMonitoringRunRepository
{
    Task AddAsync(MonitoringRunState run, CancellationToken cancellationToken);
    Task CompleteAsync(Guid id, DateTimeOffset completedAt, MonitoringRunStatus status, int positionCount, int failureCount, CancellationToken cancellationToken);
    Task<MonitoringRunState?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<MonitoringRunState>> GetRecentAsync(int limit, CancellationToken cancellationToken);
}

public sealed class MonitoringRunService(
    DurableRiskMonitor monitor,
    IMonitoringRunRepository repository,
    TimeProvider timeProvider)
{
    public async Task<MonitoringRunResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        var startedAt = timeProvider.GetUtcNow();
        var id = Guid.NewGuid();
        var run = new MonitoringRunState(id, startedAt, null, MonitoringRunStatus.Running, 0, 0);
        await repository.AddAsync(run, cancellationToken);

        try
        {
            var summary = await monitor.CheckOnceAsync(cancellationToken);
            var completedAt = timeProvider.GetUtcNow();
            var status = summary.FailureCount == 0
                ? MonitoringRunStatus.Completed
                : summary.SuccessCount == 0
                    ? MonitoringRunStatus.Failed
                    : MonitoringRunStatus.PartiallyFailed;
            await repository.CompleteAsync(id, completedAt, status, summary.PositionCount, summary.FailureCount, cancellationToken);
            return new MonitoringRunResult(id, status, summary.PositionCount, summary.FailureCount, startedAt, completedAt);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            var completedAt = timeProvider.GetUtcNow();
            await repository.CompleteAsync(id, completedAt, MonitoringRunStatus.Failed, 0, 1, CancellationToken.None);
            throw;
        }
    }
}
