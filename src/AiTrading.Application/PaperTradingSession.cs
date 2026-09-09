using System.Collections.Concurrent;
using AiTrading.Domain;

namespace AiTrading.Application;

public enum PaperTradingSessionStatus { Draft, Running, Paused, Stopped }
public sealed record PaperTradingSessionConfiguration(IReadOnlyList<Symbol> Symbols,string Interval,string StrategyVersion,decimal StartingCash);
public sealed record PaperTradingSessionState(Guid Id,PaperTradingSessionConfiguration Configuration,PaperTradingSessionStatus Status,DateTimeOffset CreatedAt,DateTimeOffset UpdatedAt);

public interface IPaperTradingSessionRepository { Task AddAsync(PaperTradingSessionState session,CancellationToken cancellationToken); Task<PaperTradingSessionState?> GetAsync(Guid id,CancellationToken cancellationToken); Task UpdateAsync(PaperTradingSessionState session,CancellationToken cancellationToken); }
public interface IPaperTradingSessionService { Task<PaperTradingSessionState> CreateAsync(PaperTradingSessionConfiguration configuration,CancellationToken ct); Task<PaperTradingSessionState?> GetAsync(Guid id,CancellationToken ct); Task<PaperTradingSessionState?> TransitionAsync(Guid id,PaperTradingSessionStatus target,CancellationToken ct); }

public sealed class PaperTradingSessionService(IPaperTradingSessionRepository repository):IPaperTradingSessionService
{
 public async Task<PaperTradingSessionState>CreateAsync(PaperTradingSessionConfiguration configuration,CancellationToken ct){ValidateConfiguration(configuration);var now=DateTimeOffset.UtcNow;var session=new PaperTradingSessionState(Guid.NewGuid(),configuration,PaperTradingSessionStatus.Draft,now,now);await repository.AddAsync(session,ct);return session;}
 public Task<PaperTradingSessionState?>GetAsync(Guid id,CancellationToken ct)=>repository.GetAsync(id,ct);
 public async Task<PaperTradingSessionState?>TransitionAsync(Guid id,PaperTradingSessionStatus target,CancellationToken ct){var current=await repository.GetAsync(id,ct);if(current is null)return null;if(!IsAllowed(current.Status,target))throw new InvalidOperationException($"Invalid paper session transition: {current.Status} -> {target}.");var updated=current with{Status=target,UpdatedAt=DateTimeOffset.UtcNow};await repository.UpdateAsync(updated,ct);return updated;}
 private static bool IsAllowed(PaperTradingSessionStatus from,PaperTradingSessionStatus to)=>(from,to) switch{(PaperTradingSessionStatus.Draft,PaperTradingSessionStatus.Running)=>true,(PaperTradingSessionStatus.Running,PaperTradingSessionStatus.Paused)=>true,(PaperTradingSessionStatus.Paused,PaperTradingSessionStatus.Running)=>true,(PaperTradingSessionStatus.Running,PaperTradingSessionStatus.Stopped)=>true,(PaperTradingSessionStatus.Paused,PaperTradingSessionStatus.Stopped)=>true,_=>false};
 private static void ValidateConfiguration(PaperTradingSessionConfiguration c){if(c.Symbols is null||c.Symbols.Count==0)throw new ArgumentException("At least one symbol is required.",nameof(c));if(string.IsNullOrWhiteSpace(c.Interval))throw new ArgumentException("Interval is required.",nameof(c));if(string.IsNullOrWhiteSpace(c.StrategyVersion))throw new ArgumentException("Strategy version is required.",nameof(c));if(c.StartingCash<=0)throw new ArgumentException("Starting cash must be positive.",nameof(c));if(c.Symbols.Any(x=>string.IsNullOrWhiteSpace(x.Value)))throw new ArgumentException("All symbols must be non-empty.",nameof(c));}
}

public sealed class InMemoryPaperTradingSessionRepository:IPaperTradingSessionRepository
{
 private readonly ConcurrentDictionary<Guid,PaperTradingSessionState> sessions=new();
 public Task AddAsync(PaperTradingSessionState s,CancellationToken ct){if(!sessions.TryAdd(s.Id,s))throw new InvalidOperationException("Paper trading session already exists.");return Task.CompletedTask;}
 public Task<PaperTradingSessionState?>GetAsync(Guid id,CancellationToken ct){sessions.TryGetValue(id,out var s);return Task.FromResult(s);}
 public Task UpdateAsync(PaperTradingSessionState s,CancellationToken ct){if(!sessions.TryUpdate(s.Id,s,sessions.GetValueOrDefault(s.Id)??throw new InvalidOperationException($"Paper trading session {s.Id} does not exist.")))throw new InvalidOperationException($"Paper trading session {s.Id} update conflict.");return Task.CompletedTask;}
}
