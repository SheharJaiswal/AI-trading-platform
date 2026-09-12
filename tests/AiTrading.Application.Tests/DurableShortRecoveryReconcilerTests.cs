using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurableShortRecoveryReconcilerTests
{
    [Fact]
    public void Consistent_full_cover_state_reconciles()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 92m, 5, 40m, 1, created.AddMinutes(1)) };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, 92m, 40m, "SHORT_CLOSED", created, created.AddMinutes(1), 1);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.True(result.IsConsistent);
        Assert.Equal("CONSISTENT", result.Reason);
    }

    [Fact]
    public void Untouched_short_position_reconciles_as_open_version_zero()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 5, 100m, null, 0m, "SHORT_OPEN", created, created, 0);
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
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 10, 6, 100m, 96m, 16m, "SHORT_PARTIALLY_COVERED", created, created.AddMinutes(1), 1);
        Assert.True(DurableShortRecoveryReconciler.Reconcile(position, covers).IsConsistent);
    }

    [Fact]
    public void Non_sequential_cover_versions_are_detected()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[]
        {
            new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 95m, 1, 5m, 1, created.AddMinutes(1)),
            new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-2", 94m, 1, 6m, 3, created.AddMinutes(2))
        };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 2, 0, 100m, 94m, 11m, "SHORT_CLOSED", created, created.AddMinutes(2), 2);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.False(result.IsConsistent);
        Assert.Equal("COVER_VERSION_MISMATCH", result.Reason);
    }

    [Fact]
    public void Duplicate_idempotency_keys_are_detected()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var covers = new[]
        {
            new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 95m, 1, 5m, 1, created.AddMinutes(1)),
            new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 94m, 1, 6m, 2, created.AddMinutes(2))
        };
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 2, 0, 100m, 94m, 11m, "SHORT_CLOSED", created, created.AddMinutes(2), 2);
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
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, 92m, 40m, "OPEN", created, created.AddMinutes(1), 2);
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
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, 92m, 41m, "SHORT_CLOSED", created, created.AddMinutes(1), 1);
        var result = DurableShortRecoveryReconciler.Reconcile(position, covers);
        Assert.False(result.IsConsistent);
        Assert.Equal("INVALID_COVER", result.Reason);
    }
}
