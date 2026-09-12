namespace AiTrading.Application;

/// <summary>
/// Validates durable short state against its persisted cover-operation ledger after restart.
/// This is intentionally a pure check: it never repairs, closes, liquidates, or places orders.
/// </summary>
public static class DurableShortRecoveryReconciler
{
    public static DurableShortReconciliationResult Reconcile(
        DurableShortPositionState position,
        IReadOnlyList<DurableShortCoverState> covers)
    {
        if (position.OriginalQuantity <= 0 || position.RemainingQuantity < 0 || position.RemainingQuantity > position.OriginalQuantity)
            return Fail("INVALID_POSITION_QUANTITY");
        if (position.AverageEntryPrice <= 0)
            return Fail("INVALID_ENTRY_PRICE");
        if (position.Version < 0)
            return Fail("INVALID_POSITION_VERSION");

        var ordered = covers.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToArray();
        if (ordered.Any(x => x.PositionId != position.Id))
            return Fail("COVER_POSITION_MISMATCH");
        if (ordered.Any(x => string.IsNullOrWhiteSpace(x.IdempotencyKey)))
            return Fail("MISSING_IDEMPOTENCY_KEY");
        if (ordered.Select(x => x.IdempotencyKey).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            return Fail("DUPLICATE_IDEMPOTENCY_KEY");
        if (ordered.Any(x => x.CoverPrice <= 0 || x.CoverQuantity <= 0))
            return Fail("INVALID_COVER");

        var coveredQuantity = ordered.Sum(x => x.CoverQuantity);
        if (coveredQuantity > position.OriginalQuantity)
            return Fail("OVER_COVERED");

        var expectedRemaining = position.OriginalQuantity - coveredQuantity;
        var expectedPnl = ordered.Sum(x => x.RealizedPnl);
        var expectedVersion = ordered.Length;
        var expectedState = expectedRemaining == 0 ? "SHORT_CLOSED" : ordered.Length == 0 ? "OPEN" : "SHORT_PARTIALLY_COVERED";

        if (position.RemainingQuantity != expectedRemaining)
            return Fail("REMAINING_QUANTITY_MISMATCH");
        if (position.RealizedPnl != expectedPnl)
            return Fail("REALIZED_PNL_MISMATCH");
        if (position.Version != expectedVersion)
            return Fail("VERSION_MISMATCH");
        if (!string.Equals(position.State, expectedState, StringComparison.Ordinal))
            return Fail("STATE_MISMATCH");
        if (ordered.Length > 0 && position.LastCoverPrice != ordered[^1].CoverPrice)
            return Fail("LAST_COVER_PRICE_MISMATCH");

        return new DurableShortReconciliationResult(true, "CONSISTENT");
    }

    private static DurableShortReconciliationResult Fail(string reason) => new(false, reason);
}

public sealed record DurableShortReconciliationResult(bool IsConsistent, string Reason);
