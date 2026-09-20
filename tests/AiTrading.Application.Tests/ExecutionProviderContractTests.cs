using AiTrading.Domain;
using FluentAssertions;

namespace AiTrading.Application.Tests;

public sealed class ExecutionProviderContractTests
{
    private static ExecutionRequest Request(ExecutionMode mode = ExecutionMode.Paper, bool explicitEnablement = false) =>
        new(
            new ExecutionContext(mode, explicitEnablement),
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "execution-123",
            "paper-account",
            new Symbol("AAPL", "AAPL-1"),
            OrderSide.Buy,
            2,
            100m);

    [Fact]
    public void Rejects_Backtest_Execution_Request()
    {
        var request = Request(ExecutionMode.Backtest);

        var action = () => ExecutionProviderContract.ValidateRequest(request);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot submit execution requests*");
    }

    [Fact]
    public void Rejects_Live_Request_Without_Explicit_Enablement()
    {
        var request = Request(ExecutionMode.Live);

        var action = () => ExecutionProviderContract.ValidateRequest(request);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*explicit operator enablement*");
    }

    [Fact]
    public void Accepts_Explicit_Live_Request_Without_Enabling_Provider_Reachability()
    {
        var request = Request(ExecutionMode.Live, true);

        ExecutionProviderContract.ValidateRequest(request);

        ExecutionModePolicy.RequireExplicitLive(request.Context);
        ExecutionModePolicy.CanReachProvider(request.Context, "live").Should().BeFalse();
    }

    [Fact]
    public void Rejects_Unknown_Result_Without_Reconciliation()
    {
        var request = Request();
        var result = new ExecutionResult(
            ExecutionProviderStatus.Unknown,
            "paper",
            request.OrderId,
            request.IdempotencyKey,
            null,
            "timeout",
            false);

        var action = () => ExecutionProviderContract.ValidateResult(request, result);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*require reconciliation*");
    }

    [Fact]
    public void Creates_Unknown_Result_As_Reconciliation_Required()
    {
        var request = Request();

        var result = ExecutionProviderContract.CreateUnknown(request, "paper", "timeout");

        result.Status.Should().Be(ExecutionProviderStatus.Unknown);
        result.ReconciliationRequired.Should().BeTrue();
        result.Fill.Should().BeNull();
    }

    [Fact]
    public void Rejects_Result_With_Mismatched_Idempotency_Key()
    {
        var request = Request();
        var result = new ExecutionResult(
            ExecutionProviderStatus.Rejected,
            "paper",
            request.OrderId,
            "different-key",
            null,
            "risk blocked",
            false);

        var action = () => ExecutionProviderContract.ValidateResult(request, result);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*idempotency key*");
    }
}
