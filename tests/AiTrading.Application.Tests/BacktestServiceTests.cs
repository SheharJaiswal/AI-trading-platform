using AiTrading.Application;
using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class BacktestServiceTests
{
    [Fact]
    public async Task Service_rejects_reversed_date_range_without_mutating_execution_state()
    {
        var service = new BacktestService(new ThrowingUnitOfWorkFactory(), new DeterministicBacktestEngine(new DeterministicRecommendationEngine(), new RiskEngine()));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RunAsync(new BacktestRunRequest(new Symbol("TEST"), "1d", DateTimeOffset.Parse("2026-02-02T00:00:00Z"), DateTimeOffset.Parse("2026-02-01T00:00:00Z"), new BacktestConfiguration(10000, 1, 0, 0)), CancellationToken.None));
    }

    [Fact]
    public async Task Service_rejects_unsupported_strategy_before_persistence()
    {
        var service = new BacktestService(new ThrowingUnitOfWorkFactory(), new DeterministicBacktestEngine(new DeterministicRecommendationEngine(), new RiskEngine()));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RunAsync(new BacktestRunRequest(new Symbol("TEST"), "1d", DateTimeOffset.Parse("2026-02-01T00:00:00Z"), DateTimeOffset.Parse("2026-02-02T00:00:00Z"), new BacktestConfiguration(10000, 1, 0, 0, "future-v2")), CancellationToken.None));
    }

    [Fact]
    public async Task Service_rejects_empty_symbol_before_persistence()
    {
        var service = new BacktestService(new ThrowingUnitOfWorkFactory(), new DeterministicBacktestEngine(new DeterministicRecommendationEngine(), new RiskEngine()));
        await Assert.ThrowsAsync<ArgumentException>(() => service.RunAsync(new BacktestRunRequest(new Symbol(""), "1d", DateTimeOffset.Parse("2026-02-01T00:00:00Z"), DateTimeOffset.Parse("2026-02-02T00:00:00Z"), new BacktestConfiguration(10000, 1, 0, 0)), CancellationToken.None));
    }

    private sealed class ThrowingUnitOfWorkFactory : ITradingUnitOfWorkFactory
    {
        public Task<ITradingUnitOfWork> CreateAsync(CancellationToken cancellationToken) => throw new InvalidOperationException("Persistence should not be reached for invalid input.");
    }
}
