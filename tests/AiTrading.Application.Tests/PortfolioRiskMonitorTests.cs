using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class PortfolioRiskMonitorTests
{
    [Fact]
    public void Flags_concentration_gross_exposure_and_drawdown()
    {
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitorOptions(0.25m, 1.0m, 0.10m));
        var events = monitor.Evaluate(100m, 120m,
            new Dictionary<string, decimal> { ["AAA"] = 60m, ["BBB"] = 60m }, DateTimeOffset.UtcNow);

        Assert.Contains(events, e => e.Type == PortfolioRiskEventType.PositionConcentration && e.Symbol == "AAA");
        Assert.Contains(events, e => e.Type == PortfolioRiskEventType.GrossExposure);
        Assert.Contains(events, e => e.Type == PortfolioRiskEventType.Drawdown);
    }

    [Fact]
    public void Does_not_flag_values_at_or_below_limits()
    {
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitorOptions(0.50m, 1.0m, 0.10m));
        var events = monitor.Evaluate(100m, 110m,
            new Dictionary<string, decimal> { ["AAA"] = 50m, ["BBB"] = 50m }, DateTimeOffset.UtcNow);

        Assert.Empty(events);
    }

    [Fact]
    public void Unconfigured_thresholds_produce_no_risk_events()
    {
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitorOptions());
        var events = monitor.Evaluate(100m, 120m,
            new Dictionary<string, decimal> { ["AAA"] = 60m, ["BBB"] = 60m }, DateTimeOffset.UtcNow);

        Assert.Empty(events);
    }

    [Fact]
    public void Handles_nonpositive_equity_safely()
    {
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitorOptions(0.25m, 1.0m, 0.10m));
        var events = monitor.Evaluate(0m, 120m,
            new Dictionary<string, decimal> { ["AAA"] = 60m }, DateTimeOffset.UtcNow);

        Assert.Empty(events);
    }

    [Fact]
    public void Handles_nonpositive_peak_equity_safely()
    {
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitorOptions(0.25m, 1.0m, 0.10m));
        var events = monitor.Evaluate(100m, 0m, new Dictionary<string, decimal>(), DateTimeOffset.UtcNow);

        Assert.Empty(events);
    }
}
