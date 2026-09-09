using AiTrading.Domain;

namespace AiTrading.Application;

public interface IAlertDelivery
{
    Task DeliverAsync(Alert alert, CancellationToken cancellationToken);
}

/// <summary>
/// Safe default: alerts are durably persisted and exposed through the API, but no external
/// notification channel is enabled until the V1 channel decision is made.
/// </summary>
public sealed class NoopAlertDelivery : IAlertDelivery
{
    public Task DeliverAsync(Alert alert, CancellationToken cancellationToken) => Task.CompletedTask;
}
