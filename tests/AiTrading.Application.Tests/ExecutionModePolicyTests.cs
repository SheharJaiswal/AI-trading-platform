using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class ExecutionModePolicyTests
{
    [Fact]
    public void Paper_mode_allows_only_paper_provider()
    {
        var context = new ExecutionContext(ExecutionMode.Paper, ExplicitlyEnabled: true);

        Assert.True(ExecutionModePolicy.CanReachProvider(context, "paper"));
        Assert.False(ExecutionModePolicy.CanReachProvider(context, "live"));
    }

    [Fact]
    public void Live_mode_is_not_reachable_until_explicitly_enabled()
    {
        var context = new ExecutionContext(ExecutionMode.Live);

        Assert.False(ExecutionModePolicy.CanReachProvider(context, "live"));
        Assert.Throws<InvalidOperationException>(() => ExecutionModePolicy.RequireExplicitLive(context));
    }

    [Fact]
    public void Live_mode_requires_explicit_operator_enablement()
    {
        var context = new ExecutionContext(ExecutionMode.Live, ExplicitlyEnabled: true);

        ExecutionModePolicy.RequireExplicitLive(context);
        Assert.False(ExecutionModePolicy.CanReachProvider(context, "live"));
    }

    [Theory]
    [InlineData(ExecutionMode.Backtest)]
    [InlineData(ExecutionMode.Research)]
    public void Simulation_modes_cannot_use_live_execution(ExecutionMode mode)
    {
        var context = new ExecutionContext(mode);

        Assert.Throws<InvalidOperationException>(() => ExecutionModePolicy.RequireNonLiveSimulation(
            new ExecutionContext(ExecutionMode.Live)));
        ExecutionModePolicy.RequireNonLiveSimulation(context);
        Assert.False(ExecutionModePolicy.CanReachProvider(context, "live"));
    }

    [Fact]
    public void Paper_guard_rejects_live_mode()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ExecutionModePolicy.RequirePaper(new ExecutionContext(ExecutionMode.Live, ExplicitlyEnabled: true)));
    }
}
