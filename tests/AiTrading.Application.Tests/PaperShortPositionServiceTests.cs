using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class PaperShortPositionServiceTests
{
    [Fact]
    public void Full_cover_closes_position_and_records_profit()
    {
        var now = DateTimeOffset.UtcNow;
        var position = PaperShortPosition.Open(Guid.NewGuid(), Guid.NewGuid(), new Symbol("TEST"), 5, 100m, now);

        var result = PaperShortPositionService.Cover(position, 92m, 5, now.AddMinutes(1));

        Assert.Equal(0, result.RemainingQuantity);
        Assert.Equal(40m, result.RealizedPnl);
        Assert.Equal(92m, result.LastCoverPrice);
        Assert.Equal("SHORT_CLOSED", result.State);
        Assert.Equal(1, result.Version);
    }

    [Fact]
    public void Partial_cover_preserves_remaining_quantity()
    {
        var now = DateTimeOffset.UtcNow;
        var position = PaperShortPosition.Open(Guid.NewGuid(), Guid.NewGuid(), new Symbol("TEST"), 10, 100m, now);

        var result = PaperShortPositionService.Cover(position, 96m, 4, now.AddMinutes(1));

        Assert.Equal(6, result.RemainingQuantity);
        Assert.Equal(16m, result.RealizedPnl);
        Assert.Equal("SHORT_PARTIALLY_COVERED", result.State);
    }

    [Fact]
    public void Cover_at_loss_records_negative_realized_pnl()
    {
        var now = DateTimeOffset.UtcNow;
        var position = PaperShortPosition.Open(Guid.NewGuid(), Guid.NewGuid(), new Symbol("TEST"), 2, 100m, now);

        var result = PaperShortPositionService.Cover(position, 105m, 2, now.AddMinutes(1));

        Assert.Equal(-10m, result.RealizedPnl);
        Assert.Equal("SHORT_CLOSED", result.State);
    }

    [Fact]
    public void Over_cover_is_rejected_without_mutation()
    {
        var now = DateTimeOffset.UtcNow;
        var position = PaperShortPosition.Open(Guid.NewGuid(), Guid.NewGuid(), new Symbol("TEST"), 2, 100m, now);

        Assert.Throws<InvalidOperationException>(() => PaperShortPositionService.Cover(position, 95m, 3, now.AddMinutes(1)));
        Assert.Equal(2, position.RemainingQuantity);
        Assert.Equal(0m, position.RealizedPnl);
    }
}
