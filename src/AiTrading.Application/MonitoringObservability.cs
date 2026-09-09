namespace AiTrading.Application;

public sealed record MonitoringFailure(string Scope, string Symbol, string ErrorCode, string Message, DateTimeOffset OccurredAt);

public interface IMonitoringFailureSink
{
    void Record(MonitoringFailure failure);
}

public sealed class NoopMonitoringFailureSink : IMonitoringFailureSink
{
    public void Record(MonitoringFailure failure) { }
}
