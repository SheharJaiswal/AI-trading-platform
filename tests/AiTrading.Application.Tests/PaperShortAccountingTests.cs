namespace AiTrading.Application.Tests;

public sealed class PaperShortAccountingTests
{
    [Fact]
    public void Profit_is_entry_minus_cover_times_quantity()
    {
        var pnl = PaperShortAccounting.RealizedPnl(100m, 92m, 5);

        Assert.Equal(40m, pnl);
    }

    [Fact]
    public void Loss_is_negative_when_cover_is_above_entry()
    {
        var pnl = PaperShortAccounting.RealizedPnl(100m, 108m, 5);

        Assert.Equal(-40m, pnl);
    }

    [Fact]
    public void Partial_cover_returns_remaining_quantity_and_realized_pnl()
    {
        var result = PaperShortAccounting.Cover(100m, 95m, 10, 4);

        Assert.Equal(4, result.CoveredQuantity);
        Assert.Equal(6, result.RemainingQuantity);
        Assert.Equal(20m, result.RealizedPnl);
        Assert.False(result.FullyClosed);
    }

    [Fact]
    public void Full_cover_marks_position_closed()
    {
        var result = PaperShortAccounting.Cover(100m, 110m, 3, 3);

        Assert.Equal(0, result.RemainingQuantity);
        Assert.Equal(-30m, result.RealizedPnl);
        Assert.True(result.FullyClosed);
    }

    [Fact]
    public void Cover_cannot_exceed_open_quantity()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperShortAccounting.Cover(100m, 95m, 3, 4));
    }

    [Fact]
    public void Non_positive_prices_are_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperShortAccounting.RealizedPnl(0m, 95m, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PaperShortAccounting.RealizedPnl(100m, 0m, 1));
    }
}
