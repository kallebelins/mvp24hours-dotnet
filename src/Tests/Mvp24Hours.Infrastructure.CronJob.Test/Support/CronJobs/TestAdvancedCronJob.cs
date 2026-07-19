using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

public sealed class TestAdvancedCronJob(
    IResilientScheduleConfig<TestAdvancedCronJob> config,
    IHostApplicationLifetime hostApplication,
    IServiceProvider serviceProvider,
    ICronJobExecutionLock executionLock,
    CronJobCircuitBreaker circuitBreaker,
    ILogger<AdvancedCronJobService<TestAdvancedCronJob>> logger,
    Func<ICronJobContext, CancellationToken, Task>? execute = null,
    TimeProvider? timeProvider = null) : AdvancedCronJobService<TestAdvancedCronJob>(
        config,
        hostApplication,
        serviceProvider,
        executionLock,
        circuitBreaker,
        logger,
        timeProvider)
{
    private readonly Func<ICronJobContext, CancellationToken, Task>? _execute = execute;

    public int ExecuteCount { get; private set; }
    public ICronJobContext? LastContext { get; private set; }
    public ICronJobContext? LastStartingContext { get; private set; }
    public ICronJobContext? LastCompletedContext { get; private set; }
    public Exception? LastFailure { get; private set; }

    protected override async Task ExecuteAsync(ICronJobContext context, CancellationToken cancellationToken)
    {
        ExecuteCount++;
        LastContext = context;

        if (_execute != null)
        {
            await _execute(context, cancellationToken);
        }
    }

    protected override Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken)
    {
        LastStartingContext = context;
        return Task.CompletedTask;
    }

    protected override Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
    {
        LastCompletedContext = context;
        return Task.CompletedTask;
    }

    protected override Task OnJobFailedAsync(ICronJobContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
    {
        LastFailure = exception;
        return Task.CompletedTask;
    }
}
