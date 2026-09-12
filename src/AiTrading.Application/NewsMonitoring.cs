namespace AiTrading.Application;

public enum NewsRecency { Unknown, Current, Stale }
public enum NewsSentiment { Unknown, Positive, Neutral, Negative }
public enum NewsMateriality { Unknown, Low, Medium, High }

public sealed record MarketNewsItem(string Id, string Headline, DateTimeOffset? PublishedAt, NewsSentiment Sentiment, NewsMateriality Materiality, IReadOnlyList<string> AffectedSymbols);
public sealed record NewsObservation(MarketNewsItem Item, NewsRecency Recency, bool Actionable);
public sealed record NewsMonitoringOptions(TimeSpan CurrentWindow)
{
    public static NewsMonitoringOptions Default { get; } = new(TimeSpan.FromHours(24));
}

public static class NewsObservationClassifier
{
    public static NewsObservation Classify(MarketNewsItem item, DateTimeOffset observedAt, NewsMonitoringOptions? options = null)
    {
        var window = (options ?? NewsMonitoringOptions.Default).CurrentWindow;
        var recency = item.PublishedAt switch
        {
            null => NewsRecency.Unknown,
            var publishedAt when publishedAt > observedAt => NewsRecency.Unknown,
            var publishedAt when observedAt - publishedAt <= window => NewsRecency.Current,
            _ => NewsRecency.Stale
        };
        var actionable = recency == NewsRecency.Current
            && item.Materiality is NewsMateriality.Medium or NewsMateriality.High
            && item.AffectedSymbols.Any()
            && item.Sentiment != NewsSentiment.Unknown;
        return new NewsObservation(item, recency, actionable);
    }
}

public interface IMarketNewsProvider
{
    Task<IReadOnlyList<MarketNewsItem>> GetLatestAsync(CancellationToken cancellationToken);
}

public sealed record NewsMonitoringResult(IReadOnlyList<NewsObservation> Observations, string? ErrorCode = null, string? ErrorMessage = null);
