using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class PaperShortLifecycleTests
{
    [Fact]
    public void ShortPnlIsPositiveWhenPriceFalls()
    {
        var position = PaperShortPosition.Open("TEST", 10, 100m, 90m, 105m, 80m);
        Assert.Equal(100m, position.UnrealizedPnl);
    }

    [Fact]
    public void ShortPnlIsNegativeWhenPriceRises()
    {
        var position = PaperShortPosition.Open("TEST", 10, 100m, 110m, 105m, 80m);
        Assert.Equal(-100m, position.UnrealizedPnl);
    }

    [Fact]
    public void StopLossClosesShortAsStopped()
    {
        var position = PaperShortPosition.Open("TEST", 1, 100m, 100m, 105m, 80m);
        var marked = position.Mark(105m);
        Assert.Equal(PaperShortLifecycleStatus.ShortStopped, marked.Status);
    }

    [Fact]
    public void TargetClosesShortAsTargetHit()
    {
        var position = PaperShortPosition.Open("TEST", 1, 100m, 100m, 105m, 80m);
        var marked = position.Mark(80m);
        Assert.Equal(PaperShortLifecycleStatus.ShortTargetHit, marked.Status);
    }

    [Fact]
    public void BetweenLevelsKeepsShortOpen()
    {
        var position = PaperShortPosition.Open("TEST", 1, 100m, 100m, 105m, 80m);
        var marked = position.Mark(95m);
        Assert.Equal(PaperShortLifecycleStatus.ShortOpen, marked.Status);
        Assert.Equal(5m, marked.UnrealizedPnl);
    }

    [Fact]
    public void InvalidShortLevelsProduceNoTradeSignal()
    {
        var signal = PaperShortLifecycleEngine.Signal("TEST", 1, 100m, 95m, 80m);
        Assert.Equal(PaperShortLifecycleStatus.NoTrade, signal.Status);
    }

    [Fact]
    public void ValidShortSignalDoesNotExecuteAnOrder()
    {
        var signal = PaperShortLifecycleEngine.Signal("TEST", 1, 100m, 105m, 80m);
        Assert.Equal(PaperShortLifecycleStatus.SignalShort, signal.Status);
    }
}
