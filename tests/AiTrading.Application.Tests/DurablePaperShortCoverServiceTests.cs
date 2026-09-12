using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurablePaperShortCoverServiceTests
{
    [Fact]
    public async Task Open_then_full_cover_persists_closed_state()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);
        var now = DateTimeOffset.UtcNow;
        var opened = await service.OpenAsync(Guid.NewGuid(), new Symbol("TEST"), 5, 100m, now, CancellationToken.None);

        var covered = await service.CoverAsync(opened.Id, "cover-1", 92m, 5, opened.Version, now.AddMinutes(1), CancellationToken.None);

        Assert.Equal(0, covered.RemainingQuantity);
        Assert.Equal(40m, covered.RealizedPnl);
        Assert.Equal("SHORT_CLOSED", covered.State);
        Assert.Equal(1, covered.Version);
        Assert.Equal("cover-1", repository.LastKey);
    }

    [Fact]
    public async Task Partial_cover_preserves_remaining_quantity_and_accumulates_pnl()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);
        var opened = await service.OpenAsync(Guid.NewGuid(), new Symbol("TEST"), 10, 100m, DateTimeOffset.UtcNow, CancellationToken.None);

        var covered = await service.CoverAsync(opened.Id, "cover-1", 96m, 4, opened.Version, DateTimeOffset.UtcNow, CancellationToken.None);

        Assert.Equal(6, covered.RemainingQuantity);
        Assert.Equal(16m, covered.RealizedPnl);
        Assert.Equal("SHORT_PARTIALLY_COVERED", covered.State);
    }

    [Fact]
    public async Task Invalid_cover_values_are_rejected_before_repository_call()
    {
        var repository = new FakeRepository();
        var service = new DurablePaperShortCoverService(repository);
        var opened = await service.OpenAsync(Guid.NewGuid(), new Symbol("TEST"), 2, 100m, DateTimeOffset.UtcNow, CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CoverAsync(opened.Id, "cover-1", 0m, 1, opened.Version, DateTimeOffset.UtcNow, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CoverAsync(opened.Id, "cover-2", 95m, 0, opened.Version, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Null(repository.LastKey);
    }

    private sealed class FakeRepository : IDurableShortPositionRepository
    {
        private DurableShortPositionState? position;
        public string? LastKey { get; private set; }
        public Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult(position?.Id == positionId ? position : null);
        public Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<DurableShortCoverState>>([]);
        public Task AddAsync(DurableShortPositionState value, CancellationToken cancellationToken) { position = value; return Task.CompletedTask; }
        public Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken)
        {
            LastKey = idempotencyKey;
            if (position is null) throw new KeyNotFoundException();
            if (position.Version != expectedVersion) throw new InvalidOperationException("stale");
            var pnl = PaperShortAccounting.RealizedPnl(position.AverageEntryPrice, coverPrice, coverQuantity);
            var remaining = position.RemainingQuantity - coverQuantity;
            position = position with { RemainingQuantity = remaining, LastCoverPrice = coverPrice, RealizedPnl = position.RealizedPnl + pnl, State = remaining == 0 ? "SHORT_CLOSED" : "SHORT_PARTIALLY_COVERED", UpdatedAt = now, Version = position.Version + 1 };
            return Task.FromResult(position);
        }
    }
}
