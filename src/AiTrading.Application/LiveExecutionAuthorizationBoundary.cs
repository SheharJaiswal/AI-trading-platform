namespace AiTrading.Application;

public sealed record LiveExecutionAuthorization(
    bool Allowed,
    LiveExecutionSafetyBlockReason BlockReason,
    string Reason)
{
    public static LiveExecutionAuthorization From(LiveExecutionSafetyDecision decision)
    {
        ArgumentNullException.ThrowIfNull(decision);
        return new(decision.Allowed, decision.BlockReason, decision.Reason);
    }
}

public sealed class LiveExecutionAuthorizationBoundary
{
    public LiveExecutionAuthorization Authorize(LiveExecutionSafetyState safetyState)
    {
        ArgumentNullException.ThrowIfNull(safetyState);
        return LiveExecutionAuthorization.From(LiveExecutionSafetyGate.Evaluate(safetyState));
    }
}
