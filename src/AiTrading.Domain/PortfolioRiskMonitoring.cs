namespace AiTrading.Domain;

public sealed record PortfolioRiskThresholds(
    decimal MaxPositionConcentration = 0.25m,
    decimal MaxGrossExposure = 1.00m,
    decimal MaxDrawdown = 0.20m)
{
    public void Validate()
    {
        if (MaxPositionConcentration <= 0m || MaxPositionConcentration > 1m)
            throw new ArgumentOutOfRangeException(nameof(MaxPositionConcentration));
        if (MaxGrossExposure <= 0m)
            throw new ArgumentOutOfRangeException(nameof(MaxGrossExposure));
        if (MaxDrawdown <= 0m || MaxDrawdown > 1m)
            throw new ArgumentOutOfRangeException(nameof(MaxDrawdown));
    }
}

public enum PortfolioRiskRule
{
    PositionConcentration,
    GrossExposure,
    Drawdown
}

public sealed record PortfolioRiskEvent(
    PortfolioRiskRule Rule,
    AlertSeverity Severity,
    decimal ObservedValue,
    decimal Threshold,
    string Message,
    DateTimeOffset CreatedAt,
    Symbol? Symbol);

public static class PortfolioRiskMonitor
{
    public static IReadOnlyList<PortfolioRiskEvent> Evaluate(
        Portfolio portfolio,
        decimal? peakEquity,
        IReadOnlyDictionary<Symbol, decimal>? prices = null,
        PortfolioRiskThresholds? thresholds = null,
        DateTimeOffset? evaluatedAt = null)
    {
        var effectiveThresholds = thresholds ?? new PortfolioRiskThresholds();
        effectiveThresholds.Validate();

        var equity = portfolio.Cash + portfolio.UnrealizedPnl;
        if (equity <= 0m)
            return [];

        var now = evaluatedAt ?? DateTimeOffset.UtcNow;
        var events = new List<PortfolioRiskEvent>();
        var exposures = portfolio.Positions
            .Select(position =>
            {
                var price = prices is not null && prices.TryGetValue(position.Symbol, out var marketPrice)
                    ? marketPrice
                    : position.AverageEntryPrice;
                return (position, exposure: Math.Abs(position.Quantity * price));
            })
            .ToArray();

        foreach (var item in exposures.Where(x => x.exposure / equity > effectiveThresholds.MaxPositionConcentration))
        {
            var observed = item.exposure / equity;
            events.Add(new PortfolioRiskEvent(
                PortfolioRiskRule.PositionConcentration,
                AlertSeverity.Warning,
                observed,
                effectiveThresholds.MaxPositionConcentration,
                $"Position concentration for {item.position.Symbol} exceeded the configured threshold.",
                now,
                item.position.Symbol));
        }

        var grossExposure = exposures.Sum(x => x.exposure) / equity;
        if (grossExposure > effectiveThresholds.MaxGrossExposure)
        {
            events.Add(new PortfolioRiskEvent(
                PortfolioRiskRule.GrossExposure,
                AlertSeverity.Warning,
                grossExposure,
                effectiveThresholds.MaxGrossExposure,
                "Gross portfolio exposure exceeded the configured threshold.",
                now,
                null));
        }

        if (peakEquity is > 0m)
        {
            var drawdown = Math.Max(0m, (peakEquity.Value - equity) / peakEquity.Value);
            if (drawdown > effectiveThresholds.MaxDrawdown)
            {
                events.Add(new PortfolioRiskEvent(
                    PortfolioRiskRule.Drawdown,
                    AlertSeverity.Warning,
                    drawdown,
                    effectiveThresholds.MaxDrawdown,
                    "Portfolio drawdown exceeded the configured threshold.",
                    now,
                    null));
            }
        }

        return events;
    }
}
