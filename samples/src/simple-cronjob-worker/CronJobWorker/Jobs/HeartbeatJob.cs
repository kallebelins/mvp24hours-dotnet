using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Services;

namespace CronJobWorker.Jobs;

/// <summary>
/// Heartbeat job — fires every minute and logs a timestamped pulse.
/// Demonstrates the simplest possible <see cref="CronJobService{T}"/> usage:
/// override <see cref="DoWork"/> and register with <c>AddCronJob&lt;HeartbeatJob&gt;</c>.
/// </summary>
public sealed class HeartbeatJob : CronJobService<HeartbeatJob>
{
    private readonly ILogger<HeartbeatJob> _jobLogger;
    private readonly TimeProvider _timeProvider;

    public HeartbeatJob(
        IScheduleConfig<HeartbeatJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ILogger<CronJobService<HeartbeatJob>> logger,
        // TimeProvider is injected for testability — swap with FakeTimeProvider in tests.
        TimeProvider? timeProvider = null)
        : base(config, hostApplication, rootServiceProvider, logger, timeProvider)
    {
        _jobLogger = rootServiceProvider.GetRequiredService<ILogger<HeartbeatJob>>();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Called on every scheduled tick. Logs a heartbeat with the current UTC time.
    /// </summary>
    public override Task DoWork(CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        _jobLogger.LogInformation(
            "[HeartbeatJob] Pulse at {Time} UTC — execution #{Count}",
            now.ToString("yyyy-MM-dd HH:mm:ss"),
            ExecutionCount);

        return Task.CompletedTask;
    }
}
