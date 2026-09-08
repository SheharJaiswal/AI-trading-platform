using AiTrading.Domain;

namespace AiTrading.Application;

/// <summary>Durable paper execution boundary. IdempotencyKey identifies one client execution attempt.</summary>
public sealed class DurablePaperTradingService(
    RecommendationService recommendations,
    RiskEngine risk,
    IPaperExecutionProvider execution,
    ITradingUnitOfWorkFactory unitOfWorkFactory)
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

        var portfolio = await unitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken)
            ?? throw new InvalidOperationException($"Portfolio {portfolioId} does not exist.");
        var recommendation = await recommendations.GetRecommendationAsync(symbol, cancellationToken);
        var riskResult = risk.Evaluate(recommendation, portfolio.Cash, quantity);
        if (riskResult.Decision != RiskDecision.Approved) return (riskResult, null);

        var order = new PaperOrder(orderId, symbol, OrderSide.Buy, quantity, recommendation.ReferencePrice, DateTimeOffset.UtcNow);
        var fill = await execution.ExecuteAsync(order, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var orderState = new OrderState(order.Id, idempotencyKey, order.Symbol, order.Symbol.InstrumentToken, order.Side, order.Quantity, order.LimitPrice, recommendation.StrategyVersion, order.CreatedAt, "paper", "filled");
        var fillState = new FillState(fill.Id, fill.OrderId, fill.Symbol, fill.Side, fill.Quantity, fill.Price, fill.Timestamp, "paper");
        await unitOfWork.Orders.AddAsync(orderState, cancellationToken);
        await unitOfWork.Orders.AddFillAsync(fillState, cancellationToken);

        var positions = await unitOfWork.Portfolios.GetOpenPositionsAsync(portfolioId, cancellationToken);
        var existingPosition = positions.SingleOrDefault(x => x.Symbol == symbol);
        var value = fill.Price * fill.Quantity;
        if (value > portfolio.Cash) throw new InvalidOperationException("Insufficient virtual cash at persistence boundary.");
        if (existingPosition is null)
            await unitOfWork.Portfolios.SavePositionAsync(new PositionState(Guid.NewGuid(), portfolioId, symbol, symbol.InstrumentToken, fill.Quantity, fill.Price, fill.Price, null, now, now), cancellationToken);
        else
        {
            var totalQuantity = existingPosition.Quantity + fill.Quantity;
            var averageEntry = ((existingPosition.AverageEntryPrice * existingPosition.Quantity) + value) / totalQuantity;
            await unitOfWork.Portfolios.SavePositionAsync(existingPosition with { Quantity = totalQuantity, AverageEntryPrice = averageEntry, CurrentMarketPrice = fill.Price, UpdatedAt = now }, cancellationToken);
        }
        await unitOfWork.Portfolios.SaveAsync(portfolio with { Cash = portfolio.Cash - value, UpdatedAt = now }, portfolio.Version, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return (riskResult, fillState);
    }
}
