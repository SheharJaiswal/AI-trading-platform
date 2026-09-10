using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record PortfolioRiskMonitoringOptions(decimal MaxPositionWeight = 0.25m, decimal MaxGrossExposureRatio = 1.0m, decimal MaxDrawdownPercent = 20m)
{
    public void Validate()
    {
        if (MaxPositionWeight <= 0m || MaxPositionWeight > 1m) throw new ArgumentOutOfRangeException(nameof(MaxPositionWeight));
        if (MaxGrossExposureRatio <= 0m) throw new ArgumentOutOfRangeException(nameof(MaxGrossExposureRatio));
        if (MaxDrawdownPercent <= 0m || MaxDrawdownPercent > 100m) throw new ArgumentOutOfRangeException(nameof(MaxDrawdownPercent));
    }
}

public sealed record PortfolioRiskEvent(string Type, AlertSeverity Severity, string Message, DateTimeOffset Timestamp, Symbol? Symbol, decimal? CurrentValue, decimal? LimitValue);

public sealed class PortfolioRiskMonitor(PortfolioRiskMonitoringOptions options)
{
    public IReadOnlyList<PortfolioRiskEvent> Evaluate(Portfolio portfolio, IReadOnlyDictionary<Symbol, decimal> prices, decimal peakEquity)
    {
        options.Validate();
        if (peakEquity <= 0m) return [];
        var equity = portfolio.Cash + portfolio.Positions.Sum(p => p.Quantity * (prices.TryGetValue(p.Symbol, out var price) ? price : p.AverageEntryPrice));
        if (equity <= 0m) return [];
        var events = new List<PortfolioRiskEvent>();
        var gross = portfolio.Positions.Sum(p => Math.Abs(p.Quantity * (prices.TryGetValue(p.Symbol, out var price) ? price : p.AverageEntryPrice)));
        var grossRatio = gross / equity;
        if (grossRatio > options.MaxGrossExposureRatio)
            events.Add(new("GROSS_EXPOSURE", AlertSeverity.High, $"Gross portfolio exposure is {grossRatio:P2} of equity.", DateTimeOffset.UtcNow, null, grossRatio, options.MaxGrossExposureRatio));
        foreach (var position in portfolio.Positions)
        {
            var value = Math.Abs(position.Quantity * (prices.TryGetValue(position.Symbol, out var price) ? price : position.AverageEntryPrice));
            var weight = value / equity;
            if (weight > options.MaxPositionWeight)
                events.Add(new("POSITION_CONCENTRATION", AlertSeverity.Warning, $"Position {position.Symbol} is {weight:P2} of portfolio equity.", DateTimeOffset.UtcNow, position.Symbol, weight, options.MaxPositionWeight));
        }
        var drawdown = ((peakEquity - equity) / peakEquity) * 100m;
        if (drawdown >= options.MaxDrawdownPercent)
            events.Add(new("PORTFOLIO_DRAWDOWN", drawdown >= options.MaxDrawdownPercent * 1.5m ? AlertSeverity.Critical : AlertSeverity.High, $"Portfolio drawdown is {drawdown:F2}%.", DateTimeOffset.UtcNow, null, drawdown, options.MaxDrawdownPercent));
        return events;
    }
}
