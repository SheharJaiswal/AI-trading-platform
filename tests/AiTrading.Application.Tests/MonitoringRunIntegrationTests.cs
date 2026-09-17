using AiTrading.Application;
using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AiTrading.Application.Tests;

public sealed class MonitoringRunIntegrationTests
{
    private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=ai_trading_test;User=root;Password=test;";

    [Fact]
    public async Task MonitoringRunService_Returns_Persisted_Run_By_Id()
    {
        await using var db = await CreateMigratedContextAsync();
        var id = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow.AddHours(-1);
        db.Set<MonitoringRunRecord>().Add(new MonitoringRunRecord
        {
            Id = id,
            StartedAt = startedAt,
            CompletedAt = startedAt.AddMinutes(1),
            Status = MonitoringRunStatus.Completed.ToString(),
            PositionCount = 3,
            FailureCount = 0
        });
        await db.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var runService = new MonitoringRunService(
            new DurableRiskMonitor(new FakeMarketDataProvider(), new TestUnitOfWorkFactory(options), Guid.NewGuid(), new NoopAlertDelivery()),
            new EfMonitoringRunRepository(new TestDbContextFactory(options)),
            TimeProvider.System);

        var run = await runService.GetAsync(id, CancellationToken.None);

        Assert.NotNull(run);
        Assert.Equal(id, run!.Id);
        Assert.Equal(MonitoringRunStatus.Completed, run.Status);
        Assert.Equal(3, run.PositionCount);
        Assert.Equal(0, run.FailureCount);
    }

