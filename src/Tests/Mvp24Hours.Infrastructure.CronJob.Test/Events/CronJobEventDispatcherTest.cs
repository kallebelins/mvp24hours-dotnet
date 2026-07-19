using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.CronJob.Context;
using Mvp24Hours.Infrastructure.CronJob.Events;
using Mvp24Hours.Infrastructure.CronJob.Test.Support;

namespace Mvp24Hours.Infrastructure.CronJob.Test.Events;

[Trait("Category", "Unit")]
public class CronJobEventDispatcherTest
{
    [Fact]
    public async Task DispatchStartingAsync_ShouldInvokeHandlersInOrder()
    {
        var first = new RecordingCronJobEventHandler { OrderValue = 1 };
        var second = new RecordingCronJobEventHandler { OrderValue = 2 };
        ServiceProvider provider = BuildProvider(first, second);
        var dispatcher = new CronJobEventDispatcher(provider, NullLogger<CronJobEventDispatcher>.Instance);
        CronJobContext context = CronJobTestHelpers.CreateContext();

        await dispatcher.DispatchStartingAsync(context, CancellationToken.None);

        first.Events.Should().ContainSingle().Which.Should().Be("Starting");
        second.Events.Should().ContainSingle().Which.Should().Be("Starting");
    }

    [Fact]
    public async Task DispatchCompletedAsync_ShouldInvokeCompletedHandler()
    {
        var handler = new RecordingCronJobEventHandler();
        ServiceProvider provider = BuildProvider(handler);
        var dispatcher = new CronJobEventDispatcher(provider, NullLogger<CronJobEventDispatcher>.Instance);
        CronJobContext context = CronJobTestHelpers.CreateContext();

        await dispatcher.DispatchCompletedAsync(context, TimeSpan.FromSeconds(1), CancellationToken.None);

        handler.Events.Should().ContainSingle().Which.Should().Be("Completed");
    }

    [Fact]
    public async Task DispatchFailedAsync_ShouldInvokeFailedHandler()
    {
        var handler = new RecordingCronJobEventHandler();
        ServiceProvider provider = BuildProvider(handler);
        var dispatcher = new CronJobEventDispatcher(provider, NullLogger<CronJobEventDispatcher>.Instance);
        CronJobContext context = CronJobTestHelpers.CreateContext();

        await dispatcher.DispatchFailedAsync(context, new InvalidOperationException("boom"), TimeSpan.FromSeconds(1), CancellationToken.None);

        handler.Events.Should().ContainSingle().Which.Should().Be("Failed");
    }

    [Fact]
    public async Task DispatchCancelledAsync_ShouldInvokeCancelledHandler()
    {
        var handler = new RecordingCronJobEventHandler();
        ServiceProvider provider = BuildProvider(handler);
        var dispatcher = new CronJobEventDispatcher(provider, NullLogger<CronJobEventDispatcher>.Instance);
        CronJobContext context = CronJobTestHelpers.CreateContext();

        await dispatcher.DispatchCancelledAsync(context, TimeSpan.FromMilliseconds(50), CancellationToken.None);

        handler.Events.Should().ContainSingle().Which.Should().Be("Cancelled");
    }

    [Fact]
    public async Task DispatchRetryAsync_ShouldInvokeRetryHandler()
    {
        var handler = new RecordingCronJobEventHandler();
        ServiceProvider provider = BuildProvider(handler);
        var dispatcher = new CronJobEventDispatcher(provider, NullLogger<CronJobEventDispatcher>.Instance);
        CronJobContext context = CronJobTestHelpers.CreateContext();

        await dispatcher.DispatchRetryAsync(context, new InvalidOperationException("retry"), TimeSpan.FromSeconds(2), CancellationToken.None);

        handler.Events.Should().ContainSingle().Which.Should().Be("Retry");
    }

    [Fact]
    public async Task DispatchSkippedAsync_ShouldInvokeSkippedHandler()
    {
        var handler = new RecordingCronJobEventHandler();
        ServiceProvider provider = BuildProvider(handler);
        var dispatcher = new CronJobEventDispatcher(provider, NullLogger<CronJobEventDispatcher>.Instance);

        await dispatcher.DispatchSkippedAsync("MyJob", SkipReason.Paused, CancellationToken.None);

        handler.Events.Should().ContainSingle().Which.Should().Be("Skipped:Paused");
        handler.SkippedEvents.Should().ContainSingle()
            .Which.Should().Be(("MyJob", SkipReason.Paused));
    }

    [Fact]
    public async Task DispatchStartingAsync_ShouldContinue_WhenHandlerThrows()
    {
        var failing = new FailingCronJobEventHandler();
        var recording = new RecordingCronJobEventHandler();
        ServiceProvider provider = BuildProvider(failing, recording);
        var dispatcher = new CronJobEventDispatcher(provider, NullLogger<CronJobEventDispatcher>.Instance);
        CronJobContext context = CronJobTestHelpers.CreateContext();

        Func<Task> act = () => dispatcher.DispatchStartingAsync(context, CancellationToken.None);

        await act.Should().NotThrowAsync();
        recording.Events.Should().ContainSingle().Which.Should().Be("Starting");
    }

    private static ServiceProvider BuildProvider(params ICronJobEventHandler[] handlers)
    {
        var services = new ServiceCollection();
        foreach (ICronJobEventHandler handler in handlers)
        {
            if (handler is ICronJobStartingHandler starting)
            {
                services.AddSingleton<ICronJobStartingHandler>(starting);
            }
            if (handler is ICronJobCompletedHandler completed)
            {
                services.AddSingleton<ICronJobCompletedHandler>(completed);
            }
            if (handler is ICronJobFailedHandler failed)
            {
                services.AddSingleton<ICronJobFailedHandler>(failed);
            }
            if (handler is ICronJobCancelledHandler cancelled)
            {
                services.AddSingleton<ICronJobCancelledHandler>(cancelled);
            }
            if (handler is ICronJobRetryHandler retry)
            {
                services.AddSingleton<ICronJobRetryHandler>(retry);
            }
            if (handler is ICronJobSkippedHandler skipped)
            {
                services.AddSingleton<ICronJobSkippedHandler>(skipped);
            }
        }

        return services.BuildServiceProvider();
    }
}
