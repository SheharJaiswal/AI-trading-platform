namespace AiTrading.Application;

public enum LiveExecutionSafetyBlockReason
{
    None,
    InvalidReconciliationState,
    NotExplicitlyEnabled,
    OperatorDisabled,
    EmergencyStopActive,
    ProviderUnhealthy,
    MissingExecutionContext,
    UnresolvedReconciliation
}

public sealed record LiveExecutionSafetyState(
    bool ExplicitlyEnabled,
    bool OperatorDisabled,
    bool EmergencyStopActive,
    bool ProviderHealthy,
    int UnresolvedReconciliationCount,
    string? AccountId = null,
    string? Environment = null);

public sealed record LiveExecutionSafetyDecision(
    bool Allowed,
    LiveExecutionSafetyBlockReason BlockReason,
    string Reason);

public static class LiveExecutionSafetyGate
{
    public static LiveExecutionSafetyDecision Evaluate(LiveExecutionSafetyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.UnresolvedReconciliationCount < 0)
            return Block(LiveExecutionSafetyBlockReason.InvalidReconciliationState, "Live execution reconciliation state is invalid.");
        if (!state.ExplicitlyEnabled)
            return Block(LiveExecutionSafetyBlockReason.NotExplicitlyEnabled, "Live execution is not explicitly enabled.");
        if (state.OperatorDisabled)
            return Block(LiveExecutionSafetyBlockReason.OperatorDisabled, "Live execution is operator-disabled.");
        if (state.EmergencyStopActive)
            return Block(LiveExecutionSafetyBlockReason.EmergencyStopActive, "Live execution emergency stop is active.");
        if (!state.ProviderHealthy)
            return Block(LiveExecutionSafetyBlockReason.ProviderUnhealthy, "Live execution provider is not healthy.");
        if (string.IsNullOrWhiteSpace(state.AccountId) || string.IsNullOrWhiteSpace(state.Environment))
            return Block(LiveExecutionSafetyBlockReason.MissingExecutionContext, "Live execution account and environment context are required.");
        if (state.UnresolvedReconciliationCount > 0)
            return Block(LiveExecutionSafetyBlockReason.UnresolvedReconciliation, "Live execution has unresolved reconciliation state.");

        return new(true, LiveExecutionSafetyBlockReason.None, "Live execution safety controls are satisfied.");
    }

    private static LiveExecutionSafetyDecision Block(LiveExecutionSafetyBlockReason reason, string message) =>
        new(false, reason, message);
}
