using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurableShortRecoveryServiceTests
{
    [Fact]
    public async Task Reconcile_loads_position_and_cover_ledger_and_returns_consistent()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, 92m, 40m, "SHORT_CLOSED", created, created.AddMinutes(1), 1);
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 92m, 5, 40m, 1, created.AddMinutes(1)) };
        var repository = new FakeRepository(position, covers);

        var result = await new DurableShortRecoveryService(repository).ReconcileAsync(positionId, CancellationToken.None);

        Assert.True(result.IsConsistent);
        Assert.Equal("CONSISTENT", result.Reason);
        Assert.True(repository.CoversRequested);
    }

    [Fact]
    public async Task Reconcile_surfaces_ledger_tampering_as_deterministic_mismatch()
    {
        var positionId = Guid.NewGuid();
        var created = DateTimeOffset.UtcNow;
        var position = new DurableShortPositionState(positionId, Guid.NewGuid(), new Symbol("TEST"), 5, 0, 100m, 92m, 40m, "SHORT_CLOSED", created, created.AddMinutes(1), 1);
        var covers = new[] { new DurableShortCoverState(Guid.NewGuid(), positionId, "cover-1", 92m, 5, 41m, 1, created.AddMinutes(1)) };

        var result = await new DurableShortRecoveryService(new FakeRepository(position, covers)).ReconcileAsync(positionId, CancellationToken.None);

        Assert.False(result.IsConsistent);
        Assert.Equal("INVALID_COVER", result.Reason);
    }

    [Fact]
    public async Task Reconcile_throws_not_found_for_missing_position()
    {
        var missingId = Guid.NewGuid();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new DurableShortRecoveryService(new FakeRepository(null, [])).ReconcileAsync(missingId, CancellationToken.None));
    }

    private sealed class FakeRepository(DurableShortPositionState? position, IReadOnlyList<DurableShortCoverState> covers) : IDurableShortPositionRepository
    {
        public bool CoversRequested { get; private set; }
        public Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult(position?.Id == positionId ? position : null);
        public Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken cancellationToken)
        {
            CoversRequested = true;
            return Task.FromResult<IReadOnlyList<DurableShortCoverState>>(covers.Where(x => x.PositionId == positionId).ToArray());
        }
        public Task AddAsync(DurableShortPositionState value, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
