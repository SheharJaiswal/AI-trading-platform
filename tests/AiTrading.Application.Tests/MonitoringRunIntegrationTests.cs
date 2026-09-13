using AiTrading.Application;
using AiTrading.Domain;
using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Application.Tests;

public sealed class MonitoringRunIntegrationTests
{
    private static string? ConnectionString => Environment.GetEnvironmentVariable("AI_TRADING_MYSQL_CONNECTION");

    [Fact]
    public async Task MonitoringRunService_Persists_Completed_Run_Around_Durable_Risk_Check()
    {
        await using var db = await CreateMigratedContextAsync();
        var portfolioId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Portfolios.Add(new PortfolioRecord
        {
            Id = portfolioId,
            Cash = 100_000m,
            RealizedPnl = 0m,
            UpdatedAt = now,
            Version = 1
        });
        await db.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString!, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var monitor = new DurableRiskMonitor(
            new FakeMarketDataProvider(),
            new TestUnitOfWorkFactory(options),
            portfolioId,
            new NoopAlertDelivery());
        var runService = new MonitoringRunService(
            monitor,
            new EfMonitoringRunRepository(new TestDbContextFactory(options)),
            TimeProvider.System);

        var result = await runService.RunOnceAsync(CancellationToken.None);

        await using var verify = new TradingDbContext(options);
        var record = await verify.Set<MonitoringRunRecord>().AsNoTracking().SingleAsync(x => x.Id == result.Id);
        Assert.Equal(MonitoringRunStatus.Completed.ToString(), record.Status);
        Assert.Equal(0, record.PositionCount);
        Assert.Equal(0, record.FailureCount);
        Assert.NotNull(record.CompletedAt);
        Assert.True(Math.Abs((result.CompletedAt - record.CompletedAt!.Value).TotalMilliseconds) < 1);
    }

    [Fact]
    public async Task MonitoringRunService_RecoversOnlyStaleRunningRuns()
    {
        await using var db = await CreateMigratedContextAsync();
        var staleId = Guid.NewGuid();
        var freshId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Set<MonitoringRunRecord>().AddRange(
            new MonitoringRunRecord
            {
                Id = staleId,
                StartedAt = now.AddMinutes(-10),
                Status = MonitoringRunStatus.Running.ToString(),
                PositionCount = 0,
                FailureCount = 0
            },
            new MonitoringRunRecord
            {
                Id = freshId,
                StartedAt = now.AddMinutes(-1),
                Status = MonitoringRunStatus.Running.ToString(),
                PositionCount = 0,
                FailureCount = 0
            });
        await db.SaveChangesAsync();

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString!, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var runService = new MonitoringRunService(
            new DurableRiskMonitor(new FakeMarketDataProvider(), new TestUnitOfWorkFactory(options), Guid.NewGuid(), new NoopAlertDelivery()),
            new EfMonitoringRunRepository(new TestDbContextFactory(options)),
            TimeProvider.System);

        var recovered = await runService.RecoverStaleRunsAsync(TimeSpan.FromMinutes(5), CancellationToken.None);

        Assert.Equal(1, recovered);
        await using var verify = new TradingDbContext(options);
        var stale = await verify.Set<MonitoringRunRecord>().AsNoTracking().SingleAsync(x => x.Id == staleId);
        var fresh = await verify.Set<MonitoringRunRecord>().AsNoTracking().SingleAsync(x => x.Id == freshId);
        Assert.Equal(MonitoringRunStatus.Failed.ToString(), stale.Status);
        Assert.Equal(1, stale.FailureCount);
        Assert.NotNull(stale.CompletedAt);
        Assert.Equal(MonitoringRunStatus.Running.ToString(), fresh.Status);
        Assert.Null(fresh.CompletedAt);
    }

    private static async Task<TradingDbContext> CreateMigratedContextAsync()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AI_TRADING_MYSQL_CONNECTION must be configured for MySQL integration tests.");

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(connectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var db = new TradingDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }

    private sealed class FakeMarketDataProvider : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("No quote should be requested for an empty portfolio.");

        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Candle>>([]);
    }

    private sealed class TestUnitOfWorkFactory(DbContextOptions<TradingDbContext> options) : ITradingUnitOfWorkFactory
    {
        public Task<ITradingUnitOfWork> CreateAsync(CancellationToken cancellationToken) =>
            Task.FromResult<ITradingUnitOfWork>(new EfTradingUnitOfWork(new TradingDbContext(options)));
    }

    private sealed class TestDbContextFactory(DbContextOptions<TradingDbContext> options) : IDbContextFactory<TradingDbContext>
    {
        public TradingDbContext CreateDbContext() => new(options);
        public Task<TradingDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new TradingDbContext(options));
    }
}
