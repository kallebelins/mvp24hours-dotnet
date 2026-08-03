using App.Core.Contract.Logic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Services;

namespace App.Worker.Jobs;

public sealed class ItemProcessingJob : CronJobService<ItemProcessingJob>
{
    private readonly IItemProcessor _processor;
    private readonly ILogger<ItemProcessingJob> _jobLogger;

    public ItemProcessingJob(
        IScheduleConfig<ItemProcessingJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ILogger<CronJobService<ItemProcessingJob>> logger)
        : base(config, hostApplication, rootServiceProvider, logger)
    {
        _processor = rootServiceProvider.GetRequiredService<IItemProcessor>();
        _jobLogger = rootServiceProvider.GetRequiredService<ILogger<ItemProcessingJob>>();
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        _jobLogger.LogInformation("[ItemProcessingJob] Starting execution #{Count}", ExecutionCount);
        await _processor.ProcessAsync(cancellationToken);
    }
}
