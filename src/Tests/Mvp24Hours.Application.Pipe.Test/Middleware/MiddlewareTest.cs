using Mvp24Hours.Application.Pipe.Test.Support;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Middleware;

namespace Mvp24Hours.Application.Pipe.Test.Middleware;

[Trait("Category", "Unit")]
public class MiddlewareTest
{
    [Fact]
    public async Task TimeoutPipelineMiddleware_Should_ThrowWhenOperationExceedsTimeout()
    {
        var middleware = new TimeoutPipelineMiddleware(TimeSpan.FromMilliseconds(100));
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        Func<Task> act = () => middleware.ExecuteAsync(message, () => Task.Delay(500));

        await act.Should().ThrowAsync<PipelineTimeoutException>()
            .Where(ex => ex.Timeout == TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void TimeoutPipelineMiddleware_Should_RejectNonPositiveTimeout()
    {
        Action act = () => _ = new TimeoutPipelineMiddleware(TimeSpan.Zero);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task PipelineMiddlewareExecutor_Should_ExecuteInOrderValue()
    {
        var order = new List<int>();
        var middlewares = new IPipelineMiddleware[]
        {
            new OrderTrackingMiddleware(-100, order),
            new OrderTrackingMiddleware(-200, order),
            new OrderTrackingMiddleware(-300, order)
        };
        IPipelineMessage message = PipeTestHelpers.CreateMessage();

        await PipelineMiddlewareExecutor.ExecuteAsync(
            middlewares,
            message,
            () =>
            {
                order.Add(0);
                return Task.CompletedTask;
            });

        order.Should().Equal(-300, -200, -100, 0);
    }

    [Fact]
    public async Task PipelineMiddlewareExecutor_Should_RunCoreActionWhenNoMiddlewares()
    {
        bool executed = false;
        await PipelineMiddlewareExecutor.ExecuteAsync([], PipeTestHelpers.CreateMessage(), () =>
        {
            executed = true;
            return Task.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Fact]
    public void PipelineMiddlewareExecutor_Sync_Should_ExecuteInOrder()
    {
        var order = new List<int>();
        var middlewares = new IPipelineMiddlewareSync[]
        {
            new SyncOrderTrackingMiddleware(10, order),
            new SyncOrderTrackingMiddleware(5, order)
        };

        PipelineMiddlewareExecutor.Execute(middlewares, PipeTestHelpers.CreateMessage(), () => order.Add(0));

        order.Should().Equal(5, 10, 0);
    }

    private sealed class OrderTrackingMiddleware(int order, List<int> log) : IPipelineMiddleware
    {
        public int Order => order;

        public Task ExecuteAsync(IPipelineMessage message, Func<Task> next, CancellationToken cancellationToken = default)
        {
            log.Add(order);
            return next();
        }
    }

    private sealed class SyncOrderTrackingMiddleware(int order, List<int> log) : IPipelineMiddlewareSync
    {
        public int Order => order;

        public void Execute(IPipelineMessage message, Action next)
        {
            log.Add(order);
            next();
        }
    }
}
