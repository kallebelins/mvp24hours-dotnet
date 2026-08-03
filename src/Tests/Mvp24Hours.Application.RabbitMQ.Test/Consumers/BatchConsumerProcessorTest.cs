using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Application.RabbitMQ.Test.Consumers;

public class BatchConsumerProcessorTest
{
    private static BatchConsumerOptions CreateOptions(Action<BatchConsumerOptions>? configure = null)
    {
        var options = new BatchConsumerOptions
        {
            MaxBatchSize = 5,
            MinBatchSize = 1,
            BatchTimeout = TimeSpan.FromMilliseconds(500),
            MessageWaitTimeout = TimeSpan.FromMilliseconds(100),
            PrefetchCount = 5
        };
        configure?.Invoke(options);
        return options;
    }

    private static BasicDeliverEventArgs CreateEventArgs(byte[] body, ulong deliveryTag = 1)
    {
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();

        return new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: deliveryTag,
            redelivered: false,
            exchange: "ex",
            routingKey: "rk",
            properties: propertiesMock.Object,
            body: body);
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrow()
    {
        ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var serializer = new JsonMessageSerializer();

        Action act = () => new BatchConsumerProcessor<TestOrderEvent>(
            null!,
            services,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddMessageAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(),
            services,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance);

        processor.Dispose();

        Func<Task> act = () => processor.AddMessageAsync(CreateEventArgs("not-json"u8.ToArray()));

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task AddMessageAsync_WithInvalidPayload_ShouldNackWithoutRequeue()
    {
        var channelMock = new Mock<IModel>();
        ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        using var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(),
            services,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
            channel: channelMock.Object);

        await processor.AddMessageAsync(CreateEventArgs("invalid-json"u8.ToArray(), deliveryTag: 9));

        channelMock.Verify(c => c.BasicNack(9, false, false), Times.Once);
    }

    [Fact]
    public async Task FlushAsync_WithEmptyQueue_ShouldNotThrow()
    {
        ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        using var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(),
            services,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance);

        Func<Task> act = () => processor.FlushAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessBatch_WithoutRegisteredConsumer_ShouldBatchAckWhenEnabled()
    {
        var channelMock = new Mock<IModel>();
        ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        using var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(o => o.UseBatchAcknowledgment = true),
            services,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
            channel: channelMock.Object);

        byte[] body = serializer.Serialize(new TestOrderEvent { Name = "one" });
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 1));
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 2));
        await processor.FlushAsync();

        channelMock.Verify(c => c.BasicAck(2, true), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_WithIndividualAck_ShouldAckEachMessage()
    {
        var channelMock = new Mock<IModel>();
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, TestOrderBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();
        TestOrderBatchConsumer.Reset();

        var serializer = new JsonMessageSerializer();
        using var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(o => o.UseBatchAcknowledgment = false),
            provider,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
            channel: channelMock.Object);

        byte[] body = serializer.Serialize(new TestOrderEvent { Name = "ack-each" });
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 10));
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 11));
        await processor.FlushAsync();

        channelMock.Verify(c => c.BasicAck(10, false), Times.Once);
        channelMock.Verify(c => c.BasicAck(11, false), Times.Once);
        TestOrderBatchConsumer.ProcessedCount.Should().Be(2);
    }

    [Fact]
    public async Task ProcessBatch_WithPartialResults_ShouldAckAndNackIndividually()
    {
        var channelMock = new Mock<IModel>();
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, PartialResultBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();

        var serializer = new JsonMessageSerializer();
        using var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(o => o.UseBatchAcknowledgment = false),
            provider,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
            channel: channelMock.Object);

        byte[] body = serializer.Serialize(new TestOrderEvent { Name = "partial" });
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 20));
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 21));
        await processor.FlushAsync();

        channelMock.Verify(c => c.BasicAck(20, false), Times.Once);
        channelMock.Verify(c => c.BasicNack(21, false, false), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_WhenConsumerThrows_ShouldNackAllWithRequeue()
    {
        var channelMock = new Mock<IModel>();
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, ThrowingBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();

        var serializer = new JsonMessageSerializer();
        using var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(o => o.RequeueOnFailure = true),
            provider,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
            channel: channelMock.Object);

        byte[] body = serializer.Serialize(new TestOrderEvent { Name = "fail" });
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 30));
        await processor.FlushAsync();

        channelMock.Verify(c => c.BasicNack(30, false, true), Times.Once);
    }

    [Fact]
    public async Task ProcessBatch_WithParallelProcessingEnabled_ShouldInvokeConsumer()
    {
        var channelMock = new Mock<IModel>();
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, TestOrderBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();
        TestOrderBatchConsumer.Reset();

        var serializer = new JsonMessageSerializer();
        using var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(o => o.EnableParallelProcessing = true),
            provider,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
            channel: channelMock.Object);

        byte[] body = serializer.Serialize(new TestOrderEvent { Name = "parallel" });
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 40));
        await processor.FlushAsync();

        TestOrderBatchConsumer.ProcessedCount.Should().Be(1);
    }

    [Fact]
    public async Task Dispose_WithPendingMessages_ShouldNackRemaining()
    {
        var channelMock = new Mock<IModel>();
        ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        var processor = new BatchConsumerProcessor<TestOrderEvent>(
            CreateOptions(),
            services,
            serializer,
            NullLogger<BatchConsumerProcessor<TestOrderEvent>>.Instance,
            channel: channelMock.Object);

        byte[] body = serializer.Serialize(new TestOrderEvent { Name = "pending" });
        await processor.AddMessageAsync(CreateEventArgs(body, deliveryTag: 50));
        processor.Dispose();

        channelMock.Verify(c => c.BasicNack(50, false, true), Times.Once);
    }

    private sealed class PartialResultBatchConsumer : IBatchConsumer<TestOrderEvent>
    {
        public Task<IEnumerable<IBatchMessageResult>?> ConsumeAsync(
            IBatchConsumeContext<TestOrderEvent> context,
            CancellationToken cancellationToken = default)
        {
            IBatchMessageResult[] results =
            [
                BatchMessageResult.Ack(20),
                BatchMessageResult.Nack(21, requeue: false, errorMessage: "bad")
            ];
            return Task.FromResult<IEnumerable<IBatchMessageResult>?>(results);
        }
    }

    private sealed class ThrowingBatchConsumer : IBatchConsumer<TestOrderEvent>
    {
        public Task<IEnumerable<IBatchMessageResult>?> ConsumeAsync(
            IBatchConsumeContext<TestOrderEvent> context,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("batch failed");
        }
    }
}
