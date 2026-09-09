using AiTrading.Domain;

namespace AiTrading.Application.Tests;

public sealed class AlertDeliveryTests
{
    [Fact]
    public async Task NoopDelivery_has_no_external_side_effect()
    {
        var delivery = new NoopAlertDelivery();
        var alert = new Alert(
            "position:STOP_LOSS:1",
            AlertSeverity.High,
            "Stop loss breached.",
            DateTimeOffset.UtcNow,
            new Symbol("TEST"));

        await delivery.DeliverAsync(alert, CancellationToken.None);
    }
}
