//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

[Trait("Category", "Unit")]
public class NotificationPublishersTest
{
    private sealed class TrackingNotification : IMediatorNotification
    {
        public string Message { get; init; } = string.Empty;
    }

    private sealed class FastHandler(string label, List<string> log, bool fail = false)
        : IMediatorNotificationHandler<TrackingNotification>
    {
        public Task Handle(TrackingNotification notification, CancellationToken cancellationToken)
        {
            if (fail)
            {
                throw new InvalidOperationException($"fail-{label}");
            }

            log.Add(label);
            return Task.CompletedTask;
        }
    }

    private sealed class SlowHandler(string label, List<string> log, int delayMs)
        : IMediatorNotificationHandler<TrackingNotification>
    {
        public async Task Handle(TrackingNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(delayMs, cancellationToken);
            log.Add(label);
        }
    }

    private sealed class ParallelSlowHandler(string label, System.Collections.Concurrent.ConcurrentBag<string> log, int delayMs)
        : IMediatorNotificationHandler<TrackingNotification>
    {
        public async Task Handle(TrackingNotification notification, CancellationToken cancellationToken)
        {
            await Task.Delay(delayMs, cancellationToken);
            log.Add(label);
        }
    }

    [Fact]
    public async Task SequentialNotificationPublisher_ShouldExecuteInOrder()
    {
        var log = new List<string>();
        var publisher = new SequentialNotificationPublisher();
        IMediatorNotificationHandler<TrackingNotification>[] handlers =
        [
            new FastHandler("first", log),
            new FastHandler("second", log)
        ];

        await publisher.PublishAsync(new TrackingNotification(), handlers, CancellationToken.None);

        Assert.Equal(["first", "second"], log);
    }

    [Fact]
    public async Task SequentialNotificationPublisher_FailingHandler_ShouldStopSubsequentHandlers()
    {
        var log = new List<string>();
        var publisher = new SequentialNotificationPublisher();
        IMediatorNotificationHandler<TrackingNotification>[] handlers =
        [
            new FastHandler("first", log, fail: true),
            new FastHandler("second", log)
        ];

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(new TrackingNotification(), handlers, CancellationToken.None));

