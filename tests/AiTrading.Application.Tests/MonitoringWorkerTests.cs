using Microsoft.Extensions.Logging.Abstractions;
using AiTrading.Worker;

namespace AiTrading.Application.Tests;

public sealed class MonitoringWorkerTests
{
    [Fact]
    public async Task Executes_check_and_uses_injected_interval_without_real_delay()
    {
        var checks = 0;
        var checkCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var delay = new RecordingDelay(cancellation, cancelOnDelay: TimeSpan.FromSeconds(30));
        var worker = new MonitoringWorker(
            NullLogger<MonitoringWorker>.Instance,
            _ =>
            {
                checks++;
                checkCompleted.SetResult();
                return Task.CompletedTask;
            },
            delay,
            new MonitoringWorkerOptions(TimeSpan.FromSeconds(30), 3, TimeSpan.FromSeconds(1)));

        await worker.StartAsync(cancellation.Token);
        await checkCompleted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, checks);
        Assert.Equal(new[] { TimeSpan.FromSeconds(30) }, delay.Delays);
    }

    [Fact]
    public async Task Retries_transient_failure_with_bounded_exponential_backoff_then_recovers()
    {
        var attempts = 0;
        var recovered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var delay = new RecordingDelay(cancellation, cancelOnDelay: TimeSpan.FromSeconds(30));
        var worker = new MonitoringWorker(
            NullLogger<MonitoringWorker>.Instance,
            _ =>
            {
                attempts++;
                if (attempts < 3) throw new InvalidOperationException("transient");
                recovered.SetResult();
                return Task.CompletedTask;
            },
            delay,
            new MonitoringWorkerOptions(TimeSpan.FromSeconds(30), 3, TimeSpan.FromSeconds(1)));

        await worker.StartAsync(cancellation.Token);
        await recovered.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(3, attempts);
        Assert.Equal(
            new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30) },
            delay.Delays);
    }

    [Fact]
    public async Task Stops_retrying_when_cancellation_is_requested()
    {
        var attempts = 0;
        var firstAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cancellation = new CancellationTokenSource();
        var delay = new RecordingDelay(cancellation, cancelOnDelay: TimeSpan.FromSeconds(1));
        var worker = new MonitoringWorker(
            NullLogger<MonitoringWorker>.Instance,
            _ =>
            {
                attempts++;
                firstAttempt.SetResult();
                throw new InvalidOperationException("transient");
            },
            delay,
            new MonitoringWorkerOptions(TimeSpan.FromSeconds(30), 3, TimeSpan.FromSeconds(1)));

        await worker.StartAsync(cancellation.Token);
        await firstAttempt.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(1, attempts);
        Assert.Equal(new[] { TimeSpan.FromSeconds(1) }, delay.Delays);
    }

    private sealed class RecordingDelay(CancellationTokenSource cancellation, TimeSpan cancelOnDelay) : IWorkerDelay
    {
        public List<TimeSpan> Delays { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            if (delay == cancelOnDelay) cancellation.Cancel();
            return Task.CompletedTask;
        }
    }
}
