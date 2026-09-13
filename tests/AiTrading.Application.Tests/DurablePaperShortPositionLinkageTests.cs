using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class DurablePaperShortPositionLinkageTests
{
    [Fact]
    public async Task OpenAsync_ReplaysSamePositionWithoutCreatingDuplicate()
    {
        var repository = new InMemoryShortPositionRepository();
        var service = new DurablePaperShortCoverService(repository);
        var positionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var portfolioId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var symbol = new Symbol("DEMO", "123");
        var now = DateTimeOffset.Parse("2026-09-14T00:00:00Z");

        var first = await service.OpenAsync(positionId, portfolioId, symbol, 2, 100m, now, CancellationToken.None);
        var replay = await service.OpenAsync(positionId, portfolioId, symbol, 2, 100m, now.AddSeconds(5), CancellationToken.None);

        Assert.Equal(first, replay);
        Assert.Equal(1, repository.AddCount);
    }

    [Fact]
    public async Task OpenAsync_RejectsPositionIdReuseWithDifferentDetails()
    {
        var repository = new InMemoryShortPositionRepository();
        var service = new DurablePaperShortCoverService(repository);
        var positionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var portfolioId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var symbol = new Symbol("DEMO");

        await service.OpenAsync(positionId, portfolioId, symbol, 1, 100m, DateTimeOffset.UtcNow, CancellationToken.None);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.OpenAsync(positionId, portfolioId, symbol, 2, 100m, DateTimeOffset.UtcNow, CancellationToken.None));
        Assert.Contains("different short-position details", error.Message);
    }

    private sealed class InMemoryShortPositionRepository : IDurableShortPositionRepository
    {
        private readonly Dictionary<Guid, DurableShortPositionState> positions = new();
        public int AddCount { get; private set; }

        public Task<DurableShortPositionState?> GetAsync(Guid positionId, CancellationToken cancellationToken) =>
            Task.FromResult(positions.GetValueOrDefault(positionId));

        public Task<IReadOnlyList<DurableShortCoverState>> GetCoversAsync(Guid positionId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<DurableShortCoverState>>([]);

        public Task AddAsync(DurableShortPositionState position, CancellationToken cancellationToken)
        {
            AddCount++;
            if (!positions.TryAdd(position.Id, position)) throw new InvalidOperationException("duplicate");
            return Task.CompletedTask;
        }

        public Task<DurableShortPositionState> ApplyCoverAsync(Guid positionId, string idempotencyKey, decimal coverPrice, int coverQuantity, long expectedVersion, DateTimeOffset now, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
