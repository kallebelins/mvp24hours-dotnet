using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Services;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Testing;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;

/// <summary>
/// Controllable <see cref="CronJobService{T}"/> for unit tests (supports TimeProvider and work hooks).
/// </summary>
public sealed class ControllableCronJob(
    IScheduleConfig<ControllableCronJob> config,
    IHostApplicationLifetime hostApplication,
    IServiceProvider serviceProvider,
    ILogger<CronJobService<ControllableCronJob>> logger,
    TimeProvider? timeProvider = null,
    Func<CancellationToken, Task>? workAction = null) : CronJobService<ControllableCronJob>(
        config,
        hostApplication,
        serviceProvider,
        logger,
        timeProvider)
{
    private readonly Func<CancellationToken, Task>? _workAction = workAction;
    private readonly IServiceProvider _rootProvider = serviceProvider;
    private ExecutionTracker? Tracker => _rootProvider.GetService<ExecutionTracker>();

    public int DoWorkInvocationCount { get; private set; }

    public IServiceProvider? LastScopedProvider { get; private set; }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        DoWorkInvocationCount++;
        LastScopedProvider = _serviceProvider;

        if (_workAction != null)
        {
            try
            {
                await _workAction(cancellationToken);
                Tracker?.RecordExecution();
            }
            catch (Exception ex)
            {
                Tracker?.RecordFailure(ex);
                throw;
            }

            return;
        }

        Tracker?.RecordExecution();
    }
}
