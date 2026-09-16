using AiTrading.Domain;
using Xunit;

namespace AiTrading.Domain.Tests;

public sealed class PortfolioRiskMonitoringTests
{
    [Fact]
    public void Evaluate_ShouldRaiseConcentrationAndGrossExposureEvents()
    {
        var symbol = new Symbol("ABC");
        var portfolio = new Portfolio(100m, [new Position(Guid.NewGuid(), symbol, 3, 20m, null)], 0m, 0m);

        var events = PortfolioRiskMonitor.Evaluate(portfolio, 100m, new Dictionary<Symbol, decimal> { [symbol] = 40m });

        Assert.Contains(events, x => x.Rule == PortfolioRiskRule.PositionConcentration);
        Assert.Contains(events, x => x.Rule == PortfolioRiskRule.GrossExposure);
        Assert.All(events, x => Assert.Equal(AlertSeverity.Warning, x.Severity));
    }

    [Fact]
    public void Evaluate_ShouldUseEntryPriceWhenMarketPriceIsMissing()
    {
        var symbol = new Symbol("ABC");
        var portfolio = new Portfolio(100m, [new Position(Guid.NewGuid(), symbol, 2, 10m, null)], 0m, 0m);

        var events = PortfolioRiskMonitor.Evaluate(portfolio, 100m);

        Assert.Empty(events);
    }

    [Fact]
    public void Evaluate_ShouldRaiseDrawdownEventFromPeakEquity()
    {
        var portfolio = new Portfolio(70m, [], 0m, 0m);

        var events = PortfolioRiskMonitor.Evaluate(portfolio, 100m);

        var drawdown = Assert.Single(events, x => x.Rule == PortfolioRiskRule.Drawdown);
        Assert.Equal(0.30m, drawdown.ObservedValue);
    }

    [Fact]
    public void Evaluate_ShouldIgnoreInvalidPeakEquity()
    {
        var portfolio = new Portfolio(70m, [], 0m, 0m);

        var events = PortfolioRiskMonitor.Evaluate(portfolio, 0m);

        Assert.Empty(events);
    }

    [Theory]
    [InlineData(0, 1, 0.2)]
    [InlineData(0.25, 0, 0.2)]
    [InlineData(0.25, 1, 0)]
    [InlineData(1.1, 1, 0.2)]
    public void Evaluate_ShouldRejectInvalidThresholds(decimal concentration, decimal grossExposure, decimal drawdown)
    {
        var portfolio = new Portfolio(100m, [], 0m, 0m);
        var thresholds = new PortfolioRiskThresholds(concentration, grossExposure, drawdown);

        var action = () => PortfolioRiskMonitor.Evaluate(portfolio, 100m, thresholds: thresholds);

        Assert.Throws<ArgumentOutOfRangeException>(action);
    }
}
