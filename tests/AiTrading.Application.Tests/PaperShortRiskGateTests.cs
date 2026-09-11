using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class PaperShortRiskGateTests
{
    [Fact]
    public void Validate_ApprovesValidShort()
    {
        var result = PaperShortRiskGate.Validate(10, 100m, 105m, 90m);

        Assert.True(result.Approved);
        Assert.Null(result.Reason);
    }

    [Fact]
    public void Validate_RejectsStopAtOrBelowEntry()
    {
        var result = PaperShortRiskGate.Validate(1, 100m, 99m, 90m);

        Assert.False(result.Approved);
        Assert.Equal("SHORT_STOP_MUST_BE_ABOVE_ENTRY", result.Reason);
    }

    [Fact]
    public void Validate_RejectsTargetAtOrAboveEntry()
    {
        var result = PaperShortRiskGate.Validate(1, 100m, 105m, 100m);

        Assert.False(result.Approved);
        Assert.Equal("SHORT_TARGET_MUST_BE_BELOW_ENTRY", result.Reason);
    }

    [Fact]
    public void EvaluateExit_StopsWhenPriceRisesToStop()
    {
        Assert.Equal("SHORT_STOPPED", PaperShortRiskGate.EvaluateExit(105m, 105m, 90m));
    }

    [Fact]
    public void EvaluateExit_HitsTargetWhenPriceFallsToTarget()
    {
        Assert.Equal("SHORT_TARGET_HIT", PaperShortRiskGate.EvaluateExit(90m, 105m, 90m));
    }

    [Fact]
    public void UnrealizedPnl_UsesShortDirection()
    {
        Assert.Equal(50m, PaperShortRiskGate.UnrealizedPnl(100m, 95m, 10));
    }
}
