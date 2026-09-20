using System.Security.Cryptography;
using System.Text;
using AiTrading.Domain;

namespace AiTrading.Application;

public sealed record PaperTradingEventRequest(Guid SessionId, Symbol Symbol, int Quantity, string EventId);
public sealed record PaperTradingEventResponse(Guid SessionId, string EventId, RiskResult Risk, FillState? Fill, string ExecutionMode);

public sealed class PaperTradingSessionExecutionService(IPaperTradingSessionService sessions, IPaperTradeService paperTrades, ITradingUnitOfWorkFactory? unitOfWorkFactory = null)
{
    public async Task<PaperTradingEventResponse> ProcessAsync(PaperTradingEventRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.EventId) || request.EventId.Length > 128)
            throw new ArgumentException("EventId is required and must be 1-128 characters.", nameof(request.EventId));
        if (request.Quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(request.Quantity));
        var session = await sessions.GetAsync(request.SessionId, ct) ?? throw new KeyNotFoundException($"Paper trading session {request.SessionId} does not exist.");
        if (session.Status != PaperTradingSessionStatus.Running)
            throw new InvalidOperationException($"Paper trading session must be Running before processing market events; current status is {session.Status}.");
        if (!session.Configuration.Symbols.Contains(request.Symbol))
            throw new InvalidOperationException("Symbol is not part of the paper trading session configuration.");
        var key = $"session:{request.SessionId:N}:event:{request.EventId}";
        var orderId = DeterministicGuid(key);

        if (unitOfWorkFactory is not null)
        {
            await using var replayUow = await unitOfWorkFactory.CreateAsync(ct);
            var existing = await replayUow.PaperTradingEventAudits.GetBySessionAndEventAsync(request.SessionId, request.EventId, ct);
            if (existing is not null)
            {
                if (existing.Symbol != request.Symbol || existing.Quantity != request.Quantity)
                    throw new InvalidOperationException("Persisted paper event audit does not match the replay request.");
                if (!Enum.TryParse<RiskDecision>(existing.RiskDecision, out var decision))
                    throw new InvalidOperationException("Persisted paper event audit contains an invalid risk decision.");
                var existingFill = await replayUow.Orders.GetFillByOrderIdAsync(existing.OrderId, ct);
                if (decision == RiskDecision.Approved && existingFill is null)
                    throw new InvalidOperationException("Persisted paper event audit is approved but its paper fill is missing.");
                if (decision != RiskDecision.Approved && existingFill is not null)
                    throw new InvalidOperationException("Persisted paper event audit is blocked but has a paper fill.");
                if (existingFill is not null &&
                    (existingFill.OrderId != existing.OrderId || existingFill.Symbol != existing.Symbol || existingFill.Quantity != existing.Quantity ||
                     !existing.FillPrice.HasValue || existingFill.FillPrice != existing.FillPrice.Value))
                    throw new InvalidOperationException("Persisted paper event audit fill does not match the persisted audit state.");
                return new(request.SessionId, request.EventId, new RiskResult(decision, string.IsNullOrEmpty(existing.RiskReason) ? null : existing.RiskReason), existingFill, "PAPER_ONLY");
            }
        }

        var result = await paperTrades.ExecuteAsync(DeterministicGuid($"portfolio:{request.SessionId:N}"), orderId, key, request.Symbol, request.Quantity, ct);
        if (unitOfWorkFactory is not null)
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(ct);
            var existing = await uow.PaperTradingEventAudits.GetBySessionAndEventAsync(request.SessionId, request.EventId, ct);
            if (existing is null)
            {
                await uow.PaperTradingEventAudits.AddAsync(new PaperTradingEventAuditState(Guid.NewGuid(), request.SessionId, request.EventId, orderId, request.Symbol, request.Quantity, result.Risk.Decision.ToString(), result.Risk.Reason ?? string.Empty, result.Fill?.FillPrice, DateTimeOffset.UtcNow), ct);
                await uow.CommitAsync(ct);
            }
        }
        return new(request.SessionId, request.EventId, result.Risk, result.Fill, "PAPER_ONLY");
    }

    private static Guid DeterministicGuid(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(hash.AsSpan(0, 16));
    }
}
