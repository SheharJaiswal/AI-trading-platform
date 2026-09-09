using AiTrading.Domain;

namespace AiTrading.Application;

public enum PaperTradingSessionStatus
{
    Draft,
    Running,
    Paused,
    Stopped
}

public sealed record PaperTradingSessionConfiguration(
    IReadOnlyList<Symbol> Symbols,
    string Interval,
    string StrategyVersion,
    decimal StartingCash);

public sealed record PaperTradingSessionState(
    Guid Id,
    PaperTradingSessionConfiguration Configuration,
    PaperTradingSessionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public interface IPaperTradingSessionRepository
{
    Task AddAsync(PaperTradingSessionState session, CancellationToken cancellationToken);
    Task<PaperTradingSessionState?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(PaperTradingSessionState session, CancellationToken cancellationToken);
}

public sealed class PaperTradingSessionService(IPaperTradingSessionRepository repository)
{
    public async Task<PaperTradingSessionState> CreateAsync(PaperTradingSessionConfiguration configuration, CancellationToken ct)
    {
        ValidateConfiguration(configuration);
        var now = DateTimeOffset.UtcNow;
        var session = new PaperTradingSessionState(Guid.NewGuid(), configuration, PaperTradingSessionStatus.Draft, now, now);
        await repository.AddAsync(session, ct);
        return session;
    }

    public async Task<PaperTradingSessionState?> TransitionAsync(Guid id, PaperTradingSessionStatus target, CancellationToken ct)
    {
        var current = await repository.GetAsync(id, ct);
        if (current is null) return null;
        if (!IsAllowed(current.Status, target)) throw new InvalidOperationException($"Invalid paper session transition: {current.Status} -> {target}.");
        var updated = current with { Status = target, UpdatedAt = DateTimeOffset.UtcNow };
        await repository.UpdateAsync(updated, ct);
        return updated;
    }

    private static bool IsAllowed(PaperTradingSessionStatus from, PaperTradingSessionStatus to) =>
        (from, to) switch
        {
            (PaperTradingSessionStatus.Draft, PaperTradingSessionStatus.Running) => true,
            (PaperTradingSessionStatus.Running, PaperTradingSessionStatus.Paused) => true,
            (PaperTradingSessionStatus.Paused, PaperTradingSessionStatus.Running) => true,
            (PaperTradingSessionStatus.Running, PaperTradingSessionStatus.Stopped) => true,
            (PaperTradingSessionStatus.Paused, PaperTradingSessionStatus.Stopped) => true,
            _ => false
        };

    private static void ValidateConfiguration(PaperTradingSessionConfiguration configuration)
    {
        if (configuration.Symbols is null || configuration.Symbols.Count == 0) throw new ArgumentException("At least one symbol is required.", nameof(configuration));
        if (string.IsNullOrWhiteSpace(configuration.Interval)) throw new ArgumentException("Interval is required.", nameof(configuration));
        if (string.IsNullOrWhiteSpace(configuration.StrategyVersion)) throw new ArgumentException("Strategy version is required.", nameof(configuration));
        if (configuration.StartingCash <= 0) throw new ArgumentException("Starting cash must be positive.", nameof(configuration));
    }
}
