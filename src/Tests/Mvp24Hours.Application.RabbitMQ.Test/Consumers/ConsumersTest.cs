using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.Consumers;

[Trait("Category", "Unit")]
public class ConsumersTest
{
    [Fact]
    public void BatchMessageResult_Ack_ShouldMarkSuccess()
    {
        var result = BatchMessageResult.Ack(42);

        result.DeliveryTag.Should().Be(42ul);
        result.Success.Should().BeTrue();
        result.Requeue.Should().BeFalse();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void BatchMessageResult_Nack_ShouldMarkFailure()
    {
        var result = BatchMessageResult.Nack(7, requeue: false, errorMessage: "failed");

        result.Success.Should().BeFalse();
        result.Requeue.Should().BeFalse();
        result.ErrorMessage.Should().Be("failed");
    }

    [Fact]
    public void BatchMessageResult_Extensions_ShouldAggregateResults()
    {
        IBatchMessageResult[] results =
        [
            BatchMessageResult.Ack(1),
            BatchMessageResult.Nack(2, requeue: true),
            BatchMessageResult.Nack(3, requeue: false, errorMessage: "dead")
        ];

        results.SuccessCount().Should().Be(1);
        results.FailureCount().Should().Be(2);
        results.AllSucceeded().Should().BeFalse();
        results.AnyFailed().Should().BeTrue();
        results.ToRequeue().Should().HaveCount(1);
        results.ToDeadLetter().Should().HaveCount(1);
    }

    [Fact]
    public async Task BatchProcessingHelper_ProcessSequentiallyAsync_ShouldAckAllOnSuccess()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(3);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessSequentiallyAsync(
            context,
            (_, _) => Task.FromResult(true));

        results.Should().HaveCount(3);
        results.AllSucceeded().Should().BeTrue();
    }

    [Fact]
    public async Task BatchProcessingHelper_ProcessSequentiallyAsync_StopOnFirstError_ShouldNackRemaining()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(3);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessSequentiallyAsync(
            context,
            (item, _) => Task.FromResult(item.DeliveryTag == 1),
            stopOnFirstError: true);

        results.AnyFailed().Should().BeTrue();
        results.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task BatchProcessingHelper_ProcessInParallelAsync_ShouldHandleExceptions()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(2);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessInParallelAsync(
            context,
            (item, _) => item.DeliveryTag == 1
                ? Task.FromException<bool>(new InvalidOperationException("boom"))
                : Task.FromResult(true));

        results.AnyFailed().Should().BeTrue();
    }

    [Fact]
    public async Task BatchProcessingHelper_ProcessAsTransactionAsync_ShouldAckAllOnSuccess()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(2);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessAsTransactionAsync(
            context,
            (_, _) => Task.FromResult(true));

        results.AllSucceeded().Should().BeTrue();
    }

    [Fact]
    public async Task BatchProcessingHelper_ProcessByGroupAsync_ShouldGroupByKey()
    {
        IBatchConsumeContext<TestOrderEvent> context = CreateBatchContext(3, tagOffset: 0);

        IEnumerable<IBatchMessageResult> results = await BatchProcessingHelper.ProcessByGroupAsync(
            context,
            item => item.Message.Name,
            (_, _, _) => Task.FromResult(true));

        results.Should().HaveCount(3);
        results.AllSucceeded().Should().BeTrue();
    }

    [Fact]
    public void ConsumeContext_GetHeader_ShouldParseByteArrayHeader()
    {
        var headers = new Dictionary<string, object>
        {
            ["x-tenant-id"] = System.Text.Encoding.UTF8.GetBytes("tenant-1")
        };

        var context = new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent(),
            RabbitMQTestHelpers.CreateDeliverEventArgs(headers: headers),
            new ServiceCollection().BuildServiceProvider());

        context.GetHeader<string>("x-tenant-id").Should().Be("tenant-1");
    }

    [Fact]
    public async Task ConsumeContext_PublishAsync_WithoutClient_ShouldThrow()
    {
        var context = new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent(),
            RabbitMQTestHelpers.CreateDeliverEventArgs(),
            new ServiceCollection().BuildServiceProvider());

        Func<Task> act = () => context.PublishAsync(new TestOrderEvent());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*client is not available*");
    }

    [Fact]
    public async Task ConsumeContext_RespondAsync_WithoutReplyTo_ShouldThrow()
    {
        var context = new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent(),
            RabbitMQTestHelpers.CreateDeliverEventArgs(),
            new ServiceCollection().BuildServiceProvider(),
            rabbitMQClient: RabbitMQTestHelpers.CreateInMemoryBus());

        Func<Task> act = () => context.RespondAsync(new TestOrderResponse { Success = true });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*reply-to*");
    }

    [Fact]
    public async Task BatchConsumeContext_PublishAsync_ShouldUseInMemoryBus()
    {
        InMemoryBus bus = RabbitMQTestHelpers.CreateInMemoryBus();
        var items = new List<IBatchMessageItem<TestOrderEvent>>
        {
            new BatchMessageItem<TestOrderEvent>(
                new TestOrderEvent { Name = "a" },
                RabbitMQTestHelpers.CreateDeliverEventArgs(deliveryTag: 1))
        };

        var context = new BatchConsumeContext<TestOrderEvent>(
            items,
            new ServiceCollection().BuildServiceProvider(),
            bus,
            queueName: "orders",
            exchange: "test-exchange");

        await context.PublishAsync(new TestOrderEvent { Name = "published" }, "order-route");

        bus.WasPublished<TestOrderEvent>().Should().BeTrue();
    }

    private static IBatchConsumeContext<TestOrderEvent> CreateBatchContext(int count, int tagOffset = 1)
    {
        var items = new List<IBatchMessageItem<TestOrderEvent>>();
        for (int i = 0; i < count; i++)
        {
            items.Add(new BatchMessageItem<TestOrderEvent>(
                new TestOrderEvent { Name = i % 2 == 0 ? "group-a" : "group-b" },
                RabbitMQTestHelpers.CreateDeliverEventArgs(deliveryTag: (ulong)(tagOffset + i))));
        }

        return new BatchConsumeContext<TestOrderEvent>(
            items,
            new ServiceCollection().BuildServiceProvider());
    }
}
