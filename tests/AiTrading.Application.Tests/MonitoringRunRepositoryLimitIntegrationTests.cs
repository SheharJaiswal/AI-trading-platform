using AiTrading.Application;
using AiTrading.Domain;
using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Application.Tests;

public sealed class MonitoringRunRepositoryLimitIntegrationTests
{
    [Fact]
    public async Task MonitoringRunService_Clamps_History_Limit_To_One_Hundred()
    {
        var connectionString = Environment.GetEnvironmentVariable("AI_TRADING_MYSQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AI_TRADING_MYSQL_CONNECTION must be configured for MySQL integration tests.");

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(connectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;

        await using var db = new TradingDbContext(options);
        await db.Database.MigrateAsync();

        var startedAt = DateTimeOffset.UtcNow.AddYears(900);
        var records = Enumerable.Range(0, 101)
            .Select(index => new MonitoringRunRecord
            {
                Id = Guid.NewGuid(),
                StartedAt = startedAt.AddTicks(index),
                CompletedAt = startedAt.AddTicks(index).AddMinutes(1),
                Status = MonitoringRunStatus.Completed.ToString(),
                PositionCount = 0,
                FailureCount = 0
            })
            .ToArray();

        db.Set<MonitoringRunRecord>().AddRange(records);
        await db.SaveChangesAsync();

        var runService = new MonitoringRunService(
            new DurableRiskMonitor(new EmptyMarketDataProvider(), new TestUnitOfWorkFactory(options), Guid.NewGuid(), new NoopAlertDelivery()),
            new EfMonitoringRunRepository(new TestDbContextFactory(options)),
            TimeProvider.System);

        var runs = await runService.GetRecentRunsAsync(101, CancellationToken.None);

        Assert.Equal(100, runs.Count);
        Assert.DoesNotContain(runs, run => run.Id == records[0].Id);
        Assert.Equal(records[100].Id, runs[0].Id);
        Assert.Equal(records[1].Id, runs[^1].Id);
    }

    private sealed class EmptyMarketDataProvider : IMarketDataProvider
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
