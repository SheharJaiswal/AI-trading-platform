using AiTrading.Application;
using Microsoft.Extensions.Logging;

namespace AiTrading.Worker;

public sealed record MonitoringWorkerOptions(
    TimeSpan Interval,
    int MaxAttempts,
    TimeSpan InitialRetryDelay)
{
    public static MonitoringWorkerOptions Default => new(
        TimeSpan.FromSeconds(30),
        3,
        TimeSpan.FromSeconds(1));
}

public interface IWorkerDelay
{
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}

public sealed class WorkerDelay : IWorkerDelay
{
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
        Task.Delay(delay, cancellationToken);
}

public sealed class MonitoringWorker(
    ILogger<MonitoringWorker> logger,
    Func<CancellationToken, Task> checkOnce,
    IWorkerDelay delay,
    MonitoringWorkerOptions options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = Guid.NewGuid().ToString("N")
            });

            await RunIterationAsync(stoppingToken);

            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                await delay.DelayAsync(options.Interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunIterationAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= options.MaxAttempts; attempt++)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                await checkOnce(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex) when (attempt < options.MaxAttempts)
            {
                logger.LogWarning(ex, "Background risk monitoring attempt {Attempt} of {MaxAttempts} failed; retrying.", attempt, options.MaxAttempts);
                var retryDelay = TimeSpan.FromMilliseconds(options.InitialRetryDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                try
                {
                    await delay.DelayAsync(retryDelay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Background risk monitoring iteration failed after {MaxAttempts} attempts.", options.MaxAttempts);
            }
        }
    }
}
