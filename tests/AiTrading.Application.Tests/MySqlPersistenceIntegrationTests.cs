using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Application.Tests;

public sealed class MySqlPersistenceIntegrationTests
{
    private static string? ConnectionString => Environment.GetEnvironmentVariable("AI_TRADING_MYSQL_CONNECTION");

    [Fact]
    public async Task MySql_Migration_Creates_Required_Schema_And_Enforces_Idempotency()
    {
        var connectionString = ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("AI_TRADING_MYSQL_CONNECTION must be configured for MySQL integration tests.");
        }

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(connectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;

        await using var db = new TradingDbContext(options);
        await db.Database.MigrateAsync();

        var tables = await db.Database.SqlQueryRaw<string>(
            "SELECT TABLE_NAME AS Value FROM information_schema.tables WHERE table_schema = DATABASE() AND TABLE_NAME IN ('portfolios','orders','fills','positions','alerts','market_data_snapshots')")
            .ToListAsync();

        Assert.Equal(6, tables.Count);

        var key = $"integration-{Guid.NewGuid():N}";
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        db.Orders.Add(new OrderRecord
        {
            Id = firstId,
            IdempotencyKey = key,
            Symbol = "TCS",
            Side = "BUY",
            Quantity = 1,
            LimitPrice = 100m,
            StrategyVersion = "integration",
            CreatedAt = DateTimeOffset.UtcNow,
            ExecutionMode = "paper",
            Status = "created"
        });
        await db.SaveChangesAsync();

        db.Orders.Add(new OrderRecord
        {
            Id = secondId,
            IdempotencyKey = key,
            Symbol = "TCS",
            Side = "BUY",
            Quantity = 1,
            LimitPrice = 100m,
            StrategyVersion = "integration",
            CreatedAt = DateTimeOffset.UtcNow,
            ExecutionMode = "paper",
            Status = "created"
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
