//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Context;

namespace Mvp24Hours.Application.Pipe.Test.Context;

[Trait("Category", "Unit")]
public class OperationActivityMiddlewareTest
{
    [Fact]
    public void Constructor_WithNullContextAccessor_Throws()
    {
        Action act = () => new OperationActivityMiddleware(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Order_DefaultsToOptionsValue()
    {
        var accessor = new PipelineContextAccessor();
        var sut = new OperationActivityMiddleware(accessor, options: new OperationActivityOptions { Order = -123 });

        sut.Order.Should().Be(-123);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutContext_CallsNextWithoutCreatingActivity()
    {
        var accessor = new PipelineContextAccessor();
        var sut = new OperationActivityMiddleware(accessor);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        bool called = false;

        await sut.ExecuteAsync(message, () =>
        {
            called = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithTracingDisabled_CallsNextWithoutCreatingActivity()
    {
        var accessor = new PipelineContextAccessor { Context = new PipelineContext() };
        var sut = new OperationActivityMiddleware(accessor, options: new OperationActivityOptions { EnableOperationTracing = false });
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        bool called = false;

        await sut.ExecuteAsync(message, () =>
        {
            called = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        called.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithContextAndListener_CreatesActivityWithTags()
    {
        // Force PipelineContext's static initialization (and its ActivitySource) before
        // registering the listener; evaluating PipelineContext.ActivitySource.Name lazily
        // inside ShouldListenTo can otherwise reenter the still-running static constructor
        // (ActivitySource's own ctor notifies listeners about itself) and throw a
        // NullReferenceException.
        string sourceName = PipelineContext.ActivitySource.Name;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var accessor = new PipelineContextAccessor { Context = new PipelineContext("corr-activity") };
        var sut = new OperationActivityMiddleware(accessor);
        IPipelineMessage message = PipeTestHelpers.CreateMessage("key", "value");
        Activity? capturedDuringNext = null;

        await sut.ExecuteAsync(message, () =>
        {
            capturedDuringNext = Activity.Current;
            return Task.CompletedTask;
        }, CancellationToken.None);

        capturedDuringNext.Should().NotBeNull();
        capturedDuringNext!.GetTagItem("message.token").Should().Be(message.Token);
        capturedDuringNext.GetTagItem("operation.type").Should().Be("pipeline");
    }

    [Fact]
    public async Task ExecuteAsync_WhenContextHasOperationNameMetadata_UsesItAsActivityName()
    {
        string sourceName = PipelineContext.ActivitySource.Name;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var context = new PipelineContext();
        context.SetMetadata("__CurrentOperationName", "MyCustomOperation");
        var accessor = new PipelineContextAccessor { Context = context };
        var sut = new OperationActivityMiddleware(accessor);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        string? capturedName = null;

        await sut.ExecuteAsync(message, () =>
        {
            capturedName = Activity.Current?.OperationName;
            return Task.CompletedTask;
        }, CancellationToken.None);

        capturedName.Should().Be("MyCustomOperation");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_SetsErrorStatusAndRethrows()
    {
        string sourceName = PipelineContext.ActivitySource.Name;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
        };
        ActivitySource.AddActivityListener(listener);

        var accessor = new PipelineContextAccessor { Context = new PipelineContext() };
        var sut = new OperationActivityMiddleware(accessor);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Func<Task> act = () => sut.ExecuteAsync(message, () => throw new InvalidOperationException("boom"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
    }

    [Fact]
    public async Task ExecuteAsync_WithIncludeContentCountDisabled_StillExecutesNext()
    {
        var accessor = new PipelineContextAccessor { Context = new PipelineContext() };
        var sut = new OperationActivityMiddleware(accessor, options: new OperationActivityOptions { IncludeContentCount = false });
        IPipelineMessage message = PipeTestHelpers.CreateMessage("key", "value");
        bool called = false;

        await sut.ExecuteAsync(message, () =>
        {
            called = true;
            return Task.CompletedTask;
        }, CancellationToken.None);

        called.Should().BeTrue();
    }

    [Fact]
    public void OperationActivityOptions_Defaults_MatchDocumentedValues()
    {
        var options = new OperationActivityOptions();

        options.Order.Should().Be(-900);
        options.EnableOperationTracing.Should().BeTrue();
        options.OperationActivityKind.Should().Be(ActivityKind.Internal);
        options.IncludeContentCount.Should().BeTrue();
    }
}
