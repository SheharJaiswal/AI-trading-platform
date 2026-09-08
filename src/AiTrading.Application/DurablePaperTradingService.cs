using AiTrading.Domain;

namespace AiTrading.Application;

public interface IPaperTradeService
{
    Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken);
}

public sealed class DurablePaperTradingService(
    RecommendationService recommendations,
    RiskEngine risk,
    IPaperExecutionProvider execution,
    ITradingUnitOfWorkFactory unitOfWorkFactory,
    decimal startingCash = 1_000_000m) : IPaperTradeService
{
    public async Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
            throw new ArgumentException("A non-empty idempotency key of at most 128 characters is required.", nameof(idempotencyKey));

        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var existingOrder = await unitOfWork.Orders.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingOrder is not null)
        {
            if (existingOrder.Id != orderId)
                throw new InvalidOperationException("The idempotency key is already associated with a different order.");
            var existingFill = await unitOfWork.Orders.GetFillByOrderIdAsync(orderId, cancellationToken);
            if (existingFill is null)
                throw new InvalidOperationException($"Order {orderId} exists without a fill; execution state requires reconciliation.");
            return (new(RiskDecision.Approved, null), existingFill);
        }

        var now = DateTimeOffset.UtcNow;
        var portfolio = await unitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken)
            ?? new PortfolioState(portfolioId, startingCash, 0m, now, 0);

        var recommendation = await recommendations.GetRecommendationAsync(symbol, cancellationToken);
        var riskResult = risk.Evaluate(recommendation, portfolio.Cash, quantity);
        if (riskResult.Decision != RiskDecision.Approved) return (riskResult, null);

        var order = new PaperOrder(orderId, symbol, OrderSide.Buy, quantity, recommendation.ReferencePrice, now);
        var fill = await execution.ExecuteAsync(order, cancellationToken);
        var fillValue = fill.Price * fill.Quantity;
        if (fillValue > portfolio.Cash)
            throw new InvalidOperationException("Insufficient virtual cash at persistence boundary.");

        var orderState = new OrderState(order.Id, idempotencyKey, order.Symbol, order.Symbol.InstrumentToken, order.Side, order.Quantity, order.LimitPrice, recommendation.StrategyVersion, order.CreatedAt, "paper", "filled");
        var fillState = new FillState(order.Id, fill.OrderId, fill.Symbol, fill.Side, fill.Quantity, fill.Price, fill.Timestamp, "paper");
        await unitOfWork.Orders.AddAsync(orderState, cancellationToken);
        await unitOfWork.Orders.AddFillAsync(fillState, cancellationToken);

        var positions = await unitOfWork.Portfolios.GetOpenPositionsAsync(portfolioId, cancellationToken);
        var existingPosition = positions.SingleOrDefault(x => x.Symbol == symbol);
        if (existingPosition is null)
            await unitOfWork.Portfolios.SavePositionAsync(new PositionState(Guid.NewGuid(), portfolioId, symbol, symbol.InstrumentToken, fill.Quantity, fill.Price, fill.Price, null, now, now), cancellationToken);
        else
        {
            var totalQuantity = existingPosition.Quantity + fill.Quantity;
            var averageEntry = ((existingPosition.AverageEntryPrice * existingPosition.Quantity) + fillValue) / totalQuantity;
            await unitOfWork.Portfolios.SavePositionAsync(existingPosition with { Quantity = totalQuantity, AverageEntryPrice = averageEntry, CurrentMarketPrice = fill.Price, UpdatedAt = now }, cancellationToken);
        }

        await unitOfWork.Portfolios.SaveAsync(portfolio with { Cash = portfolio.Cash - fillValue, UpdatedAt = now }, portfolio.Version, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return (riskResult, fillState);
    }
}

public sealed class DurablePortfolioQueryService(ITradingUnitOfWorkFactory unitOfWorkFactory)
{
    public async Task<Portfolio?> GetAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var state = await unitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken);
        if (state is null) return null;
        var positions = await unitOfWork.Portfolios.GetOpenPositionsAsync(portfolioId, cancellationToken);
        var domainPositions = positions.Select(x => new Position(x.Id, x.Symbol, x.Quantity, x.AverageEntryPrice, x.StopLoss)).ToArray();
        var unrealized = positions.Sum(x => (x.CurrentMarketPrice - x.AverageEntryPrice) * x.Quantity);
        return new Portfolio(state.Cash, domainPositions, unrealized, state.RealizedPnl);
    }
}

public sealed class DurableAlertQueryService(ITradingUnitOfWorkFactory unitOfWorkFactory)
{
    public async Task<IReadOnlyList<AlertState>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        return await unitOfWork.Alerts.GetAllAsync(cancellationToken);
    }
}
