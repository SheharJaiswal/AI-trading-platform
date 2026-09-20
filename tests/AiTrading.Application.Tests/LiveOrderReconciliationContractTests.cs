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
    public void Mismatched_Idempotency_Is_Divergent()
    {
        var broker = Broker() with { IdempotencyKey = "different-key" };
        Assert.Equal(LiveOrderReconciliationOutcome.Divergent,
            LiveOrderReconciliationContract.Compare(Local(), broker));
    }

    [Fact]
    public void Mismatched_Provider_Order_Id_Is_Divergent()
    {
        var broker = Broker() with { ProviderOrderId = "different-provider-order" };
        Assert.Equal(LiveOrderReconciliationOutcome.Divergent,
            LiveOrderReconciliationContract.Compare(Local(), broker));
    }

    [Fact]
    public void Mismatched_Status_Is_Divergent()
    {
        Assert.Equal(LiveOrderReconciliationOutcome.Divergent,
            LiveOrderReconciliationContract.Compare(Local(), Broker(LiveOrderStatus.Rejected)));
    }

    [Fact]
    public void Unknown_Local_State_Is_Unresolved()
    {
        Assert.Equal(LiveOrderReconciliationOutcome.Unresolved,
            LiveOrderReconciliationContract.Compare(Local(LiveOrderStatus.Unknown), Broker()));
    }

    [Fact]
    public void Unknown_Broker_State_Is_Unresolved()
    {
        Assert.Equal(LiveOrderReconciliationOutcome.Unresolved,
            LiveOrderReconciliationContract.Compare(Local(), Broker(LiveOrderStatus.Unknown)));
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
