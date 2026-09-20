namespace AiTrading.Application;

public sealed record LiveExecutionSafetyState(
    bool ExplicitlyEnabled,
    bool OperatorDisabled,
    bool EmergencyStopActive,
    bool ProviderHealthy,
    int UnresolvedReconciliationCount);

public sealed record LiveExecutionSafetyDecision(
    bool Allowed,
    string Reason);

public static class LiveExecutionSafetyGate
{
    public static LiveExecutionSafetyDecision Evaluate(LiveExecutionSafetyState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (!state.ExplicitlyEnabled)
            return new(false, "Live execution is not explicitly enabled.");
        if (state.OperatorDisabled)
            return new(false, "Live execution is operator-disabled.");
        if (state.EmergencyStopActive)
            return new(false, "Live execution emergency stop is active.");
        if (!state.ProviderHealthy)
            return new(false, "Live execution provider is not healthy.");
        if (state.UnresolvedReconciliationCount > 0)
            return new(false, "Live execution has unresolved reconciliation state.");

        return new(true, "Live execution safety controls are satisfied.");
    }
}
