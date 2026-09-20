using System.Collections.Concurrent;
using AiTrading.Domain;

namespace AiTrading.Application;

/// <summary>Durable paper execution boundary. IdempotencyKey identifies one client execution attempt.</summary>
public sealed class DurablePaperTradingService(
    RecommendationService recommendations,
    RiskEngine risk,
    IPaperExecutionProvider execution,
    ITradingUnitOfWorkFactory unitOfWorkFactory,
    decimal startingCash) : IPaperTradeService
{
    private sealed record InFlightExecution(Guid PortfolioId, Guid OrderId, Symbol Symbol, int Quantity, RiskResult Risk, FillState? Fill);
    private static readonly ConcurrentDictionary<string, TaskCompletionSource<InFlightExecution>> InFlight = new(StringComparer.Ordinal);

    public async Task<(RiskResult Risk, FillState? Fill)> ExecuteAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
            throw new ArgumentException("A non-empty idempotency key of at most 128 characters is required.", nameof(idempotencyKey));
        if (startingCash <= 0)
            throw new InvalidOperationException("Starting cash must be positive.");
        cancellationToken.ThrowIfCancellationRequested();

        var completion = new TaskCompletionSource<InFlightExecution>(TaskCreationOptions.RunContinuationsAsynchronously);
        var registered = InFlight.GetOrAdd(idempotencyKey, completion);
        if (!ReferenceEquals(registered, completion))
        {
            var shared = await registered.Task.WaitAsync(cancellationToken);
            if (shared.PortfolioId != portfolioId)
                throw new InvalidOperationException("The idempotency key is already associated with a different portfolio.");
            if (shared.OrderId != orderId)
                throw new InvalidOperationException("The idempotency key is already associated with a different order.");
            if (shared.Symbol != symbol || shared.Quantity != quantity)
                throw new InvalidOperationException("The idempotency key is already associated with different order inputs.");
            return (shared.Risk, shared.Fill);
        }

        try
        {
            var result = await ExecuteCoreAsync(portfolioId, orderId, idempotencyKey, symbol, quantity, cancellationToken);
            completion.TrySetResult(new InFlightExecution(portfolioId, orderId, symbol, quantity, result.Risk, result.Fill));
            return result;
        }
        catch (Exception ex)
        {
            completion.TrySetException(ex);
            throw;
        }
        finally
        {
            InFlight.TryRemove(new KeyValuePair<string, TaskCompletionSource<InFlightExecution>>(idempotencyKey, completion));
        }
    }

    private async Task<(RiskResult Risk, FillState? Fill)> ExecuteCoreAsync(Guid portfolioId, Guid orderId, string idempotencyKey, Symbol symbol, int quantity, CancellationToken cancellationToken)
    {
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);
        var orderWithSameId = await unitOfWork.Orders.GetAsync(orderId, cancellationToken);
        if (orderWithSameId is not null && orderWithSameId.IdempotencyKey != idempotencyKey)
            throw new InvalidOperationException($"Order {orderId} is already associated with a different idempotency key; execution state requires reconciliation.");

        var existingOrder = await unitOfWork.Orders.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingOrder is not null)
        {
            if (existingOrder.Id != orderId)
                throw new InvalidOperationException("The idempotency key is already associated with a different order.");
            if (existingOrder.Symbol != symbol || existingOrder.Quantity != quantity)
                throw new InvalidOperationException("The idempotency key is already associated with different order inputs.");
            if (!string.Equals(existingOrder.ExecutionMode, "paper", StringComparison.OrdinalIgnoreCase) || !string.Equals(existingOrder.Status, "filled", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Order {orderId} has an invalid persisted paper execution state; execution state requires reconciliation.");
            var existingFill = await unitOfWork.Orders.GetFillByOrderIdAsync(orderId, cancellationToken);
            if (existingFill is null)
                throw new InvalidOperationException($"Order {orderId} exists without a fill; execution state requires reconciliation.");
            if (existingFill.OrderId != orderId || existingFill.Symbol != existingOrder.Symbol || existingFill.Quantity != existingOrder.Quantity || existingFill.Side != existingOrder.Side)
                throw new InvalidOperationException($"Order {orderId} has an inconsistent paper fill; execution state requires reconciliation.");
            if (!string.Equals(existingFill.ExecutionProvider, "paper", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Order {orderId} has a non-paper fill provider; execution state requires reconciliation.");
            if (existingFill.FillPrice <= 0)
                throw new InvalidOperationException($"Order {orderId} has an invalid paper fill price; execution state requires reconciliation.");
            if (existingFill.Timestamp == default)
                throw new InvalidOperationException($"Order {orderId} has an invalid paper fill timestamp; execution state requires reconciliation.");
            return (new(RiskDecision.Approved, null), existingFill);
        }

        var portfolio = await unitOfWork.Portfolios.GetAsync(portfolioId, cancellationToken);
        if (portfolio is null)
        {
            var now = DateTimeOffset.UtcNow;
            portfolio = new PortfolioState(portfolioId, startingCash, 0m, now, 0, startingCash);
            await unitOfWork.Portfolios.SaveAsync(portfolio, 0, cancellationToken);
        }

        var recommendation = await recommendations.GetRecommendationAsync(symbol, cancellationToken);
        var riskResult = risk.Evaluate(recommendation, portfolio.Cash, quantity);
        if (riskResult.Decision != RiskDecision.Approved) return (riskResult, null);

        var order = new PaperOrder(orderId, symbol, OrderSide.Buy, quantity, recommendation.ReferencePrice, DateTimeOffset.UtcNow);
        var fill = await execution.ExecuteAsync(order, cancellationToken);
        if (fill.OrderId != order.Id || fill.Symbol != order.Symbol || fill.Quantity != order.Quantity || fill.Side != order.Side || fill.Price <= 0)
            throw new InvalidOperationException($"Paper execution provider returned an inconsistent fill for order {orderId}; execution state requires reconciliation.");
        var nowFilled = DateTimeOffset.UtcNow;
        var orderState = new OrderState(order.Id, idempotencyKey, order.Symbol, order.Symbol.InstrumentToken, order.Side, order.Quantity, order.LimitPrice, recommendation.StrategyVersion, order.CreatedAt, "paper", "filled");
        var fillState = new FillState(order.Id, fill.OrderId, fill.Symbol, fill.Side, fill.Quantity, fill.Price, fill.Timestamp, "paper");
        await unitOfWork.Orders.AddAsync(orderState, cancellationToken);
        await unitOfWork.Orders.AddFillAsync(fillState, cancellationToken);

        var positions = await unitOfWork.Portfolios.GetOpenPositionsAsync(portfolioId, cancellationToken);
        var existingPosition = positions.SingleOrDefault(x => x.Symbol == symbol);
        var value = fill.Price * fill.Quantity;
        if (value > portfolio.Cash) throw new InvalidOperationException("Insufficient virtual cash at persistence boundary.");
        if (existingPosition is null)
            await unitOfWork.Portfolios.SavePositionAsync(new PositionState(Guid.NewGuid(), portfolioId, symbol, symbol.InstrumentToken, fill.Quantity, fill.Price, fill.Price, null, nowFilled, nowFilled), cancellationToken);
        else
        {
            var totalQuantity = existingPosition.Quantity + fill.Quantity;
            var averageEntry = ((existingPosition.AverageEntryPrice * existingPosition.Quantity) + value) / totalQuantity;
            await unitOfWork.Portfolios.SavePositionAsync(existingPosition with { Quantity = totalQuantity, AverageEntryPrice = averageEntry, CurrentMarketPrice = fill.Price, UpdatedAt = nowFilled }, cancellationToken);
        }
        await unitOfWork.Portfolios.SaveAsync(portfolio with { Cash = portfolio.Cash - value, UpdatedAt = nowFilled }, portfolio.Version, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return (riskResult, fillState);
    }
}
