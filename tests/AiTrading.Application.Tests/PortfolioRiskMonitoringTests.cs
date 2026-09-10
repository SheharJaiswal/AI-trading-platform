using AiTrading.Application;
using AiTrading.Domain;
using Xunit;

namespace AiTrading.Application.Tests;

public sealed class PortfolioRiskMonitoringTests
{
    [Fact]
    public void Flags_concentrated_position()
    {
        var symbol = new Symbol("AAA");
        var portfolio = new Portfolio(100m, [new Position(Guid.NewGuid(), symbol, 5, 100m, null)], 0m, 0m);
        var events = new PortfolioRiskMonitor(new(MaxPositionWeight: 0.25m)).Evaluate(portfolio, new Dictionary<Symbol, decimal> { [symbol] = 100m }, 1000m);
        Assert.Contains(events, x => x.Type == "POSITION_CONCENTRATION");
    }

    [Fact]
    public void Flags_drawdown_when_explicitly_configured()
    {
        var portfolio = new Portfolio(700m, [], 0m, 0m);
        var events = new PortfolioRiskMonitor(new(MaxDrawdownPercent: 20m)).Evaluate(portfolio, new Dictionary<Symbol, decimal>(), 1000m);
        Assert.Contains(events, x => x.Type == "PORTFOLIO_DRAWDOWN");
    }

    [Fact]
    public void Unconfigured_limits_do_not_create_risk_policy()
    {
        var symbol = new Symbol("AAA");
        var portfolio = new Portfolio(100m, [new Position(Guid.NewGuid(), symbol, 5, 100m, null)], 0m, 0m);
        var events = new PortfolioRiskMonitor(new()).Evaluate(portfolio, new Dictionary<Symbol, decimal> { [symbol] = 100m }, 1000m);
        Assert.Empty(events);
    }

    [Fact]
    public void Rejects_invalid_configured_limits()
    {
        var monitor = new PortfolioRiskMonitor(new(MaxPositionWeight: 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => monitor.Evaluate(new Portfolio(100m, [], 0m, 0m), new Dictionary<Symbol, decimal>(), 100m));
    }

    [Fact]
    public void Does_not_emit_drawdown_for_missing_peak_equity()
    {
        var portfolio = new Portfolio(100m, [], 0m, 0m);
        var events = new PortfolioRiskMonitor(new(MaxDrawdownPercent: 20m)).Evaluate(portfolio, new Dictionary<Symbol, decimal>(), 0m);
        Assert.Empty(events);
    }
}
