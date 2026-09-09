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
        var result = await paperTrades.ExecuteAsync(DeterministicGuid($"portfolio:{request.SessionId:N}"), orderId, key, request.Symbol, request.Quantity, ct);
        if (unitOfWorkFactory is not null)
        {
            await using var uow = await unitOfWorkFactory.CreateAsync(ct);
            var existing = await uow.PaperTradingEventAudits.GetBySessionAsync(request.SessionId, ct);
            if (!existing.Any(x => x.EventId == request.EventId))
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
