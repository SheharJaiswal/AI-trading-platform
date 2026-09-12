namespace AiTrading.Application;

/// <summary>
/// Application operation for read-only durable short recovery reconciliation.
/// It loads persisted state and its cover ledger, then delegates to the deterministic reconciler.
/// </summary>
public sealed class DurableShortRecoveryService(IDurableShortPositionRepository repository)
{
    public async Task<DurableShortReconciliationResult> ReconcileAsync(Guid positionId, CancellationToken ct)
    {
        var position = await repository.GetAsync(positionId, ct)
            ?? throw new KeyNotFoundException($"Paper short position {positionId} does not exist.");

        var covers = await repository.GetCoversAsync(positionId, ct);
        return DurableShortRecoveryReconciler.Reconcile(position, covers);
    }
}
