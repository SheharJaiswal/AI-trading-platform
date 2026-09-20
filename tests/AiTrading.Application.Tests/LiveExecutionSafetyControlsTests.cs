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
        Assert.Equal(LiveExecutionSafetyBlockReason.None, decision.BlockReason);
        Assert.Equal("Live execution safety controls are satisfied.", decision.Reason);
    }

    [Theory]
    [InlineData(false, false, false, true, 0, LiveExecutionSafetyBlockReason.NotExplicitlyEnabled)]
    [InlineData(true, true, false, true, 0, LiveExecutionSafetyBlockReason.OperatorDisabled)]
    [InlineData(true, false, true, true, 0, LiveExecutionSafetyBlockReason.EmergencyStopActive)]
    [InlineData(true, false, false, false, 0, LiveExecutionSafetyBlockReason.ProviderUnhealthy)]
    [InlineData(true, false, false, true, 1, LiveExecutionSafetyBlockReason.UnresolvedReconciliation)]
    [InlineData(true, false, false, true, -1, LiveExecutionSafetyBlockReason.InvalidReconciliationState)]
    public void Safety_Block_Uses_Stable_Reason_Code(
        bool explicitlyEnabled,
        bool operatorDisabled,
        bool emergencyStopActive,
        bool providerHealthy,
        int unresolvedReconciliationCount,
        LiveExecutionSafetyBlockReason expectedReason)
    {
        var decision = LiveExecutionSafetyGate.Evaluate(new(
            explicitlyEnabled,
            operatorDisabled,
            emergencyStopActive,
            providerHealthy,
            unresolvedReconciliationCount));

        Assert.False(decision.Allowed);
        Assert.Equal(expectedReason, decision.BlockReason);
    }

    [Fact]
    public void Multiple_Safety_Failures_Use_First_Fail_Closed_Reason()
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
        Assert.Equal(LiveExecutionSafetyBlockReason.NotExplicitlyEnabled, decision.BlockReason);
        Assert.Contains("not explicitly enabled", decision.Reason);
    }
}
