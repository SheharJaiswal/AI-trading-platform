using AiTrading.Domain;

namespace AiTrading.Application;

public enum ExecutionProviderStatus
{
    Submitted,
    PartiallyFilled,
    Filled,
    Rejected,
    Unknown
}

public sealed record ExecutionRequest(
    ExecutionContext Context,
    Guid OrderId,
    string IdempotencyKey,
    string AccountContext,
    Symbol Symbol,
    OrderSide Side,
    int Quantity,
    decimal LimitPrice,
    string? EnvironmentContext = null);

public sealed record ExecutionResult(
    ExecutionProviderStatus Status,
    string Provider,
    Guid OrderId,
    string IdempotencyKey,
    Fill? Fill,
    string? Reason,
    bool ReconciliationRequired);

public interface IExecutionProvider
{
    string Name { get; }

    Task<ExecutionResult> ExecuteAsync(
        ExecutionRequest request,
        CancellationToken cancellationToken);
}

public static class ExecutionProviderContract
{
    public static void ValidateRequest(ExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request.Context);

        if (request.OrderId == Guid.Empty)
            throw new ArgumentException("OrderId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || request.IdempotencyKey.Length > 128)
            throw new ArgumentException("A non-empty idempotency key of at most 128 characters is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.AccountContext))
            throw new ArgumentException("Account context is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.Symbol.Value))
            throw new ArgumentException("Symbol is required.", nameof(request));
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be positive.", nameof(request));
        if (request.LimitPrice <= 0)
            throw new ArgumentException("Limit price must be positive.", nameof(request));

        if (request.Context.Mode is ExecutionMode.Backtest or ExecutionMode.Research)
            throw new InvalidOperationException("Historical and research workflows cannot submit execution requests.");

        if (request.Context.Mode == ExecutionMode.Live)
        {
            if (!request.Context.ExplicitlyEnabled)
                throw new InvalidOperationException("Live execution requires explicit operator enablement.");
            if (string.IsNullOrWhiteSpace(request.EnvironmentContext))
                throw new InvalidOperationException("Live execution requires explicit environment context.");
        }
    }

    public static ExecutionResult CreateUnknown(
        ExecutionRequest request,
        string provider,
        string reason)
    {
        ValidateRequest(request);
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentException("Provider is required.", nameof(provider));
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason is required.", nameof(reason));

        return new(
            ExecutionProviderStatus.Unknown,
            provider,
            request.OrderId,
            request.IdempotencyKey,
            null,
            reason,
            true);
    }

    public static void ValidateResult(ExecutionRequest request, ExecutionResult result)
    {
        ValidateRequest(request);
        ArgumentNullException.ThrowIfNull(result);

        if (result.OrderId != request.OrderId)
            throw new InvalidOperationException("Execution result order identity does not match the request.");
        if (!string.Equals(result.IdempotencyKey, request.IdempotencyKey, StringComparison.Ordinal))
            throw new InvalidOperationException("Execution result idempotency key does not match the request.");
        if (string.IsNullOrWhiteSpace(result.Provider))
            throw new InvalidOperationException("Execution result provider is required.");

        if (result.Status == ExecutionProviderStatus.Unknown && !result.ReconciliationRequired)
            throw new InvalidOperationException("Unknown execution outcomes require reconciliation.");
        if (result.Status == ExecutionProviderStatus.Rejected && result.Fill is not null)
            throw new InvalidOperationException("Rejected execution cannot contain a fill.");
        if (result.Status is ExecutionProviderStatus.PartiallyFilled or ExecutionProviderStatus.Filled && result.Fill is null)
            throw new InvalidOperationException("Filled execution outcomes require a fill.");
    }
}
