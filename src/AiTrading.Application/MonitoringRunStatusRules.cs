namespace AiTrading.Application;

public static class MonitoringRunStatusRules
{
    public static MonitoringRunStatus Resolve(int positionCount, int successCount, int failureCount)
    {
        if (positionCount < 0 || successCount < 0 || failureCount < 0 || successCount + failureCount != positionCount)
            throw new ArgumentException("Monitoring counts must be non-negative and add up to the position count.");
        if (failureCount == 0) return MonitoringRunStatus.Completed;
        return successCount == 0 ? MonitoringRunStatus.Failed : MonitoringRunStatus.PartiallyFailed;
    }
}
