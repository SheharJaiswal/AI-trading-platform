using AiTrading.Application;
using AiTrading.Domain;
using Moq;

namespace AiTrading.Application.Tests;

public sealed class DurablePaperTradingPersistedFillIntegrityTests
{
    [Fact]
    public async Task Persisted_Idempotent_Replay_With_NonPaper_Fill_Provider_Fails_Closed()
    {
        var portfolioId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var symbol = new Symbol("TCS", "123");
        var order = new OrderState(orderId, "persisted-non-paper-fill", symbol, symbol.InstrumentToken, OrderSide.Buy, 1, 100m, "strategy", DateTimeOffset.UtcNow, "paper", "filled");
        var fill = new FillState(orderId, orderId, symbol, OrderSide.Buy, 1, 100m, DateTimeOffset.UtcNow, "broker");

        var orders = new Mock<IOrderRepository>();
        orders.Setup(x => x.GetAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        orders.Setup(x => x.GetByIdempotencyKeyAsync(order.IdempotencyKey, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        orders.Setup(x => x.GetFillByOrderIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(fill);

        var unitOfWork = new Mock<ITradingUnitOfWork>();
        unitOfWork.SetupGet(x => x.Orders).Returns(orders.Object);
        var factory = new Mock<ITradingUnitOfWorkFactory>();
        factory.Setup(x => x.CreateAsync(It.IsAny<CancellationToken>())).ReturnsAsync(unitOfWork.Object);

        var execution = new Mock<IPaperExecutionProvider>();
        var service = new DurablePaperTradingService(
            new RecommendationService(new FakeMarketData()),
            new RiskEngine(),
            execution.Object,
            factory.Object,
            10_000m);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExecuteAsync(
            portfolioId, orderId, order.IdempotencyKey, symbol, 1, CancellationToken.None));

        Assert.Equal($"Order {orderId} has a non-paper fill provider; execution state requires reconciliation.", error.Message);
        execution.Verify(x => x.ExecuteAsync(It.IsAny<PaperOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class FakeMarketData : IMarketDataProvider
    {
        public Task<MarketQuote> GetQuoteAsync(Symbol symbol, CancellationToken cancellationToken) =>
            Task.FromResult(new MarketQuote(symbol, "NSE", symbol.InstrumentToken ?? "123", DateTimeOffset.UtcNow, 100, 101, 99, 101, 101, 1000, "test"));

        public Task<IReadOnlyList<Candle>> GetCandlesAsync(Symbol symbol, DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Candle>>([]);
    }
}
