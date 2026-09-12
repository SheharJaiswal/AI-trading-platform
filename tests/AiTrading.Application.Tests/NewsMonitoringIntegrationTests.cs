using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class NewsMonitoringIntegrationTests
{
    [Fact]
    public async Task ConvertsActionableNewsToAdvisoryAlertsAndDeduplicates()
    {
        var symbol = new Symbol("ABC");
        var now = DateTimeOffset.UtcNow;
        var item = new NewsItem("n1", "Material update", now.AddMinutes(-5), new[] { symbol }, NewsSentiment.Positive, NewsMateriality.High, "test");
        var provider = new StubNewsProvider(new[] { item, item });
        var service = new NewsMonitoringService(provider, new NewsClassifier(new(TimeSpan.FromMinutes(30))));

        var result = await service.CheckAsync(new[] { symbol }, now.AddHours(-1), now, CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        var alert = Assert.Single(result.Alerts);
        Assert.Equal("NEWS:n1", alert.Key);
        Assert.Equal(AlertSeverity.High, alert.Severity);
        Assert.Equal(symbol, alert.Symbol);
        Assert.Null(result.ProviderFailure);
    }

    [Fact]
    public async Task DoesNotAlertForStaleNews()
    {
        var now = DateTimeOffset.UtcNow;
        var item = new NewsItem("n1", "Old update", now.AddHours(-2), new[] { new Symbol("ABC") }, NewsSentiment.Neutral, NewsMateriality.High, "test");
        var service = new NewsMonitoringService(new StubNewsProvider(new[] { item }), new NewsClassifier(new(TimeSpan.FromMinutes(30))));

        var result = await service.CheckAsync(Array.Empty<Symbol>(), now.AddHours(-3), now, CancellationToken.None);

        Assert.Empty(result.Alerts);
        Assert.Null(result.ProviderFailure);
    }

    [Fact]
    public async Task DoesNotAlertForFutureDatedNews()
    {
        var now = DateTimeOffset.UtcNow;
        var item = new NewsItem("n1", "Future update", now.AddMinutes(5), new[] { new Symbol("ABC") }, NewsSentiment.Negative, NewsMateriality.High, "test");
        var service = new NewsMonitoringService(new StubNewsProvider(new[] { item }), new NewsClassifier(new(TimeSpan.FromMinutes(30))));

        var result = await service.CheckAsync(Array.Empty<Symbol>(), now, now, CancellationToken.None);

        var observation = Assert.Single(result.Items);
        Assert.Equal(NewsRecency.Unknown, observation.Recency);
        Assert.False(observation.IsActionableObservation);
        Assert.Empty(result.Alerts);
        Assert.Null(result.ProviderFailure);
    }

    [Fact]
    public async Task IsolatesProviderFailure()
    {
        var service = new NewsMonitoringService(new FailingNewsProvider(), new NewsClassifier(new(TimeSpan.FromMinutes(30))));

        var result = await service.CheckAsync(Array.Empty<Symbol>(), DateTimeOffset.UtcNow.AddHours(-1), DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Empty(result.Alerts);
        Assert.Equal("provider unavailable", result.ProviderFailure);
    }

    private sealed class StubNewsProvider(IReadOnlyList<NewsItem> items) : IMarketNewsProvider
    {
        public Task<IReadOnlyList<NewsItem>> GetNewsAsync(IReadOnlyCollection<Symbol> symbols, DateTimeOffset since, CancellationToken cancellationToken)
            => Task.FromResult(items);
    }

    private sealed class FailingNewsProvider : IMarketNewsProvider
    {
        public Task<IReadOnlyList<NewsItem>> GetNewsAsync(IReadOnlyCollection<Symbol> symbols, DateTimeOffset since, CancellationToken cancellationToken)
            => throw new InvalidOperationException("provider unavailable");
    }
}
