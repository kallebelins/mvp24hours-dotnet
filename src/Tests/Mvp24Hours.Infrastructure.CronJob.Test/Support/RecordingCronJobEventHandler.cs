using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Events;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Support;

public sealed class RecordingCronJobEventHandler : CronJobEventHandlerBase
{
    public int OrderValue { get; init; }

    public override int Order => OrderValue;

    public List<string> Events { get; } = [];
    public List<ICronJobContext> StartingContexts { get; } = [];
    public List<(string JobName, SkipReason Reason)> SkippedEvents { get; } = [];

    public override Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken)
    {
        Events.Add("Starting");
        StartingContexts.Add(context);
        return Task.CompletedTask;
    }

    public override Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
    {
        Events.Add("Completed");
        return Task.CompletedTask;
    }

    public override Task OnJobFailedAsync(ICronJobContext context, Exception exception, TimeSpan duration, CancellationToken cancellationToken)
    {
        Events.Add("Failed");
        return Task.CompletedTask;
    }

    public override Task OnJobCancelledAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
    {
        Events.Add("Cancelled");
        return Task.CompletedTask;
    }

    public override Task OnJobRetryAsync(ICronJobContext context, Exception exception, TimeSpan delay, CancellationToken cancellationToken)
    {
        Events.Add("Retry");
        return Task.CompletedTask;
    }

    public override Task OnJobSkippedAsync(string jobName, SkipReason reason, CancellationToken cancellationToken)
    {
        Events.Add($"Skipped:{reason}");
        SkippedEvents.Add((jobName, reason));
        return Task.CompletedTask;
    }
}

public sealed class FailingCronJobEventHandler : ICronJobStartingHandler
{
    public int Order => 0;

    public Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Handler failure");
    }
}
