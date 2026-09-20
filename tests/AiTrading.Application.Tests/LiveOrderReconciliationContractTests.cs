namespace AiTrading.Application.Tests;

public sealed class LiveOrderReconciliationContractTests
{
    private static LiveOrderState Local(LiveOrderStatus status = LiveOrderStatus.Submitted) => new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "live-execution-123",
        "account-1",
        "future-broker",
        status,
        "broker-order-1",
        status == LiveOrderStatus.Unknown,
        null,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow,
        2);

    private static LiveOrderReconciliationSnapshot Broker(LiveOrderStatus status = LiveOrderStatus.Submitted) => new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "live-execution-123",
        "future-broker",
        "broker-order-1",
        status,
        0,
        null,
        DateTimeOffset.UtcNow);

    [Fact]
    public void Matching_Identity_And_Status_Is_Matched()
    {
        Assert.Equal(LiveOrderReconciliationOutcome.Matched,
            LiveOrderReconciliationContract.Compare(Local(), Broker()));
    }

    [Fact]
    public void Matching_State_Produces_NonReconciliation_Decision()
    {
        var broker = Broker();
        var decision = LiveOrderReconciliationContract.Decide(Local(), broker);

        Assert.Equal(LiveOrderReconciliationOutcome.Matched, decision.Outcome);
        Assert.Equal(broker.Status, decision.BrokerStatus);
        Assert.False(decision.ReconciliationRequired);
        Assert.Null(decision.Reason);
        Assert.Equal(broker.ProviderOrderId, decision.ProviderOrderId);
        Assert.Equal(broker.ObservedAt, decision.ObservedAt);
    }

    [Fact]
    public void Mismatched_Idempotency_Is_Divergent()
    {
        var broker = Broker() with { IdempotencyKey = "different-key" };
        Assert.Equal(LiveOrderReconciliationOutcome.Divergent,
            LiveOrderReconciliationContract.Compare(Local(), broker));
    }

    [Fact]
    public void Mismatched_Provider_Order_Id_Is_Divergent_With_Reason()
    {
        var broker = Broker() with { ProviderOrderId = "different-provider-order" };
        var decision = LiveOrderReconciliationContract.Decide(Local(), broker);

        Assert.Equal(LiveOrderReconciliationOutcome.Divergent, decision.Outcome);
        Assert.True(decision.ReconciliationRequired);
        Assert.Contains("Provider order identity", decision.Reason);
    }

    [Fact]
    public void Mismatched_Status_Is_Divergent()
    {
        Assert.Equal(LiveOrderReconciliationOutcome.Divergent,
            LiveOrderReconciliationContract.Compare(Local(), Broker(LiveOrderStatus.Rejected)));
    }

    [Fact]
    public void Unknown_Local_State_Is_Unresolved_And_Reconciliation_Required()
    {
        var broker = Broker();
        var decision = LiveOrderReconciliationContract.Decide(Local(LiveOrderStatus.Unknown), broker);

        Assert.Equal(LiveOrderReconciliationOutcome.Unresolved, decision.Outcome);
        Assert.True(decision.ReconciliationRequired);
        Assert.Equal(broker.ProviderOrderId, decision.ProviderOrderId);
        Assert.Contains("Unknown execution state", decision.Reason);
    }

    [Fact]
    public void Unknown_Broker_State_Is_Unresolved_And_Reconciliation_Required()
    {
        var broker = Broker(LiveOrderStatus.Unknown);
        var decision = LiveOrderReconciliationContract.Decide(Local(), broker);

        Assert.Equal(LiveOrderReconciliationOutcome.Unresolved, decision.Outcome);
        Assert.True(decision.ReconciliationRequired);
        Assert.Equal(broker.Status, decision.BrokerStatus);
    }

    [Fact]
    public void Filled_Quantity_Requires_Price()
    {
        var broker = Broker(LiveOrderStatus.Filled) with { FilledQuantity = 1 };
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LiveOrderReconciliationContract.ValidateSnapshot(broker));
        Assert.Contains("average fill price", exception.Message);
    }

    [Fact]
    public void Negative_Filled_Quantity_Is_Rejected()
    {
        var broker = Broker() with { FilledQuantity = -1 };
        Assert.Throws<ArgumentException>(() =>
            LiveOrderReconciliationContract.ValidateSnapshot(broker));
    }
}
