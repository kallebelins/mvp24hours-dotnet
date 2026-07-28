using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;

namespace CronJobWorker.Jobs;

/// <summary>
/// Cleanup job — fires every 5 minutes and simulates a data-cleanup routine.
/// Demonstrates <see cref="ResilientCronJobService{T}"/> with:
/// <list type="bullet">
///   <item>Retry with exponential back-off</item>
///   <item>Circuit breaker to suppress flapping failures</item>
///   <item>Overlapping-execution prevention</item>
/// </list>
/// Register with <c>AddResilientCronJob&lt;CleanupJob&gt;</c>.
/// </summary>
public sealed class CleanupJob : ResilientCronJobService<CleanupJob>
{
    private readonly ILogger<CleanupJob> _jobLogger;
    private readonly TimeProvider _timeProvider;

    public CleanupJob(
        IResilientScheduleConfig<CleanupJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ICronJobExecutionLock executionLock,
        CronJobCircuitBreaker circuitBreaker,
        ILogger<ResilientCronJobService<CleanupJob>> logger,
        TimeProvider? timeProvider = null)
        : base(config, hostApplication, rootServiceProvider, executionLock, circuitBreaker, logger, timeProvider)
    {
        _jobLogger = rootServiceProvider.GetRequiredService<ILogger<CleanupJob>>();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Simulates a cleanup pass — purging stale records or temp files.
    /// In a real scenario, inject a scoped repository via <c>_serviceProvider</c>.
    /// </summary>
    public override async Task DoWork(CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        _jobLogger.LogInformation(
            "[CleanupJob] Starting cleanup at {Time} UTC — execution #{Count}",
            now.ToString("yyyy-MM-dd HH:mm:ss"),
            ExecutionCount);

        // Simulate work. Replace with real cleanup logic.
        await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);

        // Example: uncomment to test the retry + circuit-breaker path.
        // if (ExecutionCount % 3 == 0)
        //     throw new InvalidOperationException("Simulated transient failure.");

        _jobLogger.LogInformation(
            "[CleanupJob] Cleanup finished in execution #{Count}. Retries so far: {Retries}, Skipped: {Skipped}",
            ExecutionCount,
            RetryCount,
            SkippedCount);
    }
}
