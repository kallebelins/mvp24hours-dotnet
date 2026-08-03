using System.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Pipe.Integration.OpenTelemetry;

namespace Mvp24Hours.Application.Pipe.Test.Integration;

[Trait("Category", "Unit")]
public class OpenTelemetryMiddlewareTest
{
    [Fact]
    public async Task ExecuteAsync_WithoutActivityListener_ShouldInvokeNext()
    {
        var middleware = new OpenTelemetryMiddleware();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        bool nextCalled = false;

        await middleware.ExecuteAsync(message, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulPipeline_ShouldComplete()
    {
        using ActivityListener listener = CreateListener();
        var middleware = new OpenTelemetryMiddleware(
            NullLogger<OpenTelemetryMiddleware>.Instance,
            new OpenTelemetryOptions { IncludeMessageDetails = true, IncludeInputDetails = true });
        IPipelineMessage message = PipeTestHelpers.CreateMessage("key", "value");
        bool nextCalled = false;

        await middleware.ExecuteAsync(message, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenMessageBecomesLocked_ShouldComplete()
    {
        using ActivityListener listener = CreateListener();
        var middleware = new OpenTelemetryMiddleware();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () =>
        {
            message.Messages.Add(new MessageResult("validation failed", MessageType.Error));
            message.SetLock();
            return Task.CompletedTask;
        });

        message.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_ShouldPropagateException()
    {
        using ActivityListener listener = CreateListener();
        var middleware = new OpenTelemetryMiddleware();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Func<Task> act = () => middleware.ExecuteAsync(message, () => throw new InvalidOperationException("pipeline failed"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldPropagateCancellation()
    {
        using ActivityListener listener = CreateListener();
        var middleware = new OpenTelemetryMiddleware();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => middleware.ExecuteAsync(
            message,
            () => throw new OperationCanceledException(cts.Token),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void ExecuteSync_WithoutActivityListener_ShouldInvokeNext()
    {
        var middleware = new OpenTelemetryMiddlewareSync();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        bool nextCalled = false;

        middleware.Execute(message, () => nextCalled = true);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public void ExecuteSync_WhenMessageBecomesFaulty_ShouldComplete()
    {
        using ActivityListener listener = CreateListener();
        var middleware = new OpenTelemetryMiddlewareSync();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        middleware.Execute(message, () => message.SetFailure());

        message.IsFaulty.Should().BeTrue();
    }

    private static ActivityListener CreateListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Mvp24Hours.Pipeline",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }
}
