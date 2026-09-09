using AiTrading.Application;
using AiTrading.Domain;
using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Application.Tests;

public sealed class MonitoringResilienceTests
{
    [Fact]
    public async Task DurableRiskMonitor_Isolates_Provider_Failure_And_Continues_Other_Positions()
    {
        var connectionString = Environment.GetEnvironmentVariable("AI_TRADING_MYSQL_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("AI_TRADING_MYSQL_CONNECTION must be configured for MySQL integration tests.");

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(connectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        await using (var setup = new TradingDbContext(options))
        {
            await setup.Database.MigrateAsync();
            var portfolioId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            setup.Portfolios.Add(new PortfolioRecord { Id = portfolioId, Cash = 100_000m, UpdatedAt = now, Version = 1 });
            setup.Positions.AddRange(
                new PositionRecord { Id = Guid.NewGuid(), PortfolioId = portfolioId, Symbol = "FAIL", InstrumentToken = "1", Quantity = 1, AverageEntryPrice = 100m, CurrentMarketPrice = 100m, OpenedAt = now, UpdatedAt = now },
                new PositionRecord { Id = Guid.NewGuid(), PortfolioId = portfolioId, Symbol = "GOOD", InstrumentToken = "2", Quantity = 1, AverageEntryPrice = 100m, CurrentMarketPrice = 100m, OpenedAt = now, UpdatedAt = now });
            await setup.SaveChangesAsync();

            var failures = new RecordingFailureSink();
            var monitor = new DurableRiskMonitor(
                new FailingMarketDataProvider(),
                new TestUnitOfWorkFactory(options),
                portfolioId,
                new NoopAlertDelivery(),
                failures);

            await monitor.CheckOnceAsync(CancellationToken.None);

            var positions = await setup.Positions.AsNoTracking().ToListAsync();
            Assert.Equal(100m, positions.Single(x => x.Symbol == "FAIL").CurrentMarketPrice);
            Assert.Equal(101m, positions.Single(x => x.Symbol == "GOOD").CurrentMarketPrice);
            var failure = Assert.Single(failures.Items);
            Assert.Equal("FAIL", failure.Symbol);
            Assert.Equal("QUOTE_PROVIDER_FAILURE", failure.ErrorCode);
        }
    }

    private sealed class FailingMarketDataProvider : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken)
        {
            if (symbol.Value == "FAIL")
                throw new InvalidOperationException("simulated provider outage");
            return Task.FromResult(new MarketQuote(symbol, "NSE", symbol.InstrumentToken ?? "2", DateTimeOffset.UtcNow, 101m, 101m, 100m, 100m, 101m, 10, "integration"));
        }

        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Candle>>([]);
    }

    private sealed class RecordingFailureSink : IMonitoringFailureSink
    {
        public List<MonitoringFailure> Items { get; } = [];
        public void Record(MonitoringFailure failure) => Items.Add(failure);
    }

    private sealed class TestUnitOfWorkFactory(DbContextOptions<TradingDbContext> options) : ITradingUnitOfWorkFactory
    {
        public Task<ITradingUnitOfWork> CreateAsync(CancellationToken cancellationToken) => Task.FromResult<ITradingUnitOfWork>(new EfTradingUnitOfWork(new TradingDbContext(options)));
    }
}
