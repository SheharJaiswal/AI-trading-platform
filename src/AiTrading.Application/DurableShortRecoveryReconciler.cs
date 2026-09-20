using AiTrading.Domain;

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
        if (position.UpdatedAt < position.CreatedAt)
            return Fail("INVALID_POSITION_TIMESTAMP");

        var ordered = covers.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToArray();
        if (ordered.Any(x => x.PositionId != position.Id))
            return Fail("COVER_POSITION_MISMATCH");
        if (ordered.Any(x => x.CreatedAt < position.CreatedAt))
            return Fail("COVER_TIMESTAMP_BEFORE_POSITION");
        if (ordered.Any(x => string.IsNullOrWhiteSpace(x.IdempotencyKey)))
            return Fail("MISSING_IDEMPOTENCY_KEY");
        if (ordered.Select(x => x.IdempotencyKey).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
            return Fail("DUPLICATE_IDEMPOTENCY_KEY");
        if (ordered.Any(x => x.CoverPrice <= 0 || x.CoverQuantity <= 0 || x.RealizedPnl != PaperShortAccounting.RealizedPnl(position.AverageEntryPrice, x.CoverPrice, x.CoverQuantity)))
            return Fail("INVALID_COVER");

        var coveredQuantity = ordered.Sum(x => x.CoverQuantity);
        if (coveredQuantity > position.OriginalQuantity)
            return Fail("OVER_COVERED");

        var expectedRemaining = position.OriginalQuantity - coveredQuantity;
        var expectedPnl = ordered.Sum(x => x.RealizedPnl);
        var expectedVersion = ordered.Length;
        var expectedState = expectedRemaining == 0 ? "SHORT_CLOSED" : ordered.Length == 0 ? "SHORT_OPEN" : "SHORT_PARTIALLY_COVERED";

        if (position.RemainingQuantity != expectedRemaining)
            return Fail("REMAINING_QUANTITY_MISMATCH");
        if (position.RealizedPnl != expectedPnl)
            return Fail("REALIZED_PNL_MISMATCH");
        if (position.Version != expectedVersion)
            return Fail("VERSION_MISMATCH");
        if (ordered.Select((cover, index) => cover.ResultingPositionVersion == index + 1).Any(valid => !valid))
            return Fail("COVER_VERSION_MISMATCH");
        if (!string.Equals(position.State, expectedState, StringComparison.Ordinal))
            return Fail("STATE_MISMATCH");
        if (ordered.Length > 0 && position.LastCoverPrice != ordered[^1].CoverPrice)
            return Fail("LAST_COVER_PRICE_MISMATCH");

        return new DurableShortReconciliationResult(true, "CONSISTENT");
    }

    public static DurableShortReconciliationResult ReconcileExecution(
        OrderState order,
        FillState fill,
        DurableShortPositionState position)
    {
        if (order.Side != OrderSide.Sell || !string.Equals(order.ExecutionMode, "paper", StringComparison.OrdinalIgnoreCase) || !string.Equals(order.Status, "short-open-filled", StringComparison.Ordinal))
            return Fail("INVALID_SHORT_ORDER");
        if (order.CreatedAt == default)
            return Fail("INVALID_SHORT_ORDER_TIMESTAMP");
        if (order.Id != fill.OrderId || fill.OrderId != position.Id)
            return Fail("EXECUTION_POSITION_ID_MISMATCH");
        if (fill.Side != OrderSide.Sell || fill.Quantity <= 0 || fill.FillPrice <= 0 || !string.Equals(fill.ExecutionProvider, "paper", StringComparison.OrdinalIgnoreCase))
            return Fail("INVALID_SHORT_FILL");
        if (fill.FilledAt == default || fill.FilledAt < order.CreatedAt)
            return Fail("INVALID_SHORT_FILL_TIMESTAMP");
        if (position.UpdatedAt < position.CreatedAt)
            return Fail("INVALID_SHORT_POSITION_TIMESTAMP");
        if (position.CreatedAt < fill.FilledAt)
            return Fail("POSITION_CREATED_BEFORE_FILL");
        if (order.Quantity != fill.Quantity || order.Symbol != fill.Symbol || position.Symbol != fill.Symbol)
            return Fail("EXECUTION_POSITION_DETAILS_MISMATCH");
        if (position.OriginalQuantity != fill.Quantity || position.RemainingQuantity != fill.Quantity || position.AverageEntryPrice != fill.FillPrice)
            return Fail("POSITION_FILL_MISMATCH");
        if (position.RealizedPnl != 0m || position.LastCoverPrice is not null || position.Version != 0 || !string.Equals(position.State, "SHORT_OPEN", StringComparison.Ordinal))
            return Fail("INVALID_OPEN_POSITION_STATE");

        return new DurableShortReconciliationResult(true, "CONSISTENT");
    }

    private static DurableShortReconciliationResult Fail(string reason) => new(false, reason);
}

public sealed record DurableShortReconciliationResult(bool IsConsistent, string Reason);
