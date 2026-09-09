using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using AiTrading.Application;
using AiTrading.Domain;
using Microsoft.EntityFrameworkCore;

namespace AiTrading.Infrastructure.Persistence;

[Table("paper_trading_sessions")]
public sealed class PaperTradingSessionRecord
{
    [Key] public Guid Id { get; set; }
    [Required] public string SymbolsJson { get; set; } = "[]";
    [Required, MaxLength(16)] public string Interval { get; set; } = "";
    [Required, MaxLength(64)] public string StrategyVersion { get; set; } = "";
    public decimal StartingCash { get; set; }
    [Required, MaxLength(16)] public string Status { get; set; } = "Draft";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class EfPaperTradingSessionRepository(TradingDbContext db) : IPaperTradingSessionRepository
{
    private sealed record SymbolDto(string Value, string? InstrumentToken);

    public Task AddAsync(PaperTradingSessionState session, CancellationToken ct)
    {
        db.Set<PaperTradingSessionRecord>().Add(ToRecord(session));
        return Task.CompletedTask;
    }

    public async Task<PaperTradingSessionState?> GetAsync(Guid id, CancellationToken ct)
    {
        var record = await db.Set<PaperTradingSessionRecord>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return record is null ? null : FromRecord(record);
    }

    public async Task UpdateAsync(PaperTradingSessionState session, CancellationToken ct)
    {
        var record = await db.Set<PaperTradingSessionRecord>().SingleOrDefaultAsync(x => x.Id == session.Id, ct)
            ?? throw new InvalidOperationException($"Paper trading session {session.Id} does not exist.");
        record.Status = session.Status.ToString();
        record.UpdatedAt = session.UpdatedAt;
        await db.SaveChangesAsync(ct);
    }

    private static PaperTradingSessionRecord ToRecord(PaperTradingSessionState s) => new()
    {
        Id = s.Id,
        SymbolsJson = JsonSerializer.Serialize(s.Configuration.Symbols.Select(x => new SymbolDto(x.Value, x.InstrumentToken))),
        Interval = s.Configuration.Interval,
        StrategyVersion = s.Configuration.StrategyVersion,
        StartingCash = s.Configuration.StartingCash,
        Status = s.Status.ToString(),
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static PaperTradingSessionState FromRecord(PaperTradingSessionRecord r)
    {
        var symbols = JsonSerializer.Deserialize<List<SymbolDto>>(r.SymbolsJson) ?? [];
        return new(r.Id,
            new PaperTradingSessionConfiguration(symbols.Select(x => new Symbol(x.Value, x.InstrumentToken)).ToArray(), r.Interval, r.StrategyVersion, r.StartingCash),
            Enum.Parse<PaperTradingSessionStatus>(r.Status, true), r.CreatedAt, r.UpdatedAt);
    }
}
