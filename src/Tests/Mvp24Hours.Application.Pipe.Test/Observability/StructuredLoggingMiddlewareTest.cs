using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Context;
using Mvp24Hours.Infrastructure.Pipe.Observability;
using Mvp24Hours.Infrastructure.Testing.Logging;

namespace Mvp24Hours.Application.Pipe.Test.Observability;

[Trait("Category", "Unit")]
public class StructuredLoggingMiddlewareTest
{
    [Fact]
    public async Task ExecuteAsync_ShouldLogStartAndSuccess()
    {
        var logger = new FakeLogger<StructuredLoggingMiddleware>();
        var middleware = new StructuredLoggingMiddleware(logger);
        IPipelineMessage message = new PipelineMessage("corr-token");
        bool nextCalled = false;

        await middleware.ExecuteAsync(message, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        logger.ContainsLog(LogLevel.Information, "Pipeline operation starting").Should().BeTrue();
        logger.ContainsLog(LogLevel.Information, "completed successfully").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithFaultyMessage_ShouldLogWarningOnCompletion()
    {
        var logger = new FakeLogger<StructuredLoggingMiddleware>();
        var middleware = new StructuredLoggingMiddleware(logger);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        message.SetFailure();

        await middleware.ExecuteAsync(message, () => Task.CompletedTask);

        logger.ContainsLog(LogLevel.Warning, "completed with faults").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenNextThrows_ShouldLogErrorAndRethrow()
    {
        var logger = new FakeLogger<StructuredLoggingMiddleware>();
        var middleware = new StructuredLoggingMiddleware(logger);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Func<Task> act = () => middleware.ExecuteAsync(message, () =>
            throw new InvalidOperationException("pipeline failed"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        logger.ContainsLog(LogLevel.Error, "failed with exception").Should().BeTrue();
        logger.ContainsException<InvalidOperationException>().Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelled_ShouldLogWarningAndRethrow()
    {
        var logger = new FakeLogger<StructuredLoggingMiddleware>();
        var middleware = new StructuredLoggingMiddleware(logger);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => middleware.ExecuteAsync(message, () => Task.Delay(1000, cts.Token), cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        logger.ContainsLog(LogLevel.Warning, "cancelled").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseContextAccessorCorrelationId()
    {
        var logger = new FakeLogger<StructuredLoggingMiddleware>();
        var accessor = new PipelineContextAccessor
        {
            Context = new PipelineContext("ctx-correlation")
        };
        var middleware = new StructuredLoggingMiddleware(logger, accessor);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () => Task.CompletedTask);

        logger.ContainsLog(LogLevel.Information, "ctx-correlation").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithSlowOperation_ShouldLogSlowWarning()
    {
        var logger = new FakeLogger<StructuredLoggingMiddleware>();
        var options = new StructuredLoggingOptions
        {
            SlowOperationThreshold = TimeSpan.FromMilliseconds(1)
        };
        var middleware = new StructuredLoggingMiddleware(logger, options: options);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, async () => await Task.Delay(20));

        logger.ContainsLog(LogLevel.Warning, "Slow pipeline operation").Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledLogs_ShouldNotLogStartOrEnd()
    {
        var logger = new FakeLogger<StructuredLoggingMiddleware>();
        var options = new StructuredLoggingOptions
        {
            LogOperationStart = false,
            LogOperationEnd = false
        };
        var middleware = new StructuredLoggingMiddleware(logger, options: options);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await middleware.ExecuteAsync(message, () => Task.CompletedTask);

        logger.GetLogs(LogLevel.Information).Should().BeEmpty();
    }

    [Fact]
    public void Order_ShouldReflectConfiguredMiddlewareOrder()
    {
        var middleware = new StructuredLoggingMiddleware(
            new FakeLogger<StructuredLoggingMiddleware>(),
            options: new StructuredLoggingOptions { MiddlewareOrder = -500 });

        middleware.Order.Should().Be(-500);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new StructuredLoggingMiddleware(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ExecuteSync_ShouldLogStartAndCompletion()
    {
        var logger = new FakeLogger<StructuredLoggingMiddlewareSync>();
        var middleware = new StructuredLoggingMiddlewareSync(logger);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        bool nextCalled = false;

        middleware.Execute(message, () => nextCalled = true);

        nextCalled.Should().BeTrue();
        logger.ContainsLog(LogLevel.Information, "Pipeline operation starting").Should().BeTrue();
        logger.ContainsLog(LogLevel.Information, "Pipeline operation completed").Should().BeTrue();
    }

    [Fact]
    public void ExecuteSync_WhenNextThrows_ShouldLogErrorAndRethrow()
    {
        var logger = new FakeLogger<StructuredLoggingMiddlewareSync>();
        var middleware = new StructuredLoggingMiddlewareSync(logger);
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Action act = () => middleware.Execute(message, () =>
            throw new InvalidOperationException("sync failed"));

        act.Should().Throw<InvalidOperationException>();
        logger.ContainsLog(LogLevel.Error, "Pipeline operation failed").Should().BeTrue();
    }
}
