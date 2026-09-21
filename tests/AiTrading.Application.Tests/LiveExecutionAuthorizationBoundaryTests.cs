namespace AiTrading.Application.Tests;

public sealed class LiveExecutionAuthorizationBoundaryTests
{
    private static LiveExecutionSafetyState Safe() => new(
        ExplicitlyEnabled: true,
        OperatorDisabled: false,
        EmergencyStopActive: false,
        ProviderHealthy: true,
        UnresolvedReconciliationCount: 0,
        AccountId: "account",
        Environment: "sandbox",
        CredentialReference: LiveExecutionCredentialReference.Create(
            "broker",
            "secret://live/account",
            "account",
            "sandbox"));

    [Fact]
    public void Authorize_Delegates_To_Safety_Gate_And_Preserves_Allowed_Decision()
    {
        var authorization = new LiveExecutionAuthorizationBoundary().Authorize(Safe());

        Assert.True(authorization.Allowed);
        Assert.Equal(LiveExecutionSafetyBlockReason.None, authorization.BlockReason);
        Assert.Equal("Live execution safety controls are satisfied.", authorization.Reason);
    }

    [Fact]
    public void Authorize_Fails_Closed_When_Safety_Gate_Blocks()
    {
        var authorization = new LiveExecutionAuthorizationBoundary().Authorize(Safe() with
        {
            EmergencyStopActive = true
        });

        Assert.False(authorization.Allowed);
        Assert.Equal(LiveExecutionSafetyBlockReason.EmergencyStopActive, authorization.BlockReason);
        Assert.Equal("Live execution emergency stop is active.", authorization.Reason);
    }

    [Fact]
    public void Authorize_Fails_Closed_When_Credential_Reference_Is_Missing()
    {
        var authorization = new LiveExecutionAuthorizationBoundary().Authorize(Safe() with
        {
            CredentialReference = null
        });

        Assert.False(authorization.Allowed);
        Assert.Equal(LiveExecutionSafetyBlockReason.MissingCredentialReference, authorization.BlockReason);
    }
}
