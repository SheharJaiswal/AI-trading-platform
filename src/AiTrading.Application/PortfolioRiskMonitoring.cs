using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record PortfolioRiskMonitoringOptions(decimal? MaxPositionWeight = null, decimal? MaxGrossExposureRatio = null, decimal? MaxDrawdownPercent = null)
{
    public void Validate()
    {
        if (MaxPositionWeight is <= 0m or > 1m) throw new ArgumentOutOfRangeException(nameof(MaxPositionWeight));
        if (MaxGrossExposureRatio is <= 0m) throw new ArgumentOutOfRangeException(nameof(MaxGrossExposureRatio));
        if (MaxDrawdownPercent is <= 0m or > 100m) throw new ArgumentOutOfRangeException(nameof(MaxDrawdownPercent));
    }
}

public sealed record PortfolioRiskEvent(string Type, AlertSeverity Severity, string Message, DateTimeOffset Timestamp, Symbol? Symbol, decimal? CurrentValue, decimal? LimitValue);

public sealed class PortfolioRiskMonitor(PortfolioRiskMonitoringOptions options)
{
    public IReadOnlyList<PortfolioRiskEvent> Evaluate(Portfolio portfolio, IReadOnlyDictionary<Symbol, decimal> prices, decimal peakEquity)
    {
        options.Validate();
        var equity = portfolio.Cash + portfolio.Positions.Sum(p => p.Quantity * (prices.TryGetValue(p.Symbol, out var price) ? price : p.AverageEntryPrice));
        if (equity <= 0m) return [];

        var events = new List<PortfolioRiskEvent>();
        var observedAt = DateTimeOffset.UtcNow;
        var gross = portfolio.Positions.Sum(p => Math.Abs(p.Quantity * (prices.TryGetValue(p.Symbol, out var price) ? price : p.AverageEntryPrice)));
        var grossRatio = gross / equity;

        if (options.MaxGrossExposureRatio is { } maxGross && grossRatio > maxGross)
            events.Add(new("GROSS_EXPOSURE", AlertSeverity.High, $"Gross portfolio exposure is {grossRatio:P2} of equity.", observedAt, null, grossRatio, maxGross));

        if (options.MaxPositionWeight is { } maxPositionWeight)
        {
            foreach (var position in portfolio.Positions)
            {
                var value = Math.Abs(position.Quantity * (prices.TryGetValue(position.Symbol, out var price) ? price : position.AverageEntryPrice));
                var weight = value / equity;
                if (weight > maxPositionWeight)
                    events.Add(new("POSITION_CONCENTRATION", AlertSeverity.Warning, $"Position {position.Symbol} is {weight:P2} of portfolio equity.", observedAt, position.Symbol, weight, maxPositionWeight));
            }
        }

        if (options.MaxDrawdownPercent is { } maxDrawdown && peakEquity > 0m)
        {
            var drawdown = Math.Max(0m, ((peakEquity - equity) / peakEquity) * 100m);
            if (drawdown >= maxDrawdown)
                events.Add(new("PORTFOLIO_DRAWDOWN", AlertSeverity.High, $"Portfolio drawdown is {drawdown:F2}%.", observedAt, null, drawdown, maxDrawdown));
        }

        return events;
    }
}
