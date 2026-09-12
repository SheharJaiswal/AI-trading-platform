namespace AiTrading.Application;

public sealed record DurableShortRecoveryDiagnostic(
    DurableShortPositionState Position,
    IReadOnlyList<DurableShortCoverState> Covers,
    DurableShortReconciliationResult Reconciliation);

public sealed class DurableShortRecoveryQueryService(ITradingUnitOfWorkFactory unitOfWorkFactory)
{
    public async Task<DurableShortRecoveryDiagnostic?> GetAsync(Guid positionId, CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var position = await unitOfWork.DurableShortPositions.GetAsync(positionId, cancellationToken);
        if (position is null) return null;

        var covers = await unitOfWork.DurableShortPositions.GetCoversAsync(positionId, cancellationToken);
        var reconciliation = DurableShortRecoveryReconciler.Reconcile(position, covers);
        return new DurableShortRecoveryDiagnostic(position, covers, reconciliation);
    }
}
