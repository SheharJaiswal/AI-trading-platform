namespace AiTrading.Application.Tests;

public sealed class LiveOrderReconciliationEvidenceTests
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
    public void Matched_Decision_Produces_Evidence_That_Does_Not_Require_Reconciliation()
    {
        var broker = Broker();
        var decision = LiveOrderReconciliationContract.Decide(Local(), broker);
        var evidence = LiveOrderReconciliationEvidence.Create(Local(), broker, decision);

        Assert.NotEqual(Guid.Empty, evidence.EvidenceId);
        Assert.Equal(LiveOrderReconciliationOutcome.Matched, evidence.Outcome);
        Assert.False(evidence.ReconciliationRequired);
        Assert.Equal(Local().Status, evidence.LocalStatus);
        Assert.Equal(broker.Status, evidence.BrokerStatus);
        Assert.Equal(broker.ProviderOrderId, evidence.ProviderOrderId);
        Assert.Equal(broker.ObservedAt, evidence.ObservedAt);
    }

    [Fact]
    public void Divergent_Decision_Produces_Reconciliation_Required_Evidence()
    {
        var broker = Broker() with { ProviderOrderId = "different-provider-order" };
        var local = Local();
        var decision = LiveOrderReconciliationContract.Decide(local, broker);
        var evidence = LiveOrderReconciliationEvidence.Create(local, broker, decision);

        Assert.Equal(LiveOrderReconciliationOutcome.Divergent, evidence.Outcome);
        Assert.True(evidence.ReconciliationRequired);
        Assert.Contains("Provider order identity", evidence.Reason);
    }

    [Fact]
    public void Unresolved_Decision_Produces_Reconciliation_Required_Evidence()
    {
        var local = Local(LiveOrderStatus.Unknown);
        var broker = Broker();
        var decision = LiveOrderReconciliationContract.Decide(local, broker);
        var evidence = LiveOrderReconciliationEvidence.Create(local, broker, decision);

        Assert.Equal(LiveOrderReconciliationOutcome.Unresolved, evidence.Outcome);
        Assert.True(evidence.ReconciliationRequired);
        Assert.Contains("Unknown execution state", evidence.Reason);
    }

    [Fact]
    public void Evidence_Rejects_A_Decision_That_Clears_Reconciliation_For_Divergence()
    {
        var local = Local();
        var broker = Broker() with { ProviderOrderId = "different-provider-order" };
        var decision = new LiveOrderReconciliationDecision(
            LiveOrderReconciliationOutcome.Divergent,
            broker.Status,
            false,
            "invalid decision",
            broker.ProviderOrderId,
            broker.ObservedAt);

        Assert.Throws<InvalidOperationException>(() =>
            LiveOrderReconciliationEvidence.Create(local, broker, decision));
    }

    [Fact]
    public void Evidence_Rejects_Mismatched_Decision_Observation()
    {
        var local = Local();
        var broker = Broker();
        var decision = LiveOrderReconciliationContract.Decide(local, broker) with
        {
            ObservedAt = broker.ObservedAt.AddSeconds(1)
        };

        Assert.Throws<ArgumentException>(() =>
            LiveOrderReconciliationEvidence.Create(local, broker, decision));
    }
}
