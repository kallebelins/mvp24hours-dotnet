using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

[Trait("Category", "Unit")]
public class PipelineLoggingFiltersCoverageTest
{
    [Fact]
    public async Task LoggingSendFilter_OnSuccess_ShouldInvokeNext()
    {
        var filter = new LoggingSendFilter(NullLogger<LoggingSendFilter>.Instance);
        SendFilterContext<TestOrderEvent> context = CreateSendContext();
        bool nextCalled = false;

        await filter.SendAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task LoggingSendFilter_WhenCancelled_ShouldNotThrow()
    {
        var filter = new LoggingSendFilter(NullLogger<LoggingSendFilter>.Instance);
        SendFilterContext<TestOrderEvent> context = CreateSendContext();

        await filter.SendAsync(context, (_, _) =>
        {
            context.CancelSend("test cancel");
            return Task.CompletedTask;
        });

        context.ShouldCancelSend.Should().BeTrue();
    }

    [Fact]
    public async Task LoggingSendFilter_WhenNextThrows_ShouldRethrow()
    {
        var filter = new LoggingSendFilter(NullLogger<LoggingSendFilter>.Instance);
        SendFilterContext<TestOrderEvent> context = CreateSendContext();

        Func<Task> act = () => filter.SendAsync(context, (_, _) => throw new InvalidOperationException("send failed"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task LoggingPublishFilter_OnSuccess_ShouldInvokeNext()
    {
        var filter = new LoggingPublishFilter(NullLogger<LoggingPublishFilter>.Instance);
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.PublishAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task LoggingPublishFilter_WhenCancelled_ShouldNotThrow()
    {
        var filter = new LoggingPublishFilter(NullLogger<LoggingPublishFilter>.Instance);
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent());

        await filter.PublishAsync(context, (_, _) =>
        {
            context.CancelPublish("test cancel");
            return Task.CompletedTask;
        });

        context.ShouldCancelPublish.Should().BeTrue();
    }

    [Fact]
    public async Task LoggingPublishFilter_WhenNextThrows_ShouldRethrow()
    {
        var filter = new LoggingPublishFilter(NullLogger<LoggingPublishFilter>.Instance);
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent());

        Func<Task> act = () => filter.PublishAsync(context, (_, _) => throw new InvalidOperationException("publish failed"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static SendFilterContext<TestOrderEvent> CreateSendContext()
    {
        return new SendFilterContext<TestOrderEvent>(
            new TestOrderEvent(),
            "destination-queue",
            new ServiceCollection().BuildServiceProvider(),
            correlationId: "corr-1");
    }
}
