using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record PaperShortExecutionRequest(
    Guid OrderId,
    string IdempotencyKey,
    Symbol Symbol,
    int Quantity,
    decimal EntryPrice,
    decimal StopLoss,
    decimal TargetPrice,
    string StrategyVersion);

public sealed record PaperShortExecutionResult(
    RiskResult Risk,
    FillState? Fill,
    string Status);

/// <summary>
/// Paper-only short execution boundary. It persists the SELL order/fill ledger and
/// never routes to a live broker. Portfolio short-position accounting remains a
/// separate lifecycle concern so a SELL can never be mistaken for a long position.
/// </summary>
public sealed class DurablePaperShortExecutionService(
    IPaperExecutionProvider execution,
    ITradingUnitOfWorkFactory unitOfWorkFactory)
{
    public async Task<PaperShortExecutionResult> OpenAsync(
        PaperShortExecutionRequest request,
        CancellationToken cancellationToken)
    {
        Validate(request);
        await using var unitOfWork = await unitOfWorkFactory.CreateAsync(cancellationToken);

        var existingOrder = await unitOfWork.Orders.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
        if (existingOrder is not null)
        {
            if (existingOrder.Id != request.OrderId || existingOrder.Side != OrderSide.Sell)
                throw new InvalidOperationException("The idempotency key is already associated with a different order.");
            var existingFill = await unitOfWork.Orders.GetFillByOrderIdAsync(request.OrderId, cancellationToken);
            if (existingFill is null)
                throw new InvalidOperationException($"Order {request.OrderId} exists without a fill; execution state requires reconciliation.");
            return new(new(RiskDecision.Approved, null), existingFill, "already-executed");
        }

        var order = new PaperOrder(
            request.OrderId,
            request.Symbol,
            OrderSide.Sell,
            request.Quantity,
            request.EntryPrice,
            DateTimeOffset.UtcNow);
        var fill = await execution.ExecuteAsync(order, cancellationToken);
        var orderState = new OrderState(
            order.Id,
            request.IdempotencyKey,
            order.Symbol,
            order.Symbol.InstrumentToken,
            order.Side,
            order.Quantity,
            order.LimitPrice,
            request.StrategyVersion,
            order.CreatedAt,
            "paper",
            "short-open-filled");
        var fillState = new FillState(
            Guid.NewGuid(),
            fill.OrderId,
            fill.Symbol,
            fill.Side,
            fill.Quantity,
            fill.Price,
            fill.Timestamp,
            "paper");

        await unitOfWork.Orders.AddAsync(orderState, cancellationToken);
        await unitOfWork.Orders.AddFillAsync(fillState, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
        return new(new(RiskDecision.Approved, null), fillState, "executed");
    }

    private static void Validate(PaperShortExecutionRequest request)
    {
        if (request.OrderId == Guid.Empty) throw new ArgumentException("OrderId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 128)
            throw new ArgumentException("A non-empty idempotency key of at most 128 characters is required.", nameof(request));
        if (request.Quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(request));
        if (request.EntryPrice <= 0) throw new ArgumentException("Entry price must be positive.", nameof(request));
        var risk = PaperShortRiskGate.Validate(request.Quantity, request.EntryPrice, request.StopLoss, request.TargetPrice);
        if (!risk.Approved) throw new InvalidOperationException(risk.Reason);
        if (string.IsNullOrWhiteSpace(request.StrategyVersion)) throw new ArgumentException("StrategyVersion is required.", nameof(request));
    }
}
