namespace AiTrading.Application;

public enum LiveOrderStatus
{
    Pending,
    Submitted,
    PartiallyFilled,
    Filled,
    Rejected,
    Cancelled,
    Unknown
}

public sealed record LiveOrderState(
    Guid OrderId,
    string IdempotencyKey,
    string AccountContext,
    string Provider,
    LiveOrderStatus Status,
    string? ProviderOrderId,
    bool ReconciliationRequired,
    string? Reason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version);

public static class LiveOrderStateTransition
{
    public static void ValidateCreate(LiveOrderState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.OrderId == Guid.Empty) throw new ArgumentException("OrderId is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(state.IdempotencyKey) || state.IdempotencyKey.Length > 128)
            throw new ArgumentException("A non-empty idempotency key of at most 128 characters is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(state.AccountContext)) throw new ArgumentException("Account context is required.", nameof(state));
        if (string.IsNullOrWhiteSpace(state.Provider)) throw new ArgumentException("Provider is required.", nameof(state));
        if (state.Status != LiveOrderStatus.Pending) throw new InvalidOperationException("A new live order must start in Pending state.");
        if (state.Version != 1) throw new InvalidOperationException("A new live order must start at version 1.");
        if (state.ReconciliationRequired) throw new InvalidOperationException("A new live order cannot require reconciliation.");
    }

    public static void ValidateTransition(LiveOrderState current, LiveOrderStatus next, bool reconciliationRequired)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (current.Status is LiveOrderStatus.Filled or LiveOrderStatus.Rejected or LiveOrderStatus.Cancelled)
            throw new InvalidOperationException($"Terminal live order state {current.Status} cannot transition.");
        if (current.Status == LiveOrderStatus.Unknown && next != LiveOrderStatus.Unknown && !reconciliationRequired)
            throw new InvalidOperationException("An unknown submission state must remain reconciliation-required until resolved.");
        var allowed = current.Status switch
        {
            LiveOrderStatus.Pending => next is LiveOrderStatus.Submitted or LiveOrderStatus.Rejected or LiveOrderStatus.Unknown,
            LiveOrderStatus.Submitted => next is LiveOrderStatus.PartiallyFilled or LiveOrderStatus.Filled or LiveOrderStatus.Rejected or LiveOrderStatus.Cancelled or LiveOrderStatus.Unknown,
            LiveOrderStatus.PartiallyFilled => next is LiveOrderStatus.PartiallyFilled or LiveOrderStatus.Filled or LiveOrderStatus.Cancelled or LiveOrderStatus.Unknown,
            LiveOrderStatus.Unknown => next == LiveOrderStatus.Unknown,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException($"Invalid live order transition from {current.Status} to {next}.");
        if (next == LiveOrderStatus.Unknown && !reconciliationRequired)
            throw new InvalidOperationException("Unknown live order state requires reconciliation.");
    }
}
