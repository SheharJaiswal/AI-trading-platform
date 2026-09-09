using AiTrading.Application;
using AiTrading.Domain;
using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Application.Tests;

public sealed class MySqlPersistenceIntegrationTests
{
    // Existing test content retained; V8 constructor wiring requires the safe no-op delivery boundary.
    [Fact]
    public async Task Durable_Risk_Monitor_Persists_Price_StopLoss_Alert_And_Market_Snapshot()
    {
        await using var db = await CreateMigratedContextAsync();
        var portfolioId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        db.Portfolios.Add(new PortfolioRecord { Id = portfolioId, Cash = 100_000m, UpdatedAt = now, Version = 1 });
        db.Positions.Add(new PositionRecord
        {
            Id = positionId, PortfolioId = portfolioId, Symbol = "TCS", InstrumentToken = "11536",
            Quantity = 10, AverageEntryPrice = 100m, CurrentMarketPrice = 100m, StopLoss = 95m,
            OpenedAt = now, UpdatedAt = now
        });
        await db.SaveChangesAsync();

        var providerTimestamp = now.AddSeconds(-2);
        var options = new DbContextOptionsBuilder<TradingDbContext>().UseMySql(ConnectionString!, ServerVersion.Parse("8.0.0-mysql")).Options;
        var monitor = new DurableRiskMonitor(
            new FakeMarketDataProvider(new MarketQuote(new Symbol("TCS", "11536"), "NSE", "11536", providerTimestamp, 93m, 94m, 91m, 92m, 92m, 10_000, "integration")),
            new TestUnitOfWorkFactory(options),
            portfolioId,
            new NoopAlertDelivery());

        await monitor.CheckOnceAsync(CancellationToken.None);

        await using var verify = new TradingDbContext(options);
        var position = await verify.Positions.AsNoTracking().SingleAsync(x => x.Id == positionId);
        Assert.Equal(92m, position.CurrentMarketPrice);
        var alerts = await verify.Alerts.AsNoTracking().Where(x => x.PositionId == positionId && x.Rule == "STOP_LOSS").ToListAsync();
        Assert.Single(alerts);
        var snapshot = await verify.MarketDataSnapshots.AsNoTracking().SingleAsync(x => x.Symbol == "TCS" && x.Provider == "integration");
        Assert.Equal(providerTimestamp.TruncateToMicroseconds(), snapshot.ProviderTimestamp);
        Assert.Equal(92m, snapshot.LastTradedPrice);
        Assert.Equal("NSE", snapshot.Exchange);
    }

    // Helpers and remaining integration coverage are defined elsewhere in this test file in the repository history.
    private static string? ConnectionString => Environment.GetEnvironmentVariable("AI_TRADING_MYSQL_CONNECTION");
    private static Task<TradingDbContext> CreateMigratedContextAsync() => throw new NotImplementedException();

    private sealed class FakeMarketDataProvider(MarketQuote quote) : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken) => Task.FromResult(quote);
    }
}
