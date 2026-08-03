using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Context;

namespace Mvp24Hours.Application.Pipe.Test.Context;

[Trait("Category", "Unit")]
public class ContextPropagationMiddlewareTest
{
    [Fact]
    public void Constructor_WithNullAccessor_ShouldThrow()
    {
        Action act = () => _ = new ContextPropagationMiddleware(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Order_ShouldReflectOptions()
    {
        var middleware = new ContextPropagationMiddleware(
            new PipelineContextAccessor(),
            options: new ContextPropagationOptions { Order = -500 });

        middleware.Order.Should().Be(-500);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreateContextFromMessageToken()
    {
        var accessor = new PipelineContextAccessor();
        var middleware = new ContextPropagationMiddleware(accessor);
        IPipelineMessage message = new PipelineMessage("corr-token-123");
        IPipelineContext? captured = null;

        await middleware.ExecuteAsync(message, () =>
        {
            captured = accessor.Context;
            return Task.CompletedTask;
        }, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("corr-token-123");
        message.GetContent<IPipelineContext>(ContextPropagationMiddleware.PipelineContextKey).Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReuseExistingContextFromMessage()
    {
        var accessor = new PipelineContextAccessor();
        var middleware = new ContextPropagationMiddleware(accessor);
        var existing = new PipelineContext("existing-corr");
        IPipelineMessage message = new PipelineMessage("msg-token");
        message.AddContent(ContextPropagationMiddleware.PipelineContextKey, existing);
        IPipelineContext? captured = null;

        await middleware.ExecuteAsync(message, () =>
        {
            captured = accessor.Context;
            return Task.CompletedTask;
        }, CancellationToken.None);

        captured!.CorrelationId.Should().Be("existing-corr");
    }

    [Fact]
    public async Task ExecuteAsync_WithNestedAccessorContext_ShouldCreateChildContext()
    {
        var accessor = new PipelineContextAccessor();
        var parent = new PipelineContext("parent-corr") { TenantId = "tenant-a" };
        accessor.Context = parent;

        var middleware = new ContextPropagationMiddleware(accessor);
        IPipelineMessage message = new PipelineMessage("child-token");
        IPipelineContext? captured = null;

        await middleware.ExecuteAsync(message, () =>
        {
            captured = accessor.Context;
            return Task.CompletedTask;
        }, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().NotBe("parent-corr");
        captured.CausationId.Should().Be("parent-corr");
        captured.TenantId.Should().Be("tenant-a");
    }

    [Fact]
    public async Task ExecuteAsync_WithDefaultTenantId_ShouldApplyToNewContext()
    {
        var accessor = new PipelineContextAccessor();
        var options = new ContextPropagationOptions { DefaultTenantId = "default-tenant" };
        var middleware = new ContextPropagationMiddleware(accessor, options: options);
        IPipelineMessage message = new PipelineMessage("token");
        IPipelineContext? captured = null;

        await middleware.ExecuteAsync(message, () =>
        {
            captured = accessor.Context;
            return Task.CompletedTask;
        }, CancellationToken.None);

        captured!.TenantId.Should().Be("default-tenant");
    }

    [Fact]
    public async Task ExecuteAsync_WithSnapshotsEnabled_ShouldCaptureInitialAndFinalSnapshots()
    {
        var accessor = new PipelineContextAccessor();
        var options = new ContextPropagationOptions
        {
            CaptureInitialSnapshot = true,
            CaptureFinalSnapshot = true,
            EnableActivityTracing = false
        };
        var middleware = new ContextPropagationMiddleware(accessor, options: options);
        IPipelineMessage message = PipeTestHelpers.CreateMessage("payload", 42);

        await middleware.ExecuteAsync(message, () => Task.CompletedTask, CancellationToken.None);

        IPipelineContext context = message.GetContent<IPipelineContext>(ContextPropagationMiddleware.PipelineContextKey);
        context.Snapshots.Should().HaveCount(2);
        context.Snapshots[0].OperationName.Should().Be("Pipeline.Start");
        context.Snapshots[1].OperationName.Should().Be("Pipeline.End");
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_ShouldCaptureErrorSnapshotAndRethrow()
    {
        var accessor = new PipelineContextAccessor();
        var options = new ContextPropagationOptions
        {
            CaptureErrorSnapshot = true,
            EnableActivityTracing = false
        };
        var middleware = new ContextPropagationMiddleware(accessor, options: options);
        IPipelineMessage message = new PipelineMessage("token");
        IPipelineContext? captured = null;

        Func<Task> act = () => middleware.ExecuteAsync(message, () =>
        {
            captured = accessor.Context;
            throw new InvalidOperationException("pipeline failed");
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("pipeline failed");

        captured!.Snapshots.Should().ContainSingle(s => s.OperationName == "Pipeline.Error");
    }

    [Fact]
    public async Task ExecuteAsync_WithStoreContextDisabled_ShouldNotStoreContextInMessage()
    {
        var accessor = new PipelineContextAccessor();
        var options = new ContextPropagationOptions { StoreContextInMessage = false };
        var middleware = new ContextPropagationMiddleware(accessor, options: options);
        IPipelineMessage message = new PipelineMessage("token");

        await middleware.ExecuteAsync(message, () => Task.CompletedTask, CancellationToken.None);

        message.HasContent(ContextPropagationMiddleware.PipelineContextKey).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithFaultyMessage_ShouldCompleteWithoutThrowing()
    {
        var accessor = new PipelineContextAccessor();
        var middleware = new ContextPropagationMiddleware(
            accessor,
            options: new ContextPropagationOptions { EnableActivityTracing = false });
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.SetFailure();

        await middleware.ExecuteAsync(message, () => Task.CompletedTask, CancellationToken.None);

        message.IsFaulty.Should().BeTrue();
    }
}
