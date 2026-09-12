using AiTrading.Domain;

namespace AiTrading.Application;

public enum NewsSentiment
{
    Unknown,
    Positive,
    Neutral,
    Negative
}

public enum NewsMateriality
{
    Unknown,
    Low,
    Medium,
    High
}

public sealed record NewsItem(
    string Id,
    string Headline,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<Symbol> AffectedSymbols,
    NewsSentiment Sentiment,
    NewsMateriality Materiality,
    string Source);

public sealed record NewsMonitoringOptions(TimeSpan StaleAfter)
{
    public void Validate()
    {
        if (StaleAfter <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(StaleAfter));
    }
}

public enum NewsRecency
{
    Unknown,
    Current,
    Stale
}

public sealed record ClassifiedNews(
    NewsItem Item,
    NewsRecency Recency,
    bool IsActionableObservation);

public sealed class NewsClassifier(NewsMonitoringOptions options)
{
    public ClassifiedNews Classify(NewsItem item, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(item);
        options.Validate();

        var recency = item.PublishedAt is null
            ? NewsRecency.Unknown
            : item.PublishedAt.Value > now
                ? NewsRecency.Unknown
                : now - item.PublishedAt.Value <= options.StaleAfter
                    ? NewsRecency.Current
                    : NewsRecency.Stale;

        var actionable = recency == NewsRecency.Current &&
                         item.Materiality is NewsMateriality.Medium or NewsMateriality.High &&
                         item.AffectedSymbols.Count > 0;

        return new ClassifiedNews(item, recency, actionable);
    }
}