        Assert.Empty(log);
    }

    [Fact]
    public async Task ParallelNotificationPublisher_ShouldExecuteAllHandlers()
    {
        var log = new System.Collections.Concurrent.ConcurrentBag<string>();
        var publisher = new ParallelNotificationPublisher();
        IMediatorNotificationHandler<TrackingNotification>[] handlers =
        [
            new ParallelSlowHandler("a", log, 50),
            new ParallelSlowHandler("b", log, 50)
        ];

        await publisher.PublishAsync(new TrackingNotification(), handlers, CancellationToken.None);

        Assert.Equal(2, log.Count);
        Assert.Contains("a", log);
        Assert.Contains("b", log);
    }

    [Fact]
    public async Task ParallelNoWaitNotificationPublisher_ShouldNotWaitForHandlers()
    {
        var log = new List<string>();
        var publisher = new ParallelNoWaitNotificationPublisher(NullLogger<ParallelNoWaitNotificationPublisher>.Instance);
        IMediatorNotificationHandler<TrackingNotification>[] handlers =
        [
            new SlowHandler("slow", log, 200)
        ];

        await publisher.PublishAsync(new TrackingNotification(), handlers, CancellationToken.None);

        Assert.Empty(log);
    }

    [Fact]
    public async Task ParallelNoWaitNotificationPublisher_FailingHandler_ShouldNotThrow()
    {
        var publisher = new ParallelNoWaitNotificationPublisher(NullLogger<ParallelNoWaitNotificationPublisher>.Instance);
        IMediatorNotificationHandler<TrackingNotification>[] handlers =
        [
            new FastHandler("fail", [], fail: true)
        ];

        await publisher.PublishAsync(new TrackingNotification(), handlers, CancellationToken.None);
    }

    [Fact]
    public async Task SequentialContinueOnExceptionPublisher_SingleFailure_ShouldRethrow()
    {
        var log = new List<string>();
        var publisher = new SequentialContinueOnExceptionPublisher();
        IMediatorNotificationHandler<TrackingNotification>[] handlers =
        [
            new FastHandler("first", log, fail: true),
            new FastHandler("second", log)
        ];

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(new TrackingNotification(), handlers, CancellationToken.None));

        Assert.Equal(["second"], log);
    }

    [Fact]
    public async Task SequentialContinueOnExceptionPublisher_MultipleFailures_ShouldThrowAggregateException()
    {
        var publisher = new SequentialContinueOnExceptionPublisher();
        IMediatorNotificationHandler<TrackingNotification>[] handlers =
        [
            new FastHandler("first", [], fail: true),
            new FastHandler("second", [], fail: true)
        ];

        AggregateException ex = await Assert.ThrowsAsync<AggregateException>(() =>
            publisher.PublishAsync(new TrackingNotification(), handlers, CancellationToken.None));

        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task PipelineHookBase_DefaultImplementations_ShouldComplete()
    {
        var hook = new TestPipelineHook();
        var request = new TestCommand { Name = "hook", Value = 1 };

        await hook.OnPipelineStartAsync(request, typeof(TestCommand), CancellationToken.None);
        await hook.OnPipelineCompleteAsync(request, "ok", typeof(TestCommand), typeof(string), 10, CancellationToken.None);
        await hook.OnPipelineErrorAsync(request, new Exception("err"), typeof(TestCommand), 10, CancellationToken.None);

        Assert.True(true);
    }

    [Fact]
    public async Task PipelineHookBehavior_ShouldInvokeGlobalAndTypedHooks()
    {
        TestTrackingPipelineHook.Reset();
        TestTrackingTypedPipelineHook.Reset();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
            options.RegisterHandlersFromAssembly(typeof(TestCommand).Assembly));
        services.AddSingleton<IPipelineHook, TestTrackingPipelineHook>();
        services.AddSingleton<IPipelineHook<TestCommand>, TestTrackingTypedPipelineHook>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineHookBehavior<,>));

        ServiceProvider sp = services.BuildServiceProvider();
        IMediator mediator = sp.GetRequiredService<IMediator>();

        await mediator.SendAsync(new TestCommand { Name = "hooks", Value = 1 });

        Assert.Contains("global-start", TestTrackingPipelineHook.Events);
        Assert.Contains("global-complete", TestTrackingPipelineHook.Events);
        Assert.Contains("typed-start", TestTrackingTypedPipelineHook.Events);
        Assert.Contains("typed-complete", TestTrackingTypedPipelineHook.Events);
    }

    [Fact]
    public async Task PipelineHookBehavior_HandlerFailure_ShouldInvokeErrorHooks()
    {
        TestTrackingPipelineHook.Reset();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(options =>
            options.RegisterHandlersFromAssembly(typeof(FailingCommand).Assembly));
        services.AddSingleton<IPipelineHook, TestTrackingPipelineHook>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PipelineHookBehavior<,>));

        ServiceProvider sp = services.BuildServiceProvider();
        IMediator mediator = sp.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new FailingCommand { Message = "hook error" }));

        Assert.Contains("global-error", TestTrackingPipelineHook.Events);
    }

    private sealed class TestPipelineHook : PipelineHookBase;

    private sealed class TestTrackingPipelineHook : PipelineHookBase
    {
        public static List<string> Events { get; } = [];

        public static void Reset() => Events.Clear();

        public override Task OnPipelineStartAsync(object request, Type requestType, CancellationToken cancellationToken)
        {
            Events.Add("global-start");
            return Task.CompletedTask;
        }

        public override Task OnPipelineCompleteAsync(
            object request,
            object? response,
            Type requestType,
            Type responseType,
            long elapsedMilliseconds,
            CancellationToken cancellationToken)
        {
            Events.Add("global-complete");
            return Task.CompletedTask;
        }

        public override Task OnPipelineErrorAsync(
            object request,
            Exception exception,
            Type requestType,
            long elapsedMilliseconds,
            CancellationToken cancellationToken)
        {
            Events.Add("global-error");
            return Task.CompletedTask;
        }
    }

    private sealed class TestTrackingTypedPipelineHook : PipelineHookBase<TestCommand>
    {
        public static List<string> Events { get; } = [];

        public static void Reset() => Events.Clear();

        public override Task OnPipelineStartAsync(TestCommand request, CancellationToken cancellationToken)
        {
            Events.Add("typed-start");
            return Task.CompletedTask;
        }

        public override Task OnPipelineCompleteAsync(
            TestCommand request,
            object? response,
            long elapsedMilliseconds,
            CancellationToken cancellationToken)
        {
            Events.Add("typed-complete");
            return Task.CompletedTask;
        }
    }
}
