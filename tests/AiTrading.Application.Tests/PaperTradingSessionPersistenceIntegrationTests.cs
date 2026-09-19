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
        var created = TruncateToMySqlMicroseconds(DateTimeOffset.UtcNow);
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
        var now = TruncateToMySqlMicroseconds(DateTimeOffset.UtcNow);

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

    [Fact]
    public async Task MySql_Duplicate_Session_Event_Audit_Commit_Is_Idempotent()
    {
        await using var seedDb = await CreateMigratedContextAsync();
        var sessionId = Guid.NewGuid();
        var eventId = $"atomic-{Guid.NewGuid():N}";
        var now = TruncateToMySqlMicroseconds(DateTimeOffset.UtcNow);
        seedDb.PaperTradingEventAudits.Add(new PaperTradingEventAuditRecord
        {
            Id = Guid.NewGuid(), SessionId = sessionId, EventId = eventId, OrderId = Guid.NewGuid(), Symbol = "TCS",
            Quantity = 1, RiskDecision = "Approved", RiskReason = "original", FillPrice = 100m, CreatedAt = now
        });
        await seedDb.SaveChangesAsync();

        await using (var duplicateDb = await CreateMigratedContextAsync())
        await using (var unitOfWork = new EfTradingUnitOfWork(duplicateDb))
        {
            await unitOfWork.PaperTradingEventAudits.AddAsync(new PaperTradingEventAuditState(
                Guid.NewGuid(), sessionId, eventId, Guid.NewGuid(), new Symbol("TCS"), 1, "Approved", "duplicate", 100m, now.AddSeconds(1)),
                CancellationToken.None);

            await unitOfWork.CommitAsync(CancellationToken.None);
        }

        await using var verifyDb = await CreateMigratedContextAsync();
        var events = await verifyDb.PaperTradingEventAudits.AsNoTracking().Where(x => x.SessionId == sessionId).ToListAsync();
        Assert.Single(events);
        Assert.Equal("original", events[0].RiskReason);
    }

    [Fact]
    public async Task MySql_Targeted_Session_Event_Lookup_Returns_Only_Requested_Event()
    {
        await using var db = await CreateMigratedContextAsync();
        var sessionId = Guid.NewGuid();
        var now = TruncateToMySqlMicroseconds(DateTimeOffset.UtcNow);
        db.PaperTradingEventAudits.AddRange(
            new PaperTradingEventAuditRecord { Id = Guid.NewGuid(), SessionId = sessionId, EventId = "evt-target", OrderId = Guid.NewGuid(), Symbol = "TCS", Quantity = 1, RiskDecision = "Approved", RiskReason = "target", FillPrice = 100m, CreatedAt = now },
            new PaperTradingEventAuditRecord { Id = Guid.NewGuid(), SessionId = sessionId, EventId = "evt-other", OrderId = Guid.NewGuid(), Symbol = "TCS", Quantity = 1, RiskDecision = "RiskBlocked", RiskReason = "other", FillPrice = null, CreatedAt = now.AddSeconds(1) });
        await db.SaveChangesAsync();

        var repository = new EfPaperTradingEventAuditRepository(db);
        var result = await repository.GetBySessionAndEventAsync(sessionId, "evt-target", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("evt-target", result!.EventId);
        Assert.Equal("target", result.RiskReason);
    }

    [Fact]
    public async Task MySql_Returns_Session_Events_In_Deterministic_Order_When_Timestamps_Tie()
    {
        await using var db = await CreateMigratedContextAsync();
        var sessionId = Guid.NewGuid();
        var now = TruncateToMySqlMicroseconds(DateTimeOffset.UtcNow);
        var repository = new EfPaperTradingEventAuditRepository(db);

        db.PaperTradingEventAudits.AddRange(
            new PaperTradingEventAuditRecord
            {
                Id = Guid.NewGuid(), SessionId = sessionId, EventId = "evt-b", OrderId = Guid.NewGuid(), Symbol = "TCS",
                Quantity = 1, RiskDecision = "Approved", RiskReason = "", FillPrice = 100m, CreatedAt = now
            },
            new PaperTradingEventAuditRecord
            {
                Id = Guid.NewGuid(), SessionId = sessionId, EventId = "evt-a", OrderId = Guid.NewGuid(), Symbol = "TCS",
                Quantity = 1, RiskDecision = "RiskBlocked", RiskReason = "test", FillPrice = null, CreatedAt = now
            });
        await db.SaveChangesAsync();

        var events = await repository.GetBySessionAsync(sessionId, CancellationToken.None);

        Assert.Equal(["evt-b", "evt-a"], events.Select(x => x.EventId).ToArray());
    }

    private static DateTimeOffset TruncateToMySqlMicroseconds(DateTimeOffset value)
        => new(value.Ticks - (value.Ticks % TimeSpan.TicksPerMicrosecond), value.Offset);

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
