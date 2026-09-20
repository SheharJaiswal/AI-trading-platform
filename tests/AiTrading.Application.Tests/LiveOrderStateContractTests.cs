namespace AiTrading.Application.Tests;

public sealed class LiveOrderStateContractTests
{
    private static LiveOrderState Pending() => new(
        Guid.Parse("22222222-2222-2222-2222-222222222222"),
        "live-execution-123",
        "account-1",
        "future-broker",
        LiveOrderStatus.Pending,
        null,
        false,
        null,
        DateTimeOffset.UtcNow,
        DateTimeOffset.UtcNow,
        1);

    [Fact]
    public void New_Order_Must_Start_Pending_At_Version_One()
    {
        var state = Pending();
        LiveOrderStateTransition.ValidateCreate(state);
        Assert.Equal(LiveOrderStatus.Pending, state.Status);
        Assert.Equal(1, state.Version);
    }

    [Fact]
    public void Unknown_State_Requires_Reconciliation()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LiveOrderStateTransition.ValidateTransition(Pending(), LiveOrderStatus.Unknown, false));
        Assert.Contains("requires reconciliation", exception.Message);
    }

    [Fact]
    public void Unknown_State_Cannot_Be_Retried_Without_Reconciliation()
    {
        var state = Pending() with { Status = LiveOrderStatus.Unknown, ReconciliationRequired = true };
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LiveOrderStateTransition.ValidateTransition(state, LiveOrderStatus.Submitted, false));
        Assert.Contains("remain reconciliation-required", exception.Message);
    }

    [Fact]
    public void Terminal_State_Cannot_Be_Overwritten()
    {
        var state = Pending() with { Status = LiveOrderStatus.Filled };
        var exception = Assert.Throws<InvalidOperationException>(() =>
            LiveOrderStateTransition.ValidateTransition(state, LiveOrderStatus.Unknown, true));
        Assert.Contains("Terminal live order state", exception.Message);
    }

    [Fact]
    public void Pending_Can_Become_Submitted_Or_Rejected()
    {
        var state = Pending();
        LiveOrderStateTransition.ValidateTransition(state, LiveOrderStatus.Submitted, false);
        LiveOrderStateTransition.ValidateTransition(state, LiveOrderStatus.Rejected, false);
    }

    [Fact]
    public void Duplicate_Identity_Uses_Stable_Idempotency_Key()
    {
        var first = Pending();
        var duplicate = first with { OrderId = Guid.Parse("33333333-3333-3333-3333-333333333333") };
        Assert.Equal(first.IdempotencyKey, duplicate.IdempotencyKey);
        Assert.NotEqual(first.OrderId, duplicate.OrderId);
    }
}
