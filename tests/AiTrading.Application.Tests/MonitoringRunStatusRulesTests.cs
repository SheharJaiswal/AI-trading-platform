using AiTrading.Application;

namespace AiTrading.Application.Tests;

public sealed class MonitoringRunStatusRulesTests
{
    [Fact]
    public void NoFailures_IsCompleted()
    {
        Assert.Equal(MonitoringRunStatus.Completed, MonitoringRunStatusRules.Resolve(3, 3, 0));
    }

    [Fact]
    public void MixedResults_ArePartiallyFailed()
    {
        Assert.Equal(MonitoringRunStatus.PartiallyFailed, MonitoringRunStatusRules.Resolve(3, 2, 1));
    }

    [Fact]
    public void AllFailures_AreFailed()
    {
        Assert.Equal(MonitoringRunStatus.Failed, MonitoringRunStatusRules.Resolve(3, 0, 3));
    }

    [Fact]
    public void InconsistentCounts_AreRejected()
    {
        Assert.Throws<ArgumentException>(() => MonitoringRunStatusRules.Resolve(3, 1, 1));
    }
}
