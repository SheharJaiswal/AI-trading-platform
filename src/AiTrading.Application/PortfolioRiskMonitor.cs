namespace AiTrading.Application;

public sealed record PortfolioRiskMonitorOptions(
    decimal MaxPositionWeight = 0.25m,
    decimal MaxGrossExposure = 1.00m,
    decimal MaxDrawdown = 0.10m);

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
        var grossExposure = positionMarketValues.Values.Sum(Math.Abs);
        var grossRatio = grossExposure / portfolioEquity;

        if (grossRatio > options.MaxGrossExposure)
            events.Add(new(PortfolioRiskEventType.GrossExposure, null, grossRatio, options.MaxGrossExposure,
                $"Gross exposure {grossRatio:P2} exceeds configured limit {options.MaxGrossExposure:P2}.", observedAt));

        foreach (var position in positionMarketValues)
        {
            var weight = Math.Abs(position.Value) / portfolioEquity;
            if (weight > options.MaxPositionWeight)
                events.Add(new(PortfolioRiskEventType.PositionConcentration, position.Key, weight, options.MaxPositionWeight,
                    $"Position {position.Key} weight {weight:P2} exceeds configured limit {options.MaxPositionWeight:P2}.", observedAt));
        }

        var drawdown = Math.Max(0m, (peakEquity - portfolioEquity) / peakEquity);
        if (drawdown > options.MaxDrawdown)
            events.Add(new(PortfolioRiskEventType.Drawdown, null, drawdown, options.MaxDrawdown,
                $"Portfolio drawdown {drawdown:P2} exceeds configured limit {options.MaxDrawdown:P2}.", observedAt));

        return events;
    }
}
