using AiTrading.Application;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Infrastructure.Persistence;

public sealed class EfMonitoringRunRepository(IDbContextFactory<TradingDbContext> contextFactory) : IMonitoringRunRepository
{
    public async Task AddAsync(MonitoringRunState run, CancellationToken cancellationToken)
    {
        ValidateState(run);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.Set<MonitoringRunRecord>().Add(new MonitoringRunRecord
        {
            Id = run.Id,
            StartedAt = run.StartedAt,
            CompletedAt = run.CompletedAt,
            Status = run.Status.ToString(),
            PositionCount = run.PositionCount,
            FailureCount = run.FailureCount
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(Guid id, DateTimeOffset completedAt, MonitoringRunStatus status, int positionCount, int failureCount, CancellationToken cancellationToken)
    {
        var completed = new MonitoringRunState(id, DateTimeOffset.MinValue, completedAt, status, positionCount, failureCount);
        ValidateCompletion(completed);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await db.Set<MonitoringRunRecord>().AsNoTracking().SingleAsync(x => x.Id == id, cancellationToken);
        if (record.StartedAt == default)
            throw new InvalidOperationException($"Monitoring run {id} has an invalid persisted start timestamp; monitoring state requires reconciliation.");

        if (completedAt < record.StartedAt)
            throw new InvalidOperationException($"Monitoring run {id} has a completion timestamp earlier than its start timestamp; monitoring state requires reconciliation.");

        var updated = await db.Set<MonitoringRunRecord>()
            .Where(x => x.Id == id &&
                        x.Status == MonitoringRunStatus.Running.ToString() &&
                        x.CompletedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.CompletedAt, completedAt)
                .SetProperty(x => x.Status, status.ToString())
                .SetProperty(x => x.PositionCount, positionCount)
                .SetProperty(x => x.FailureCount, failureCount), cancellationToken);

        if (updated != 1)
            throw new InvalidOperationException($"Monitoring run {id} is no longer running; completion was rejected to preserve terminal state integrity.");
    }

    public async Task<int> RecoverStaleRunningAsync(DateTimeOffset startedBefore, DateTimeOffset recoveredAt, CancellationToken cancellationToken)
    {
        if (recoveredAt < startedBefore)
            throw new ArgumentException("Recovery timestamp must not precede the stale-run cutoff.", nameof(recoveredAt));

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var staleRuns = await db.Set<MonitoringRunRecord>()
            .Where(x => x.Status == MonitoringRunStatus.Running.ToString() && x.CompletedAt == null && x.StartedAt < startedBefore)
            .ToListAsync(cancellationToken);

        foreach (var record in staleRuns)
        {
            if (record.StartedAt == default)
                throw new InvalidOperationException($"Monitoring run {record.Id} has an invalid persisted start timestamp; monitoring state requires reconciliation.");

            if (record.PositionCount != 0 || record.FailureCount != 0)
                throw new InvalidOperationException($"Monitoring run {record.Id} has inconsistent running counts; monitoring state requires reconciliation.");

            if (recoveredAt < record.StartedAt)
                throw new InvalidOperationException($"Monitoring run {record.Id} has a recovery timestamp earlier than its start timestamp; monitoring state requires reconciliation.");

            record.CompletedAt = recoveredAt;
            record.Status = MonitoringRunStatus.Failed.ToString();
            record.PositionCount = 0;
            record.FailureCount = 1;
        }

        if (staleRuns.Count > 0)
            await db.SaveChangesAsync(cancellationToken);

        return staleRuns.Count;
    }

    public async Task<MonitoringRunState?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await db.Set<MonitoringRunRecord>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        return record is null ? null : ToState(record);
    }

    public async Task<IReadOnlyList<MonitoringRunState>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 100);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var records = await db.Set<MonitoringRunRecord>()
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .ThenByDescending(x => x.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return records.Select(ToState).ToList();
    }

