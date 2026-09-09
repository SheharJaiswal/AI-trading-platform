using AiTrading.Application;
using AiTrading.Domain;
using AiTrading.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Application.Tests;

public sealed class PaperTradingSessionPersistenceIntegrationTests
{
    private static string? ConnectionString => Environment.GetEnvironmentVariable("AI_TRADING_MYSQL_CONNECTION");

    [Fact]
    public async Task MySql_Persists_Session_Configuration_And_Lifecycle_State()
    {
        await using var db = await CreateMigratedContextAsync();
        var repository = new EfPaperTradingSessionRepository(db);
        var id = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var session = new PaperTradingSessionState(
            id,
            new PaperTradingSessionConfiguration(
                [new Symbol("TCS", "11536"), new Symbol("INFY", "1594")],
                "15m",
                "v5-deterministic",
                250_000m),
            PaperTradingSessionStatus.Draft,
            created,
            created);

        await repository.AddAsync(session, CancellationToken.None);
        var restored = await repository.GetAsync(id, CancellationToken.None);

        Assert.NotNull(restored);
        Assert.Equal(PaperTradingSessionStatus.Draft, restored!.Status);
        Assert.Equal("15m", restored.Configuration.Interval);
        Assert.Equal("v5-deterministic", restored.Configuration.StrategyVersion);
        Assert.Equal(250_000m, restored.Configuration.StartingCash);
        Assert.Equal(["TCS", "INFY"], restored.Configuration.Symbols.Select(x => x.Value).ToArray());
        Assert.Equal(["11536", "1594"], restored.Configuration.Symbols.Select(x => x.InstrumentToken ?? string.Empty).ToArray());

        var running = restored with
        {
            Status = PaperTradingSessionStatus.Running,
            UpdatedAt = created.AddMinutes(1)
        };
        await repository.UpdateAsync(running, CancellationToken.None);

        var updated = await repository.GetAsync(id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(PaperTradingSessionStatus.Running, updated!.Status);
        Assert.Equal(created.AddMinutes(1), updated.UpdatedAt);
    }

    [Fact]
    public async Task MySql_Enforces_Unique_Session_Event_Idempotency_Key()
    {
        await using var db = await CreateMigratedContextAsync();
        var sessionId = Guid.NewGuid();
        var eventId = $"integration-{Guid.NewGuid():N}";
        var orderId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        db.Set<PaperTradingSessionRecord>().Add(new PaperTradingSessionRecord
        {
            Id = sessionId,
            SymbolsJson = "[{\"Value\":\"TCS\",\"InstrumentToken\":\"11536\"}]",
            Interval = "15m",
            StrategyVersion = "v5-deterministic",
            StartingCash = 250_000m,
            Status = "Running",
            CreatedAt = now,
            UpdatedAt = now
        });
        db.PaperTradingEventAudits.Add(new PaperTradingEventAuditRecord
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            EventId = eventId,
            OrderId = orderId,
            Symbol = "TCS",
            Quantity = 1,
            RiskDecision = "Approved",
            RiskReason = "integration",
            FillPrice = 100m,
            CreatedAt = now
        });
        await db.SaveChangesAsync();

        db.PaperTradingEventAudits.Add(new PaperTradingEventAuditRecord
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            EventId = eventId,
            OrderId = Guid.NewGuid(),
            Symbol = "TCS",
            Quantity = 1,
            RiskDecision = "Approved",
            RiskReason = "duplicate",
            FillPrice = 100m,
            CreatedAt = now.AddSeconds(1)
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static async Task<TradingDbContext> CreateMigratedContextAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString))
            throw new InvalidOperationException("AI_TRADING_MYSQL_CONNECTION must be configured for MySQL integration tests.");

        var options = new DbContextOptionsBuilder<TradingDbContext>()
            .UseMySql(ConnectionString, ServerVersion.Parse("8.0.0-mysql"))
            .Options;
        var db = new TradingDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }
}
