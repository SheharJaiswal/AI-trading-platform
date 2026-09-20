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

    public async Task AddAsync(PaperTradingSessionState session, CancellationToken ct)
    {
        ValidateState(session);
        db.Set<PaperTradingSessionRecord>().Add(ToRecord(session));
        await db.SaveChangesAsync(ct);
    }

    public async Task<PaperTradingSessionState?> GetAsync(Guid id, CancellationToken ct)
    {
        var record = await db.Set<PaperTradingSessionRecord>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        return record is null ? null : FromRecord(record);
    }

    public async Task UpdateAsync(PaperTradingSessionState session, CancellationToken ct)
    {
        ValidateState(session);
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
        if (r.Id == Guid.Empty)
            throw new InvalidOperationException($"Paper trading session has an invalid id; session state requires reconciliation.");

        if (r.CreatedAt == default || r.UpdatedAt < r.CreatedAt)
            throw new InvalidOperationException($"Paper trading session {r.Id} has invalid persisted timestamps; session state requires reconciliation.");

        if (!Enum.TryParse<PaperTradingSessionStatus>(r.Status, ignoreCase: true, out var status) || !Enum.IsDefined(status))
            throw new InvalidOperationException($"Paper trading session {r.Id} has an invalid persisted status; session state requires reconciliation.");

        List<SymbolDto> symbols;
        try
        {
            symbols = JsonSerializer.Deserialize<List<SymbolDto>>(r.SymbolsJson) ?? [];
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException($"Paper trading session {r.Id} has invalid persisted symbol data; session state requires reconciliation.", exception);
        }

        if (symbols.Count == 0 || symbols.Any(x => string.IsNullOrWhiteSpace(x.Value)))
            throw new InvalidOperationException($"Paper trading session {r.Id} has invalid persisted symbols; session state requires reconciliation.");

        if (string.IsNullOrWhiteSpace(r.Interval) || string.IsNullOrWhiteSpace(r.StrategyVersion) || r.StartingCash <= 0)
            throw new InvalidOperationException($"Paper trading session {r.Id} has invalid persisted configuration; session state requires reconciliation.");

        return new(r.Id,
            new PaperTradingSessionConfiguration(symbols.Select(x => new Symbol(x.Value, x.InstrumentToken)).ToArray(), r.Interval, r.StrategyVersion, r.StartingCash),
            status, r.CreatedAt, r.UpdatedAt);
    }

    private static void ValidateState(PaperTradingSessionState session)
    {
        if (session.Id == Guid.Empty)
            throw new ArgumentException("Paper trading session id must not be empty.", nameof(session));

        if (!Enum.IsDefined(session.Status))
            throw new ArgumentException("Paper trading session status is invalid.", nameof(session));

        if (session.CreatedAt == default || session.UpdatedAt < session.CreatedAt)
            throw new ArgumentException("Paper trading session timestamps are invalid.", nameof(session));

        if (session.Configuration is null || session.Configuration.Symbols is null || session.Configuration.Symbols.Count == 0)
            throw new ArgumentException("Paper trading session requires at least one symbol.", nameof(session));

        if (session.Configuration.Symbols.Any(x => string.IsNullOrWhiteSpace(x.Value)))
            throw new ArgumentException("Paper trading session symbols must not be empty.", nameof(session));

        if (string.IsNullOrWhiteSpace(session.Configuration.Interval))
            throw new ArgumentException("Paper trading session interval is required.", nameof(session));

        if (string.IsNullOrWhiteSpace(session.Configuration.StrategyVersion))
            throw new ArgumentException("Paper trading session strategy version is required.", nameof(session));

        if (session.Configuration.StartingCash <= 0)
            throw new ArgumentException("Paper trading session starting cash must be positive.", nameof(session));
    }
}