    [Fact]
    public async Task MonitoringRunService_Rejects_NonPositive_History_Limit()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var runService = new MonitoringRunService(
            new DurableRiskMonitor(new FakeMarketDataProvider(), new TestUnitOfWorkFactory(options), Guid.NewGuid(), new NoopAlertDelivery()),
            new EfMonitoringRunRepository(new TestDbContextFactory(options)),
            TimeProvider.System);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => runService.GetRecentRunsAsync(0, CancellationToken.None));
    }

    [Fact]
    public async Task MonitoringRunService_Returns_Recent_Runs_In_Descending_Start_Order()
    {
        await using var db = await CreateMigratedContextAsync();
        var now = DateTimeOffset.UtcNow;
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        db.Set<MonitoringRunRecord>().AddRange(
            new MonitoringRunRecord
            {
                Id = olderId,
                StartedAt = now.AddHours(-2),
                CompletedAt = now.AddHours(-1),
                Status = MonitoringRunStatus.Completed.ToString(),
                PositionCount = 1,
                FailureCount = 0
            },
            new MonitoringRunRecord
            {
                Id = newerId,
                StartedAt = now.AddHours(-1),
                CompletedAt = now.AddMinutes(-30),
                Status = MonitoringRunStatus.Completed.ToString(),
                PositionCount = 2,
                FailureCount = 0
            });
        await db.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var runService = new MonitoringRunService(
            new DurableRiskMonitor(new FakeMarketDataProvider(), new TestUnitOfWorkFactory(options), Guid.NewGuid(), new NoopAlertDelivery()),
            new EfMonitoringRunRepository(new TestDbContextFactory(options)),
            TimeProvider.System);

        var runs = await runService.GetRecentRunsAsync(2, CancellationToken.None);

        Assert.Equal(2, runs.Count);
        Assert.Equal(newerId, runs[0].Id);
        Assert.Equal(olderId, runs[1].Id);
    }

    [Fact]
    public async Task MonitoringRunService_Uses_Id_As_TieBreaker_For_Equal_Start_Times()
    {
        await using var db = await CreateMigratedContextAsync();
        var startedAt = DateTimeOffset.UtcNow.AddHours(-1);
        var lowerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var higherId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        db.Set<MonitoringRunRecord>().AddRange(
            new MonitoringRunRecord
            {
                Id = lowerId,
                StartedAt = startedAt,
                CompletedAt = startedAt.AddMinutes(1),
                Status = MonitoringRunStatus.Failed.ToString(),
                PositionCount = 0,
                FailureCount = 1
            },
            new MonitoringRunRecord
            {
                Id = higherId,
                StartedAt = startedAt,
                CompletedAt = startedAt.AddMinutes(1),
                Status = MonitoringRunStatus.Failed.ToString(),
                PositionCount = 1,
                FailureCount = 1
            });
        await db.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var runService = new MonitoringRunService(
            new DurableRiskMonitor(new FakeMarketDataProvider(), new TestUnitOfWorkFactory(options), Guid.NewGuid(), new NoopAlertDelivery()),
            new EfMonitoringRunRepository(new TestDbContextFactory(options)),
            TimeProvider.System);

        var runs = await runService.GetRecentRunsAsync(2, CancellationToken.None);

        Assert.Equal(2, runs.Count);
        Assert.Equal(higherId, runs[0].Id);
        Assert.Equal(lowerId, runs[1].Id);
    }

    [Fact]
    public async Task MonitoringRunService_Honors_History_Limit_Of_One()
    {
        await using var db = await CreateMigratedContextAsync();
        var now = DateTimeOffset.UtcNow;
        var olderId = Guid.NewGuid();
        var newerId = Guid.NewGuid();
        db.Set<MonitoringRunRecord>().AddRange(
            new MonitoringRunRecord
            {
                Id = olderId,
                StartedAt = now.AddHours(3),
                CompletedAt = now.AddHours(3).AddMinutes(1),
                Status = MonitoringRunStatus.Completed.ToString(),
                PositionCount = 1,
                FailureCount = 0
            },
            new MonitoringRunRecord
            {
                Id = newerId,
                StartedAt = now.AddHours(4),
                CompletedAt = now.AddHours(4).AddMinutes(1),
                Status = MonitoringRunStatus.Completed.ToString(),
                PositionCount = 2,
                FailureCount = 0
            });
        await db.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var runService = new MonitoringRunService(
            new DurableRiskMonitor(new FakeMarketDataProvider(), new TestUnitOfWorkFactory(options), Guid.NewGuid(), new NoopAlertDelivery()),
            new EfMonitoringRunRepository(new TestDbContextFactory(options)),
            TimeProvider.System);

        var runs = await runService.GetRecentRunsAsync(1, CancellationToken.None);

        Assert.Single(runs);
        Assert.Equal(newerId, runs[0].Id);
    }

    [Fact]
    public async Task MonitoringRunService_RecoversOnlyStaleRunningRuns_AndIsIdempotent()
    {
        await using var db = await CreateMigratedContextAsync();
        var now = DateTimeOffset.UtcNow;
        var staleId = Guid.NewGuid();
        var freshId = Guid.NewGuid();
        db.Set<MonitoringRunRecord>().AddRange(
            new MonitoringRunRecord
            {
                Id = staleId,
                StartedAt = now.AddMinutes(-10),
                CompletedAt = null,
                Status = MonitoringRunStatus.Running.ToString(),
                PositionCount = 0,
                FailureCount = 0
            },
            new MonitoringRunRecord
            {
                Id = freshId,
                StartedAt = now.AddMinutes(-1),
                CompletedAt = null,
                Status = MonitoringRunStatus.Running.ToString(),
                PositionCount = 0,
                FailureCount = 0
            });
        await db.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var repository = new EfMonitoringRunRepository(new TestDbContextFactory(options));

        var recovered = await repository.RecoverStaleRunningAsync(now.AddMinutes(-5), now, CancellationToken.None);
        var recoveredAgain = await repository.RecoverStaleRunningAsync(now.AddMinutes(-5), now.AddMinutes(1), CancellationToken.None);

        Assert.Equal(1, recovered);
        Assert.Equal(0, recoveredAgain);
        await using var verificationDb = await CreateMigratedContextAsync();
        var stale = await verificationDb.Set<MonitoringRunRecord>().SingleAsync(x => x.Id == staleId);
        var fresh = await verificationDb.Set<MonitoringRunRecord>().SingleAsync(x => x.Id == freshId);
        Assert.Equal(MonitoringRunStatus.Failed.ToString(), stale.Status);
        Assert.Equal(1, stale.FailureCount);
        Assert.Null(fresh.CompletedAt);
        Assert.Equal(MonitoringRunStatus.Running.ToString(), fresh.Status);
    }

    [Fact]
    public async Task MonitoringRunService_Persists_Completed_Run_Around_Durable_Risk_Check()
    {
        await using var db = await CreateMigratedContextAsync();
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var repository = new EfMonitoringRunRepository(new TestDbContextFactory(options));
        var runId = Guid.NewGuid();
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await repository.AddAsync(new MonitoringRunState(runId, startedAt, null, MonitoringRunStatus.Running, 0, 0), CancellationToken.None);

        await repository.CompleteAsync(runId, DateTimeOffset.UtcNow, MonitoringRunStatus.Completed, 2, 0, CancellationToken.None);

        var persisted = await repository.GetAsync(runId, CancellationToken.None);
        Assert.NotNull(persisted);
        Assert.Equal(MonitoringRunStatus.Completed, persisted!.Status);
        Assert.Equal(2, persisted.PositionCount);
        Assert.Equal(0, persisted.FailureCount);
    }

    private static async Task<TradingDbContext> CreateMigratedContextAsync()
    {
        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var db = new TradingDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }

    private sealed class TestDbContextFactory(DbContextOptions<TradingDbContext> options) : IDbContextFactory<TradingDbContext>
    {
        public TradingDbContext CreateDbContext() => new(options);
        public Task<TradingDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(new TradingDbContext(options));
    }
}
