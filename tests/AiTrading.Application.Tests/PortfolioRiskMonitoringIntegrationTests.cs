using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class PortfolioRiskMonitoringIntegrationTests
{
    [Fact]
    public void Unconfigured_observations_are_disabled()
    {
        var portfolio = new Portfolio(100m, [new Position(Guid.NewGuid(), new Symbol("ABC"), 2, 100m, null)], 0m, 0m);
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitoringOptions());

        var events = monitor.Evaluate(portfolio, new Dictionary<Symbol, decimal> { [new Symbol("ABC")] = 100m }, 0m);

        Assert.Empty(events);
    }

    [Fact]
    public void Configured_concentration_and_exposure_are_observed_without_execution()
    {
        var portfolio = new Portfolio(100m, [new Position(Guid.NewGuid(), new Symbol("ABC"), 2, 100m, null)], 0m, 0m);
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitoringOptions(
            MaxPositionWeight: 0.5m,
            MaxGrossExposureRatio: 0.5m));

        var events = monitor.Evaluate(portfolio, new Dictionary<Symbol, decimal> { [new Symbol("ABC")] = 100m }, 0m);

        Assert.Equal(2, events.Count);
        Assert.Contains(events, x => x.Type == "POSITION_CONCENTRATION" && x.Severity == AlertSeverity.Warning);
        Assert.Contains(events, x => x.Type == "GROSS_EXPOSURE" && x.Severity == AlertSeverity.High);
        Assert.All(events, x => Assert.DoesNotContain("execute", x.Message, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Drawdown_requires_a_persisted_peak_value()
    {
        var portfolio = new Portfolio(80m, [], 0m, 0m);
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitoringOptions(MaxDrawdownPercent: 10m));

        Assert.Empty(monitor.Evaluate(portfolio, new Dictionary<Symbol, decimal>(), 0m));
        var events = monitor.Evaluate(portfolio, new Dictionary<Symbol, decimal>(), 100m);

        var drawdown = Assert.Single(events);
        Assert.Equal("PORTFOLIO_DRAWDOWN", drawdown.Type);
        Assert.Equal(AlertSeverity.High, drawdown.Severity);
    }

    [Fact]
    public void Invalid_configuration_is_rejected_before_observation()
    {
        var monitor = new PortfolioRiskMonitor(new PortfolioRiskMonitoringOptions(MaxPositionWeight: 1.1m));
        var portfolio = new Portfolio(100m, [], 0m, 0m);

        Assert.Throws<ArgumentOutOfRangeException>(() => monitor.Evaluate(portfolio, new Dictionary<Symbol, decimal>(), 0m));
    }
}
