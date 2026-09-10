using AiTrading.Domain;

namespace AiTrading.Application;

public interface IMarketNewsProvider
{
    Task<IReadOnlyList<NewsItem>> GetNewsAsync(IReadOnlyCollection<Symbol> symbols, DateTimeOffset since, CancellationToken cancellationToken);
}

public sealed record NewsMonitoringResult(
    IReadOnlyList<ClassifiedNews> Items,
    IReadOnlyList<Alert> Alerts,
    string? ProviderFailure);

public sealed class NewsMonitoringService(
    IMarketNewsProvider provider,
    NewsClassifier classifier)
{
    public async Task<NewsMonitoringResult> CheckAsync(
        IReadOnlyCollection<Symbol> symbols,
        DateTimeOffset since,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<NewsItem> items;
        try
        {
            items = await provider.GetNewsAsync(symbols, since, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new NewsMonitoringResult(Array.Empty<ClassifiedNews>(), Array.Empty<Alert>(), ex.Message);
        }

        var classified = new List<ClassifiedNews>();
        var alerts = new List<Alert>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var item in items)
        {
            var result = classifier.Classify(item, now);
            classified.Add(result);
            if (!result.IsActionableObservation || !seen.Add(item.Id))
                continue;

            var severity = item.Materiality == NewsMateriality.High
                ? AlertSeverity.High
                : AlertSeverity.Warning;
            var symbol = item.AffectedSymbols.FirstOrDefault();
            alerts.Add(new Alert(
                $"NEWS:{item.Id}",
                severity,
                $"Material news: {item.Headline}",
                now,
                symbol));
        }

        return new NewsMonitoringResult(classified, alerts, null);
    }
}
