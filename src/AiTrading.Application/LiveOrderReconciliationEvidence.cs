namespace AiTrading.Application;

public sealed record LiveOrderReconciliationEvidence(
    Guid EvidenceId,
    Guid OrderId,
    string IdempotencyKey,
    string Provider,
    string? ProviderOrderId,
    LiveOrderStatus LocalStatus,
    LiveOrderStatus BrokerStatus,
    LiveOrderReconciliationOutcome Outcome,
    bool ReconciliationRequired,
    string? Reason,
    DateTimeOffset ObservedAt)
{
    public static LiveOrderReconciliationEvidence Create(
        LiveOrderState local,
        LiveOrderReconciliationSnapshot broker,
        LiveOrderReconciliationDecision decision)
    {
        ArgumentNullException.ThrowIfNull(local);
        ArgumentNullException.ThrowIfNull(broker);
        ArgumentNullException.ThrowIfNull(decision);

        if (local.OrderId != broker.OrderId)
            throw new ArgumentException("Local and broker order identities must match.", nameof(broker));
        if (!string.Equals(local.IdempotencyKey, broker.IdempotencyKey, StringComparison.Ordinal))
            throw new ArgumentException("Local and broker idempotency keys must match.", nameof(broker));
        if (!string.Equals(local.Provider, broker.Provider, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Local and broker providers must match.", nameof(broker));
        if (decision.ProviderOrderId != broker.ProviderOrderId)
            throw new ArgumentException("Decision provider order identity must match the broker snapshot.", nameof(decision));
        if (decision.BrokerStatus != broker.Status || decision.ObservedAt != broker.ObservedAt)
            throw new ArgumentException("Decision broker state must match the broker snapshot.", nameof(decision));
        if (decision.ReconciliationRequired != (decision.Outcome != LiveOrderReconciliationOutcome.Matched))
            throw new InvalidOperationException("Only matched reconciliation evidence may clear the reconciliation requirement.");

        return new LiveOrderReconciliationEvidence(
            Guid.NewGuid(),
            local.OrderId,
            local.IdempotencyKey,
            local.Provider,
            broker.ProviderOrderId,
            local.Status,
            broker.Status,
            decision.Outcome,
            decision.ReconciliationRequired,
            decision.Reason,
            broker.ObservedAt);
    }
}
