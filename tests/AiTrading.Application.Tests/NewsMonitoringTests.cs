using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class NewsMonitoringTests
{
    [Fact]
    public void ClassifiesRecentMaterialNewsAsCurrentObservation()
    {
        var symbol = new Symbol("ABC");
        var now = DateTimeOffset.UtcNow;
        var item = new NewsItem("1", "Material update", now.AddMinutes(-5), new[] { symbol }, NewsSentiment.Positive, NewsMateriality.High, "test");

        var result = new NewsClassifier(new(TimeSpan.FromMinutes(30))).Classify(item, now);

        Assert.Equal(NewsRecency.Current, result.Recency);
        Assert.True(result.IsActionableObservation);
    }

    [Fact]
    public void ClassifiesOldNewsAsStale()
    {
        var now = DateTimeOffset.UtcNow;
        var item = new NewsItem("1", "Old update", now.AddHours(-2), Array.Empty<Symbol>(), NewsSentiment.Neutral, NewsMateriality.Low, "test");

        var result = new NewsClassifier(new(TimeSpan.FromMinutes(30))).Classify(item, now);

        Assert.Equal(NewsRecency.Stale, result.Recency);
        Assert.False(result.IsActionableObservation);
    }

    [Fact]
    public void MissingPublicationTimeIsUnknownAndNotActionable()
    {
        var symbol = new Symbol("ABC");
        var now = DateTimeOffset.UtcNow;
        var item = new NewsItem("1", "Unknown time", null, new[] { symbol }, NewsSentiment.Negative, NewsMateriality.High, "test");

        var result = new NewsClassifier(new(TimeSpan.FromMinutes(30))).Classify(item, now);

        Assert.Equal(NewsRecency.Unknown, result.Recency);
        Assert.False(result.IsActionableObservation);
    }

    [Fact]
    public void RejectsInvalidStaleWindow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NewsMonitoringOptions(TimeSpan.Zero).Validate());
    }
}
