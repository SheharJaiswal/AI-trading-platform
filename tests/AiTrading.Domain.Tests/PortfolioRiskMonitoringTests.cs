using AiTrading.Domain;
using FluentAssertions;

namespace AiTrading.Domain.Tests;

public sealed class PortfolioRiskMonitoringTests
{
    [Fact]
    public void Evaluate_ShouldRaiseConcentrationAndGrossExposureEvents()
    {
        var symbol = new Symbol("ABC");
        var portfolio = new Portfolio(
            100m,
            [new Position(Guid.NewGuid(), symbol, 3, 20m, null)],
            0m,
            0m);

        var events = PortfolioRiskMonitor.Evaluate(portfolio, 100m, new Dictionary<Symbol, decimal> { [symbol] = 40m });

        events.Should().ContainSingle(x => x.Rule == PortfolioRiskRule.PositionConcentration);
        events.Should().ContainSingle(x => x.Rule == PortfolioRiskRule.GrossExposure);
        events.Should().OnlyContain(x => x.Severity == AlertSeverity.Warning);
    }

    [Fact]
    public void Evaluate_ShouldUseEntryPriceWhenMarketPriceIsMissing()
    {
        var symbol = new Symbol("ABC");
        var portfolio = new Portfolio(
            100m,
            [new Position(Guid.NewGuid(), symbol, 2, 10m, null)],
            0m,
            0m);

        var events = PortfolioRiskMonitor.Evaluate(portfolio, 100m);

        events.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ShouldRaiseDrawdownEventFromPeakEquity()
    {
        var portfolio = new Portfolio(70m, [], 0m, 0m);

        var events = PortfolioRiskMonitor.Evaluate(portfolio, 100m);

        events.Should().ContainSingle(x => x.Rule == PortfolioRiskRule.Drawdown)
            .Which.ObservedValue.Should().Be(0.30m);
    }

    [Fact]
    public void Evaluate_ShouldIgnoreInvalidPeakEquity()
    {
        var portfolio = new Portfolio(70m, [], 0m, 0m);

        var events = PortfolioRiskMonitor.Evaluate(portfolio, 0m);

        events.Should().BeEmpty();
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

        action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
