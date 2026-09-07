namespace AiTrading.Application;

public sealed record AiResearchRequest(string Symbol, string Question, IReadOnlyList<string> Evidence);
public sealed record AiResearchResult(string Provider, string Summary, IReadOnlyList<string> Risks, decimal Confidence, DateTimeOffset GeneratedAt);

public interface IAiProvider
{
    Task<AiResearchResult> ResearchAsync(AiResearchRequest request, CancellationToken cancellationToken);
}

public sealed class DisabledAiProvider : IAiProvider
{
    public Task<AiResearchResult> ResearchAsync(AiResearchRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(new AiResearchResult(
            "disabled",
            "AI research is not configured. The deterministic market-analysis pipeline remains available.",
            ["AI_PROVIDER_NOT_CONFIGURED"],
            0m,
            DateTimeOffset.UtcNow));
}
