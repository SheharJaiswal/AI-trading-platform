using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class PaperShortCoverPlannerTests
{
    [Fact]
    public void Profit_cover_produces_positive_realized_pnl()
    {
        var result = PaperShortCoverPlanner.Plan(100m, 92m, 5, 5);

        Assert.Equal(40m, result.RealizedPnl);
        Assert.Equal(0, result.RemainingQuantity);
        Assert.True(result.FullyClosed);
        Assert.Equal("SHORT_CLOSED", result.State);
    }

    [Fact]
    public void Partial_cover_preserves_remaining_short_quantity()
    {
        var result = PaperShortCoverPlanner.Plan(100m, 96m, 10, 4);

        Assert.Equal(16m, result.RealizedPnl);
        Assert.Equal(6, result.RemainingQuantity);
        Assert.False(result.FullyClosed);
        Assert.Equal("SHORT_PARTIALLY_COVERED", result.State);
    }

    [Fact]
    public void Losing_cover_produces_negative_realized_pnl()
    {
        var result = PaperShortCoverPlanner.Plan(100m, 105m, 2, 2);

        Assert.Equal(-10m, result.RealizedPnl);
        Assert.True(result.FullyClosed);
    }

    [Fact]
    public void Cover_cannot_exceed_open_quantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperShortCoverPlanner.Plan(100m, 95m, 2, 3));
    }

    [Fact]
    public void Non_positive_prices_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperShortCoverPlanner.Plan(0m, 95m, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperShortCoverPlanner.Plan(100m, 0m, 1, 1));
    }
}
