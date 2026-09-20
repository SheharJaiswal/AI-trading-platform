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

    public static LiveOrderReconciliationOutcome Compare(
        LiveOrderState local,
        LiveOrderReconciliationSnapshot broker)
    {
        ArgumentNullException.ThrowIfNull(local);
        ValidateSnapshot(broker);

        if (local.OrderId != broker.OrderId ||
            !string.Equals(local.IdempotencyKey, broker.IdempotencyKey, StringComparison.Ordinal) ||
            !string.Equals(local.Provider, broker.Provider, StringComparison.OrdinalIgnoreCase))
            return LiveOrderReconciliationOutcome.Divergent;

        if (local.Status == LiveOrderStatus.Unknown || broker.Status == LiveOrderStatus.Unknown)
            return LiveOrderReconciliationOutcome.Unresolved;

        if (local.Status != broker.Status)
            return LiveOrderReconciliationOutcome.Divergent;

        return LiveOrderReconciliationOutcome.Matched;
    }
}