    private static MonitoringRunState ToState(MonitoringRunRecord record)
    {
        if (record.Id == Guid.Empty)
            throw new InvalidOperationException("Monitoring run has an invalid id; monitoring state requires reconciliation.");

        MonitoringRunStatus status;
        if (!Enum.TryParse(record.Status, ignoreCase: false, out status) || !Enum.IsDefined(status))
            throw new InvalidOperationException($"Monitoring run {record.Id} has an invalid persisted status; monitoring state requires reconciliation.");

        var state = new MonitoringRunState(
            record.Id,
            record.StartedAt,
            record.CompletedAt,
            status,
            record.PositionCount,
            record.FailureCount);

        ValidateState(state);
        return state;
    }

    private static void ValidateState(MonitoringRunState state)
    {
        if (state.StartedAt == default)
            throw new InvalidOperationException($"Monitoring run {state.Id} has an invalid persisted start timestamp; monitoring state requires reconciliation.");

        if (state.PositionCount < 0 || state.FailureCount < 0)
            throw new InvalidOperationException($"Monitoring run {state.Id} has negative persisted counts; monitoring state requires reconciliation.");

        if (state.CompletedAt is not null && state.CompletedAt < state.StartedAt)
            throw new InvalidOperationException($"Monitoring run {state.Id} has a completion timestamp earlier than its start timestamp; monitoring state requires reconciliation.");

        if (state.Status == MonitoringRunStatus.Running)
        {
            if (state.CompletedAt is not null || state.PositionCount != 0 || state.FailureCount != 0)
                throw new InvalidOperationException($"Monitoring run {state.Id} has inconsistent running state; monitoring state requires reconciliation.");

            return;
        }

        if (state.CompletedAt is null)
            throw new InvalidOperationException($"Monitoring run {state.Id} has a terminal status without a completion timestamp; monitoring state requires reconciliation.");

        if (state.Status == MonitoringRunStatus.Completed && state.FailureCount != 0)
            throw new InvalidOperationException($"Monitoring run {state.Id} has failures with a completed status; monitoring state requires reconciliation.");

        if (state.Status == MonitoringRunStatus.PartiallyFailed &&
            (state.FailureCount <= 0 || state.FailureCount >= state.PositionCount))
            throw new InvalidOperationException($"Monitoring run {state.Id} has inconsistent partial-failure counts; monitoring state requires reconciliation.");

        if (state.Status == MonitoringRunStatus.Failed &&
            state.FailureCount != state.PositionCount &&
            !(state.PositionCount == 0 && state.FailureCount == 1))
            throw new InvalidOperationException($"Monitoring run {state.Id} has inconsistent failed-run counts; monitoring state requires reconciliation.");
    }

    private static void ValidateCompletion(MonitoringRunState state)
    {
        if (state.Id == Guid.Empty)
            throw new ArgumentException("Monitoring-run id must not be empty.", nameof(state));

        if (state.Status == MonitoringRunStatus.Running)
            throw new ArgumentException("A completed monitoring run cannot remain running.", nameof(state));

        if (state.PositionCount < 0 || state.FailureCount < 0)
            throw new ArgumentException("Monitoring-run counts must not be negative.", nameof(state));

        if (state.Status == MonitoringRunStatus.Completed && state.FailureCount != 0)
            throw new ArgumentException("A completed monitoring run cannot contain failures.", nameof(state));

        if (state.Status == MonitoringRunStatus.PartiallyFailed &&
            (state.FailureCount <= 0 || state.FailureCount >= state.PositionCount))
            throw new ArgumentException("Partially failed monitoring runs require both successes and failures.", nameof(state));

        if (state.Status == MonitoringRunStatus.Failed &&
            state.FailureCount != state.PositionCount &&
            !(state.PositionCount == 0 && state.FailureCount == 1))
            throw new ArgumentException("Failed monitoring-run counts are inconsistent.", nameof(state));
    }
}
