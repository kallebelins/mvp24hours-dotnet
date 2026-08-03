using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Middleware;

namespace Mvp24Hours.Application.Pipe.Test.Middleware;

[Trait("Category", "Unit")]
public class LoggingPipelineMiddlewareTest
{
    [Fact]
    public async Task LoggingPipelineMiddleware_ShouldInvokeNextOnSuccess()
    {
        var middleware = new LoggingPipelineMiddleware();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        bool nextCalled = false;

        await middleware.ExecuteAsync(message, () =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        middleware.Order.Should().Be(-1000);
    }

    [Fact]
    public async Task LoggingPipelineMiddleware_ShouldRethrowOnFailure()
    {
        var middleware = new LoggingPipelineMiddleware();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Func<Task> act = () => middleware.ExecuteAsync(message, () =>
            throw new InvalidOperationException("log-failure"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("log-failure");
    }

    [Fact]
    public void LoggingPipelineMiddlewareSync_ShouldInvokeNextOnSuccess()
    {
        var middleware = new LoggingPipelineMiddlewareSync();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();
        bool nextCalled = false;

        middleware.Execute(message, () => nextCalled = true);

        nextCalled.Should().BeTrue();
        middleware.Order.Should().Be(-1000);
    }

    [Fact]
    public void LoggingPipelineMiddlewareSync_ShouldRethrowOnFailure()
    {
        var middleware = new LoggingPipelineMiddlewareSync();
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Action act = () => middleware.Execute(message, () =>
            throw new InvalidOperationException("sync-log-failure"));

        act.Should().Throw<InvalidOperationException>().WithMessage("sync-log-failure");
    }
}
