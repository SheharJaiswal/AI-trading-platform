namespace AiTrading.Application;

public enum LiveOrderReconciliationOutcome
{
    Matched,
    Divergent,
    Unresolved
}

public sealed record LiveOrderReconciliationSnapshot(
    Guid OrderId,
    string IdempotencyKey,
    string Provider,
    string? ProviderOrderId,
    LiveOrderStatus Status,
    int FilledQuantity,
    decimal? AverageFillPrice,
    DateTimeOffset ObservedAt);

public sealed record LiveOrderReconciliationDecision(
    LiveOrderReconciliationOutcome Outcome,
    LiveOrderStatus BrokerStatus,
    bool ReconciliationRequired,
    string? Reason,
    string? ProviderOrderId,
    DateTimeOffset ObservedAt);

public static class LiveOrderReconciliationContract
{
    public static void ValidateSnapshot(LiveOrderReconciliationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.OrderId == Guid.Empty) throw new ArgumentException("OrderId is required.", nameof(snapshot));
        if (string.IsNullOrWhiteSpace(snapshot.IdempotencyKey) || snapshot.IdempotencyKey.Length > 128)
            throw new ArgumentException("A non-empty idempotency key of at most 128 characters is required.", nameof(snapshot));
        if (string.IsNullOrWhiteSpace(snapshot.Provider)) throw new ArgumentException("Provider is required.", nameof(snapshot));
        if (snapshot.FilledQuantity < 0) throw new ArgumentException("Filled quantity cannot be negative.", nameof(snapshot));
        if (snapshot.AverageFillPrice is <= 0) throw new ArgumentException("Average fill price must be positive when provided.", nameof(snapshot));
        if (snapshot.FilledQuantity > 0 && snapshot.AverageFillPrice is null)
            throw new InvalidOperationException("A filled quantity requires an average fill price.");
        if (snapshot.ObservedAt == default) throw new ArgumentException("ObservedAt is required.", nameof(snapshot));
    }

    public static LiveOrderReconciliationDecision Decide(
        LiveOrderState local,
        LiveOrderReconciliationSnapshot broker)
    {
        ArgumentNullException.ThrowIfNull(local);
        ValidateSnapshot(broker);

        if (local.OrderId != broker.OrderId)
            return Divergent(broker, "Order identity differs between local and broker state.");

        if (!string.Equals(local.IdempotencyKey, broker.IdempotencyKey, StringComparison.Ordinal))
            return Divergent(broker, "Idempotency key differs between local and broker state.");

        if (!string.Equals(local.Provider, broker.Provider, StringComparison.OrdinalIgnoreCase))
            return Divergent(broker, "Execution provider differs between local and broker state.");

        if (!string.Equals(local.ProviderOrderId, broker.ProviderOrderId, StringComparison.Ordinal))
            return Divergent(broker, "Provider order identity differs between local and broker state.");

        if (local.Status == LiveOrderStatus.Unknown || broker.Status == LiveOrderStatus.Unknown)
            return new LiveOrderReconciliationDecision(
                LiveOrderReconciliationOutcome.Unresolved,
                broker.Status,
                true,
                "Unknown execution state requires reconciliation before it can be resolved.",
                broker.ProviderOrderId,
                broker.ObservedAt);

        if (local.Status != broker.Status)
            return Divergent(broker, "Live order status differs between local and broker state.");

        return new LiveOrderReconciliationDecision(
            LiveOrderReconciliationOutcome.Matched,
            broker.Status,
            false,
            null,
            broker.ProviderOrderId,
            broker.ObservedAt);
    }

    public static LiveOrderReconciliationOutcome Compare(
        LiveOrderState local,
        LiveOrderReconciliationSnapshot broker) => Decide(local, broker).Outcome;

    private static LiveOrderReconciliationDecision Divergent(
        LiveOrderReconciliationSnapshot broker,
        string reason) => new(
            LiveOrderReconciliationOutcome.Divergent,
            broker.Status,
            true,
            reason,
            broker.ProviderOrderId,
            broker.ObservedAt);
}
