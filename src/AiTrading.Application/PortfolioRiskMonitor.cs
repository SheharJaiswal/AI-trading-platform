namespace AiTrading.Application;

public sealed record PortfolioRiskMonitorOptions(
    decimal? MaxPositionWeight = null,
    decimal? MaxGrossExposure = null,
    decimal? MaxDrawdown = null);

public enum PortfolioRiskEventType
{
    PositionConcentration,
    GrossExposure,
    Drawdown
}

public sealed record PortfolioRiskEvent(
    PortfolioRiskEventType Type,
    string? Symbol,
    decimal ObservedValue,
    decimal Threshold,
    string Message,
    DateTimeOffset ObservedAt);

public interface IPortfolioRiskMonitor
{
    IReadOnlyList<PortfolioRiskEvent> Evaluate(
        decimal portfolioEquity,
        decimal peakEquity,
        IReadOnlyDictionary<string, decimal> positionMarketValues,
        DateTimeOffset observedAt);
}

public sealed class PortfolioRiskMonitor(PortfolioRiskMonitorOptions options) : IPortfolioRiskMonitor
{
    public IReadOnlyList<PortfolioRiskEvent> Evaluate(
        decimal portfolioEquity,
        decimal peakEquity,
        IReadOnlyDictionary<string, decimal> positionMarketValues,
        DateTimeOffset observedAt)
    {
        if (portfolioEquity <= 0m) return [];
        if (peakEquity <= 0m) peakEquity = portfolioEquity;

        var events = new List<PortfolioRiskEvent>();
        var grossExposure = positionMarketValues.Values.Sum(value => Math.Abs(value));
        var grossRatio = grossExposure / portfolioEquity;

        if (options.MaxGrossExposure is { } maxGrossExposure && grossRatio > maxGrossExposure)
            events.Add(new(PortfolioRiskEventType.GrossExposure, null, grossRatio, maxGrossExposure,
                $"Gross exposure {grossRatio:P2} exceeds configured limit {maxGrossExposure:P2}.", observedAt));

        if (options.MaxPositionWeight is { } maxPositionWeight)
        {
            foreach (var position in positionMarketValues)
            {
                var weight = Math.Abs(position.Value) / portfolioEquity;
                if (weight > maxPositionWeight)
                    events.Add(new(PortfolioRiskEventType.PositionConcentration, position.Key, weight, maxPositionWeight,
                        $"Position {position.Key} weight {weight:P2} exceeds configured limit {maxPositionWeight:P2}.", observedAt));
            }
        }

        if (options.MaxDrawdown is { } maxDrawdown)
        {
            var drawdown = Math.Max(0m, (peakEquity - portfolioEquity) / peakEquity);
            if (drawdown > maxDrawdown)
                events.Add(new(PortfolioRiskEventType.Drawdown, null, drawdown, maxDrawdown,
                    $"Portfolio drawdown {drawdown:P2} exceeds configured limit {maxDrawdown:P2}.", observedAt));
        }

        return events;
    }
}
