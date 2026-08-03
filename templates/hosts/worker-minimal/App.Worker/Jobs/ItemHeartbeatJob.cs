using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Services;

namespace App.Worker.Jobs;

public sealed class ItemHeartbeatJob : CronJobService<ItemHeartbeatJob>
{
    private readonly ILogger<ItemHeartbeatJob> _jobLogger;
    private readonly TimeProvider _timeProvider;

    public ItemHeartbeatJob(
        IScheduleConfig<ItemHeartbeatJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ILogger<CronJobService<ItemHeartbeatJob>> logger,
        TimeProvider? timeProvider = null)
        : base(config, hostApplication, rootServiceProvider, logger, timeProvider)
    {
        _jobLogger = rootServiceProvider.GetRequiredService<ILogger<ItemHeartbeatJob>>();
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public override Task DoWork(CancellationToken cancellationToken)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        _jobLogger.LogInformation(
            "[ItemHeartbeatJob] Pulse at {Time} UTC — execution #{Count}",
            now.ToString("yyyy-MM-dd HH:mm:ss"),
            ExecutionCount);

        return Task.CompletedTask;
    }
}
