using AiTrading.Domain;
using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Application.Tests;

public sealed class MySqlPersistenceIntegrationTests
{
    private static string? ConnectionString => Environment.GetEnvironmentVariable("AI_TRADING_MYSQL_CONNECTION");

    [Fact]
    public async Task MySql_Migration_Creates_Required_Schema_And_Enforces_Idempotency()
    {
        await using var db = await CreateMigratedContextAsync();

        var tables = await db.Database.SqlQueryRaw<string>(
            "SELECT TABLE_NAME AS Value FROM information_schema.tables WHERE table_schema = DATABASE() AND TABLE_NAME IN ('portfolios','orders','fills','positions','alerts','market_data_snapshots')")
            .ToListAsync();

        Assert.Equal(6, tables.Count);

        var key = $"integration-{Guid.NewGuid():N}";
        db.Orders.Add(CreateOrder(Guid.NewGuid(), key));
        await db.SaveChangesAsync();

        db.Orders.Add(CreateOrder(Guid.NewGuid(), key));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task MySql_Enforces_StopLoss_Alert_Uniqueness()
    {
        await using var db = await CreateMigratedContextAsync();
        var positionId = Guid.NewGuid();
        var bucket = "integration-bucket";

        db.Alerts.Add(CreateAlert(Guid.NewGuid(), positionId, bucket));
        await db.SaveChangesAsync();

        db.Alerts.Add(CreateAlert(Guid.NewGuid(), positionId, bucket));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task MySql_Persists_Latest_Market_Price_On_Position()
    {
        await using var db = await CreateMigratedContextAsync();
        var portfolioId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Portfolios.Add(new PortfolioRecord
        {
            Id = portfolioId,
            Cash = 100_000m,
            RealizedPnl = 0m,
            UpdatedAt = now,
            Version = 1
        });
        db.Positions.Add(new PositionRecord
        {
            Id = positionId,
            PortfolioId = portfolioId,
            Symbol = "TCS",
            InstrumentToken = "11536",
            Quantity = 10,
            AverageEntryPrice = 100m,
            CurrentMarketPrice = 105m,
            StopLoss = 95m,
            OpenedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();

        var position = await db.Positions.SingleAsync(x => x.Id == positionId);
        position.CurrentMarketPrice = 92.50m;
        position.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        var restored = await db.Positions.AsNoTracking().SingleAsync(x => x.Id == positionId);
        Assert.Equal(92.50m, restored.CurrentMarketPrice);
        Assert.Equal(95m, restored.StopLoss);
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

    private static OrderRecord CreateOrder(Guid id, string key) => new()
    {
        Id = id,
        IdempotencyKey = key,
        Symbol = "TCS",
        Side = "BUY",
        Quantity = 1,
        LimitPrice = 100m,
        StrategyVersion = "integration",
        CreatedAt = DateTimeOffset.UtcNow,
        ExecutionMode = "paper",
        Status = "created"
    };

    private static AlertRecord CreateAlert(Guid id, Guid positionId, string bucket) => new()
    {
        Id = id,
        PositionId = positionId,
        Symbol = "TCS",
        Rule = "STOP_LOSS",
        Severity = "High",
        Message = "integration stop loss",
        EvaluationBucket = bucket,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
