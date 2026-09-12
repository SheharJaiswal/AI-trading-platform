using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class NewsObservationClassifierTests
{
    private static readonly DateTimeOffset ObservedAt = new(2026, 9, 13, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Classifies_item_at_current_window_boundary_as_current()
    {
        var item = Item(ObservedAt.AddHours(-24));

        var observation = NewsObservationClassifier.Classify(item, ObservedAt);

        Assert.Equal(NewsRecency.Current, observation.Recency);
        Assert.True(observation.Actionable);
    }

    [Fact]
    public void Classifies_item_older_than_configured_window_as_stale()
    {
        var item = Item(ObservedAt.AddMinutes(-61));

        var observation = NewsObservationClassifier.Classify(
            item,
            ObservedAt,
            new NewsMonitoringOptions(TimeSpan.FromHours(1)));

        Assert.Equal(NewsRecency.Stale, observation.Recency);
        Assert.False(observation.Actionable);
    }

    [Fact]
    public void Missing_publication_time_is_unknown_and_non_actionable()
    {
        var item = Item(null) with { Sentiment = NewsSentiment.Positive };

        var observation = NewsObservationClassifier.Classify(item, ObservedAt);

        Assert.Equal(NewsRecency.Unknown, observation.Recency);
        Assert.False(observation.Actionable);
    }

    [Fact]
    public void Missing_required_metadata_is_non_actionable()
    {
        var item = Item(ObservedAt.AddMinutes(-5)) with
        {
            Sentiment = NewsSentiment.Unknown,
            Materiality = NewsMateriality.High,
            AffectedSymbols = []
        };

        var observation = NewsObservationClassifier.Classify(item, ObservedAt);

        Assert.Equal(NewsRecency.Current, observation.Recency);
        Assert.False(observation.Actionable);
    }

    private static MarketNewsItem Item(DateTimeOffset? publishedAt) => new(
        "news-1",
        "Example market headline",
        publishedAt,
        NewsSentiment.Negative,
        NewsMateriality.High,
        ["TCS"]);
}
