using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurableShortRecoveryReconcilerTests
{
    [Fact]
    public void Position_timestamp_regression_is_detected()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 5, 100m, null, null, null, 0m, "SHORT_OPEN", created, created.AddMinutes(-1), 0);
        var result = DurableShortRecoveryReconciler.Reconcile(position, []);
        Assert.False(result.IsConsistent);
        Assert.Equal("INVALID_POSITION_TIMESTAMP", result.Reason);
    }

    [Fact]
    public void Cover_before_position_creation_is_detected()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 92m, 5, 40m, 1, created.AddMinutes(-1)) };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, null, null, 92m, 40m, "SHORT_CLOSED", created, created, 1);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.False(result.IsConsistent);
        Assert.Equal("COVER_TIMESTAMP_BEFORE_POSITION", result.Reason);
    }

    [Fact]
    public void Consistent_full_cover_state_reconciles()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 92m, 5, 40m, 1, created.AddMinutes(1)) };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, null, null, 92m, 40m, "SHORT_CLOSED", created, created.AddMinutes(1), 1);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.True(result.IsConsistent);
        Assert.Equal("CONSISTENT", result.Reason);
    }

    [Fact]
    public void Untouched_short_position_reconciles_as_open_version_zero()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 5, 100m, null, null, null, 0m, "SHORT_OPEN", created, created, 0);
        var result = DurableShortRecoveryReconciler.Reconcile(position, []);
        Assert.True(result.IsConsistent);
        Assert.Equal("CONSISTENT", result.Reason);
    }

    [Fact]
    public void Partial_cover_state_reconciles()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 96m, 4, 16m, 1, created.AddMinutes(1)) };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 10, 6, 100m, null, null, 96m, 16m, "SHORT_PARTIALLY_COVERED", created, created.AddMinutes(1), 1);
        Assert.True(DurableShortRecoveryReconciler.Reconcile(position, covers).IsConsistent);
    }

    [Fact]
    public void Non_sequential_cover_versions_are_detected()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 95m, 1, 5m, 1, created.AddMinutes(1)), new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-2", 94m, 1, 6m, 3, created.AddMinutes(2)) };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 2, 0, 100m, null, null, 94m, 11m, "SHORT_CLOSED", created, created.AddMinutes(2), 2);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.False(result.IsConsistent);
        Assert.Equal("COVER_VERSION_MISMATCH", result.Reason);
    }

    [Fact]
    public void Duplicate_idempotency_keys_are_detected()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 95m, 1, 5m, 1, created.AddMinutes(1)), new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 94m, 1, 6m, 2, created.AddMinutes(2)) };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 2, 0, 100m, null, null, 94m, 11m, "SHORT_CLOSED", created, created.AddMinutes(2), 2);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.False(result.IsConsistent);
        Assert.Equal("DUPLICATE_IDEMPOTENCY_KEY", result.Reason);
    }

    [Fact]
    public void Version_and_state_mismatch_are_detected()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 92m, 5, 40m, 1, created.AddMinutes(1)) };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, null, null, 92m, 40m, "OPEN", created, created.AddMinutes(1), 2);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.False(result.IsConsistent);
        Assert.Equal("VERSION_MISMATCH", result.Reason);
    }

    [Fact]
    public void Tampered_cover_pnl_is_detected()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 92m, 5, 41m, 1, created.AddMinutes(1)) };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, null, null, 92m, 41m, "SHORT_CLOSED", created, created.AddMinutes(1), 1);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.False(result.IsConsistent);
        Assert.Equal("INVALID_COVER", result.Reason);
    }

    [Fact]
    public void Consistent_open_order_fill_and_position_reconcile()
    {
        var orderId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();
        var symbol = new Symbol("TEST", "123");
        var now = DateTimeOffset.UtcNow;
        var order = new OrderState(orderId, "short-1", symbol, symbol.InstrumentToken, OrderSide.Sell, 5, 100m, "baseline-v1", now, "paper", "short-open-filled");
        var fill = new FillState(Guid.NewGuid(), orderId, symbol, OrderSide.Sell, 5, 99.5m, now.AddMilliseconds(1), "paper");
        var position = new DurableShortPositionState(orderId, portfolioId, symbol, 5, 5, 99.5m, null, null, null, 0m, "SHORT_OPEN", now.AddMilliseconds(1), now.AddMilliseconds(1), 0);
        var result = DurableShortRecoveryReconciler.ReconcileExecution(order, fill, position);
        Assert.True(result.IsConsistent);
        Assert.Equal("CONSISTENT", result.Reason);
    }

    [Fact]
    public void Execution_position_identity_mismatch_is_detected()
    {
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TEST", "123");
        var now = DateTimeOffset.UtcNow;
        var order = new OrderState(orderId, "short-1", symbol, symbol.InstrumentToken, OrderSide.Sell, 5, 100m, "baseline-v1", now, "paper", "short-open-filled");
        var fill = new FillState(Guid.NewGuid(), orderId, symbol, OrderSide.Sell, 5, 99.5m, now.AddMilliseconds(1), "paper");
        var position = new DurableShortPositionState(Guid.NewGuid(), Guid.NewGuid(), symbol, 5, 5, 99.5m, null, null, null, 0m, "SHORT_OPEN", now.AddMilliseconds(1), now.AddMilliseconds(1), 0);
        var result = DurableShortRecoveryReconciler.ReconcileExecution(order, fill, position);
        Assert.False(result.IsConsistent);
        Assert.Equal("EXECUTION_POSITION_ID_MISMATCH", result.Reason);
    }

    [Fact]
    public void Live_execution_mode_is_rejected_by_short_reconciliation()
    {
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TEST", "123");
        var now = DateTimeOffset.UtcNow;
        var order = new OrderState(orderId, "short-1", symbol, symbol.InstrumentToken, OrderSide.Sell, 5, 100m, "baseline-v1", now, "live", "short-open-filled");
        var fill = new FillState(Guid.NewGuid(), orderId, symbol, OrderSide.Sell, 5, 99.5m, now.AddMilliseconds(1), "live");
        var position = new DurableShortPositionState(orderId, Guid.NewGuid(), symbol, 5, 5, 99.5m, null, null, null, 0m, "SHORT_OPEN", now.AddMilliseconds(1), now.AddMilliseconds(1), 0);
        var result = DurableShortRecoveryReconciler.ReconcileExecution(order, fill, position);
        Assert.False(result.IsConsistent);
        Assert.Equal("INVALID_SHORT_ORDER", result.Reason);
    }

    [Fact]
    public void Live_fill_provider_is_rejected_even_when_order_is_marked_paper()
    {
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TEST", "123");
        var now = DateTimeOffset.UtcNow;
        var order = new OrderState(orderId, "short-1", symbol, symbol.InstrumentToken, OrderSide.Sell, 5, 100m, "baseline-v1", now, "paper", "short-open-filled");
        var fill = new FillState(Guid.NewGuid(), orderId, symbol, OrderSide.Sell, 5, 99.5m, now.AddMilliseconds(1), "live");
        var position = new DurableShortPositionState(orderId, Guid.NewGuid(), symbol, 5, 5, 99.5m, null, null, null, 0m, "SHORT_OPEN", now.AddMilliseconds(1), now.AddMilliseconds(1), 0);
        var result = DurableShortRecoveryReconciler.ReconcileExecution(order, fill, position);
        Assert.False(result.IsConsistent);
        Assert.Equal("INVALID_SHORT_FILL", result.Reason);
    }
}
