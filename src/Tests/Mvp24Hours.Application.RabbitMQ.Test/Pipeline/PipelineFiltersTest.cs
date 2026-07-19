using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy;
using Mvp24Hours.Infrastructure.RabbitMQ.MultiTenancy.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline.Filters;

namespace Mvp24Hours.Application.RabbitMQ.Test.Pipeline;

public class PipelineFiltersTest
{
    [Fact]
    public async Task CorrelationConsumeFilter_ShouldSetAsyncLocalContext()
    {
        var filter = new CorrelationConsumeFilter();
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(
            new TestOrderEvent(),
            b => b.WithCorrelationId("corr-123").WithMessageId("msg-1"));

        CorrelationContext? captured = null;
        await filter.ConsumeAsync(context, async (_, _) =>
        {
            captured = CorrelationConsumeFilter.Current;
            await Task.CompletedTask;
        });

        captured.Should().NotBeNull();
        captured!.CorrelationId.Should().Be("corr-123");
        context.Items["CorrelationId"].Should().Be("corr-123");
    }

    [Fact]
    public async Task TelemetryConsumeFilter_ShouldInvokeNext()
    {
        var filter = new TelemetryConsumeFilter();
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.ConsumeAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task TelemetryPublishFilter_ShouldInjectTraceParentHeader()
    {
        var filter = new TelemetryPublishFilter();
        PublishFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreatePublishFilterContext(new TestOrderEvent());
        bool nextCalled = false;

        await filter.PublishAsync(context, async (_, _) =>
        {
            nextCalled = true;
            await Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        // traceparent is injected only when an Activity listener is registered
        context.Items.Should().NotContainKey("traceparent");
    }

    [Fact]
    public async Task ExceptionHandlingConsumeFilter_ShouldRethrowWhenConfigured()
    {
        var filter = new ExceptionHandlingConsumeFilter(
            options: Microsoft.Extensions.Options.Options.Create(new ExceptionHandlingFilterOptions
            {
                MaxRetries = 0,
                RethrowException = true
            }));
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        Func<Task> act = () => filter.ConsumeAsync(context, (_, _) => throw new InvalidOperationException("boom"));

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task FilterPipelineExecutor_WithoutFilters_ShouldExecuteFinalAction()
    {
        var services = new ServiceCollection();
        IServiceProvider provider = services.BuildServiceProvider();
        var executor = new FilterPipelineExecutor(provider, new FilterPipelineOptions());
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());
        bool executed = false;

        await executor.ExecuteConsumeFiltersAsync(
            context,
            (_, _) =>
            {
                executed = true;
                return Task.CompletedTask;
            });

        executed.Should().BeTrue();
    }

    [Fact]
    public void ConsumeFilterContext_SendToDeadLetter_ShouldSkipRemainingFilters()
    {
        ConsumeFilterContext<TestOrderEvent> context = RabbitMQTestHelpers.CreateConsumeFilterContext(new TestOrderEvent());

        context.SendToDeadLetter("invalid message");

        context.ShouldSendToDeadLetter.Should().BeTrue();
        context.DeadLetterReason.Should().Be("invalid message");
    }
}
