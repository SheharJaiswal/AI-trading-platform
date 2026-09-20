namespace AiTrading.Application.Tests;

public sealed class LiveExecutionSafetyControlsTests
{
    private static LiveExecutionSafetyState Safe() => new(
        ExplicitlyEnabled: true,
        OperatorDisabled: false,
        EmergencyStopActive: false,
        ProviderHealthy: true,
        UnresolvedReconciliationCount: 0);

    [Fact]
    public void Safe_State_Allows_Live_Execution()
    {
        var decision = LiveExecutionSafetyGate.Evaluate(Safe());

        Assert.True(decision.Allowed);
        Assert.Equal("Live execution safety controls are satisfied.", decision.Reason);
    }

    [Fact]
    public void Explicit_Enablement_Is_Required()
    {
        var decision = LiveExecutionSafetyGate.Evaluate(Safe() with { ExplicitlyEnabled = false });

        Assert.False(decision.Allowed);
        Assert.Contains("not explicitly enabled", decision.Reason);
    }

    [Fact]
    public void Operator_Disable_Is_Fail_Closed()
    {
        var decision = LiveExecutionSafetyGate.Evaluate(Safe() with { OperatorDisabled = true });

        Assert.False(decision.Allowed);
        Assert.Contains("operator-disabled", decision.Reason);
    }

    [Fact]
    public void Emergency_Stop_Is_Fail_Closed()
    {
        var decision = LiveExecutionSafetyGate.Evaluate(Safe() with { EmergencyStopActive = true });

        Assert.False(decision.Allowed);
        Assert.Contains("emergency stop", decision.Reason);
    }

    [Fact]
    public void Provider_Health_Is_Required()
    {
        var decision = LiveExecutionSafetyGate.Evaluate(Safe() with { ProviderHealthy = false });

        Assert.False(decision.Allowed);
        Assert.Contains("not healthy", decision.Reason);
    }

    [Fact]
    public void Any_Unresolved_Reconciliation_Blocks_Live_Execution()
    {
        var decision = LiveExecutionSafetyGate.Evaluate(Safe() with { UnresolvedReconciliationCount = 1 });

        Assert.False(decision.Allowed);
        Assert.Contains("unresolved reconciliation", decision.Reason);
    }

    [Fact]
    public void Multiple_Safety_Failures_Remain_Fail_Closed()
    {
        var decision = LiveExecutionSafetyGate.Evaluate(Safe() with
        {
            ExplicitlyEnabled = false,
            OperatorDisabled = true,
            EmergencyStopActive = true,
            ProviderHealthy = false,
            UnresolvedReconciliationCount = 3
        });

        Assert.False(decision.Allowed);
        Assert.Contains("not explicitly enabled", decision.Reason);
    }
}
