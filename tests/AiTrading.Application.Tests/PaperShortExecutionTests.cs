using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class PaperShortExecutionTests
{
    [Fact]
    public void Short_execution_request_can_describe_a_valid_risk_bounded_trade()
    {
        var request = new PaperShortExecutionRequest(
            Guid.NewGuid(),
            "short:ABC:001",
            new Symbol("ABC", "123"),
            10,
            100m,
            105m,
            94m,
            "baseline-v1");

        var risk = PaperShortRiskGate.Validate(
            request.Quantity,
            request.EntryPrice,
            request.StopLoss,
            request.TargetPrice);

        Assert.True(risk.Approved);
    }

    [Fact]
    public void Short_execution_request_rejects_long_side_stop_configuration()
    {
        var risk = PaperShortRiskGate.Validate(10, 100m, 95m, 94m);

        Assert.False(risk.Approved);
        Assert.Contains("stop-loss", risk.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Short_execution_request_rejects_long_side_target_configuration()
    {
        var risk = PaperShortRiskGate.Validate(10, 100m, 105m, 101m);

        Assert.False(risk.Approved);
        Assert.Contains("target", risk.Reason, StringComparison.OrdinalIgnoreCase);
    }
}
