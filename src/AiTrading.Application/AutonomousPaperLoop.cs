using System.Security.Cryptography;
using System.Text;
using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record AutonomousPaperLoopOptions(int Quantity, int MaxCandidates)
{
    public static AutonomousPaperLoopOptions Default => new(1, 1);
}

public sealed record AutonomousPaperLoopCandidate(Symbol Symbol, Recommendation Recommendation);
public sealed record AutonomousPaperLoopResult(Guid SessionId, DateTimeOffset CycleAt, string ExecutionMode, IReadOnlyList<AutonomousPaperLoopCandidate> Candidates, PaperTradingEventResponse? Execution, string? NoDecisionReason);

public sealed class AutonomousPaperLoopService(
    IPaperTradingSessionService sessions,
    RecommendationService recommendations,
    PaperTradingSessionExecutionService execution,
    AutonomousPaperLoopOptions? options = null)
{
    private readonly AutonomousPaperLoopOptions _options = options ?? AutonomousPaperLoopOptions.Default;

    public async Task<AutonomousPaperLoopResult> RunCycleAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (_options.Quantity <= 0) throw new ArgumentException("Autonomous loop quantity must be positive.", nameof(_options));
        if (_options.MaxCandidates <= 0) throw new ArgumentException("Autonomous loop max candidates must be positive.", nameof(_options));

        var session = await sessions.GetAsync(sessionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Paper trading session {sessionId} does not exist.");
        if (session.Status != PaperTradingSessionStatus.Running)
            throw new InvalidOperationException($"Autonomous cycle requires a running paper session; current state is {session.Status}.");

        var cycleAt = DateTimeOffset.UtcNow;
        var candidates = new List<AutonomousPaperLoopCandidate>();
        foreach (var symbol in session.Configuration.Symbols.Distinct())
        {
            var recommendation = await recommendations.GetRecommendationAsync(symbol, cancellationToken);
            candidates.Add(new AutonomousPaperLoopCandidate(symbol, recommendation));
        }

        var ranked = candidates
            .OrderByDescending(x => x.Recommendation.Action == RecommendationAction.Buy)
            .ThenByDescending(x => x.Recommendation.Confidence)
            .ThenByDescending(x => x.Recommendation.ExpectedReturn ?? decimal.MinValue)
            .ThenBy(x => x.Symbol.Value, StringComparer.Ordinal)
            .Take(_options.MaxCandidates)
            .ToArray();

        var selected = ranked.FirstOrDefault(x => x.Recommendation.Action == RecommendationAction.Buy);
        if (selected is null)
            return new(sessionId, cycleAt, "PAPER_ONLY", ranked, null,
                candidates.Any(x => x.Recommendation.Action == RecommendationAction.NoDecision) ? "NO_ACTIONABLE_OPPORTUNITY" : "NO_BUY_CANDIDATE");

        var eventId = BuildEventId(sessionId, selected.Symbol, cycleAt, session.Configuration.Interval);
        var response = await execution.ProcessAsync(new PaperTradingEventRequest(sessionId, selected.Symbol, _options.Quantity, eventId), cancellationToken);
        return new(sessionId, cycleAt, "PAPER_ONLY", ranked, response,
            response.Risk.Decision == RiskDecision.Approved ? null : response.Risk.Reason ?? response.Risk.Decision.ToString());
    }

    private static string BuildEventId(Guid sessionId, Symbol symbol, DateTimeOffset cycleAt, string interval)
    {
        var key = $"{sessionId:N}|{symbol.Value}|{cycleAt:yyyy-MM-ddTHH:mm}|{interval}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return $"auto-{Convert.ToHexString(hash.AsSpan(0, 16)).ToLowerInvariant()}";
    }
}
