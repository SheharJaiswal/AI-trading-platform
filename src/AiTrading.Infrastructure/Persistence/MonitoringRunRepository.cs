using AiTrading.Application;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Infrastructure.Persistence;

public sealed class EfMonitoringRunRepository(IDbContextFactory<TradingDbContext> contextFactory) : IMonitoringRunRepository
{
    public async Task AddAsync(MonitoringRunState run, CancellationToken cancellationToken)
    {
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
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await db.Set<MonitoringRunRecord>().SingleAsync(x => x.Id == id, cancellationToken);
        record.CompletedAt = completedAt;
        record.Status = status.ToString();
        record.PositionCount = positionCount;
        record.FailureCount = failureCount;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RecoverStaleRunningAsync(DateTimeOffset startedBefore, DateTimeOffset recoveredAt, CancellationToken cancellationToken)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var staleRuns = await db.Set<MonitoringRunRecord>()
            .Where(x => x.Status == MonitoringRunStatus.Running.ToString() && x.CompletedAt == null && x.StartedAt < startedBefore)
            .ToListAsync(cancellationToken);

        foreach (var record in staleRuns)
        {
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
        return await db.Set<MonitoringRunRecord>().AsNoTracking().Where(x => x.Id == id).Select(x => new MonitoringRunState(x.Id, x.StartedAt, x.CompletedAt, Enum.Parse<MonitoringRunStatus>(x.Status), x.PositionCount, x.FailureCount)).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MonitoringRunState>> GetRecentAsync(int limit, CancellationToken cancellationToken)
    {
        limit = Math.Clamp(limit, 1, 100);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.Set<MonitoringRunRecord>().AsNoTracking().OrderByDescending(x => x.StartedAt).Take(limit).Select(x => new MonitoringRunState(x.Id, x.StartedAt, x.CompletedAt, Enum.Parse<MonitoringRunStatus>(x.Status), x.PositionCount, x.FailureCount)).ToListAsync(cancellationToken);
    }
}
