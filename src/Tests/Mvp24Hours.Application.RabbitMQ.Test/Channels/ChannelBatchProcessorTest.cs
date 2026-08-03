using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Channels;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Serialization;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Application.RabbitMQ.Test.Channels;

[Trait("Category", "Unit")]
public class ChannelBatchProcessorTest
{
    [Fact]
    public void ChannelBatchProcessorOptions_Defaults_ShouldHaveExpectedValues()
    {
        var options = new ChannelBatchProcessorOptions();

        options.MaxBatchSize.Should().Be(100);
        options.MinBatchSize.Should().Be(1);
        options.BatchTimeout.Should().Be(TimeSpan.FromSeconds(5));
        options.ChannelCapacity.Should().Be(200);
        options.RequeueOnFailure.Should().BeTrue();
    }

    [Fact]
    public void ChannelBatchProcessorOptions_CustomValues_ShouldBeSetCorrectly()
    {
        var options = new ChannelBatchProcessorOptions
        {
            MaxBatchSize = 50,
            MinBatchSize = 5,
            BatchTimeout = TimeSpan.FromSeconds(10),
            ChannelCapacity = 100,
            RequeueOnFailure = false
        };

        options.MaxBatchSize.Should().Be(50);
        options.MinBatchSize.Should().Be(5);
        options.BatchTimeout.Should().Be(TimeSpan.FromSeconds(10));
        options.ChannelCapacity.Should().Be(100);
        options.RequeueOnFailure.Should().BeFalse();
    }

    [Fact]
    public void BatchConsumerOptions_ChannelProcessor_ValidOptions_ShouldValidate()
    {
        var options = new BatchConsumerOptions
        {
            MaxBatchSize = 10,
            MinBatchSize = 1
        };

        Action act = () => options.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void BatchConsumerOptions_PrefetchCount_ShouldBeAtLeastMaxBatchSize()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 20 };

        options.PrefetchCount.Should().BeGreaterThanOrEqualTo((ushort)20);
    }

    [Fact]
    public void ChannelBatchProcessor_Constructor_WithNullOptions_ShouldThrow()
    {
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        Action act = () => new ChannelBatchProcessor<TestOrderEvent>(
            null!,
            provider,
            serializer,
            logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChannelBatchProcessor_Constructor_WithNullServiceProvider_ShouldThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        Action act = () => new ChannelBatchProcessor<TestOrderEvent>(
            options,
            null!,
            serializer,
            logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChannelBatchProcessor_Constructor_WithNullSerializer_ShouldThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        Action act = () => new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            null!,
            logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ChannelBatchProcessor_Constructor_WithNullLogger_ShouldThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();

        Action act = () => new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer,
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ChannelBatchProcessor_StartAndDispose_ShouldNotThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer,
            logger);

        Func<Task> act = async () =>
        {
            processor.Start();
            processor.SetQueueMetadata("test-queue", "test-exchange", "tag-1");
            await Task.CompletedTask;
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ChannelBatchProcessor_DoubleStart_ShouldNotThrow()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer,
            logger);

        Func<Task> act = async () =>
        {
            processor.Start();
            processor.Start();
            await Task.CompletedTask;
        };

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AddMessageAsync_WithValidPayload_ShouldEnqueueMessage()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 2, MinBatchSize = 1, BatchTimeout = TimeSpan.FromMilliseconds(200) };
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, TestBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;
        var channelMock = new Mock<IModel>();

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer,
            logger,
            channel: channelMock.Object);

        processor.Start();
        processor.SetQueueMetadata("orders", "exchange", "tag-1");

        BasicDeliverEventArgs eventArgs = CreateEventArgs(new TestOrderEvent { Name = "Order-1" }, deliveryTag: 1);
        await processor.AddMessageAsync(eventArgs);

        await Task.Delay(300);
        channelMock.Verify(c => c.BasicAck(1, false), Times.Once);
    }

    [Fact]
    public async Task AddMessageAsync_WithInvalidPayload_ShouldNackWithoutRequeue()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 1, MinBatchSize = 1, BatchTimeout = TimeSpan.FromMilliseconds(100), RequeueOnFailure = false };
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new Mock<IMessageSerializer>();
        serializer.Setup(s => s.Deserialize<TestOrderEvent>(It.IsAny<byte[]>())).Returns((TestOrderEvent?)null);
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;
        var channelMock = new Mock<IModel>();

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer.Object,
            logger,
            channel: channelMock.Object);

        processor.Start();
        await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent(), deliveryTag: 9));

        await Task.Delay(150);
        channelMock.Verify(c => c.BasicNack(9, false, false), Times.Once);
    }

    [Fact]
    public async Task AddMessageAsync_WhenDeserializationThrows_ShouldNackWithoutRequeue()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 1, MinBatchSize = 1, BatchTimeout = TimeSpan.FromMilliseconds(100) };
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new Mock<IMessageSerializer>();
        serializer.Setup(s => s.Deserialize<TestOrderEvent>(It.IsAny<byte[]>())).Throws(new InvalidOperationException("bad json"));
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;
        var channelMock = new Mock<IModel>();

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer.Object,
            logger,
            channel: channelMock.Object);

        processor.Start();
        await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent(), deliveryTag: 3));

        await Task.Delay(150);
        channelMock.Verify(c => c.BasicNack(3, false, false), Times.Once);
    }

    [Fact]
    public async Task AddMessageAsync_WithPartialBatchResults_ShouldNackFailedMessages()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 2, MinBatchSize = 1, BatchTimeout = TimeSpan.FromMilliseconds(200), RequeueOnFailure = true };
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, PartialFailureBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;
        var channelMock = new Mock<IModel>();

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options,
            provider,
            serializer,
            logger,
            channel: channelMock.Object);

        processor.Start();
        await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent { Name = "A" }, deliveryTag: 10));
        await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent { Name = "B" }, deliveryTag: 11));

        await Task.Delay(400);
        channelMock.Verify(c => c.BasicNack(10, false, true), Times.Once);
        channelMock.Verify(c => c.BasicAck(11, false), Times.Once);
    }

    [Fact]
    public async Task AddMessageAsync_AfterDispose_ShouldThrowObjectDisposedException()
    {
        BatchConsumerOptions options = BatchConsumerOptions.Default;
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;

        var processor = new ChannelBatchProcessor<TestOrderEvent>(options, provider, serializer, logger);
        await processor.DisposeAsync();

        Func<Task> act = () => processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent(), 1)).AsTask();

        await act.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task AddMessageAsync_WithoutRegisteredConsumer_ShouldAckAllMessages()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 2, MinBatchSize = 1, BatchTimeout = TimeSpan.FromMilliseconds(200) };
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;
        var channelMock = new Mock<IModel>();

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options, provider, serializer, logger, channel: channelMock.Object);

        processor.Start();
        await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent { Name = "no-consumer" }, deliveryTag: 20));

        await WaitUntilAsync(() => channelMock.Invocations.Any(i => i.Method.Name == "BasicAck"), TimeSpan.FromSeconds(5));

        channelMock.Verify(c => c.BasicAck(20, false), Times.Once);
    }

    [Fact]
    public async Task AddMessageAsync_WhenConsumerThrows_ShouldNackAllMessages()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 1, MinBatchSize = 1, BatchTimeout = TimeSpan.FromMilliseconds(200), RequeueOnFailure = true };
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, ThrowingBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;
        var channelMock = new Mock<IModel>();

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options, provider, serializer, logger, channel: channelMock.Object);

        processor.Start();
        await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent { Name = "throw" }, deliveryTag: 21));

        await WaitUntilAsync(() => channelMock.Invocations.Any(i => i.Method.Name == "BasicNack"), TimeSpan.FromSeconds(5));

        channelMock.Verify(c => c.BasicNack(21, false, true), Times.Once);
    }

    [Fact]
    public async Task AddMessageAsync_WhenMaxBatchSizeReached_ShouldProcessFullBatch()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 3, MinBatchSize = 1, BatchTimeout = TimeSpan.FromSeconds(5) };
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, TestBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;
        var channelMock = new Mock<IModel>();

        await using var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options, provider, serializer, logger, channel: channelMock.Object);

        processor.Start();
        for (ulong tag = 30; tag < 33; tag++)
        {
            await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent { Name = $"batch-{tag}" }, deliveryTag: tag));
        }

        await WaitUntilAsync(() => channelMock.Invocations.Count(i => i.Method.Name == "BasicAck") >= 3, TimeSpan.FromSeconds(5));

        channelMock.Verify(c => c.BasicAck(It.IsAny<ulong>(), false), Times.Exactly(3));
    }

    [Fact]
    public async Task FlushAsync_ShouldProcessPendingMessages()
    {
        var options = new BatchConsumerOptions { MaxBatchSize = 10, MinBatchSize = 5, BatchTimeout = TimeSpan.FromSeconds(30) };
        var services = new ServiceCollection();
        services.AddSingleton<IBatchConsumer<TestOrderEvent>, TestBatchConsumer>();
        ServiceProvider provider = services.BuildServiceProvider();
        var serializer = new JsonMessageSerializer();
        ILogger<ChannelBatchProcessor<TestOrderEvent>> logger = NullLogger<ChannelBatchProcessor<TestOrderEvent>>.Instance;
        var channelMock = new Mock<IModel>();

        var processor = new ChannelBatchProcessor<TestOrderEvent>(
            options, provider, serializer, logger, channel: channelMock.Object);

        processor.Start();
        await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent { Name = "flush-1" }, deliveryTag: 40));
        await processor.AddMessageAsync(CreateEventArgs(new TestOrderEvent { Name = "flush-2" }, deliveryTag: 41));
        await processor.FlushAsync();

        channelMock.Verify(c => c.BasicAck(40, false), Times.Once);
        channelMock.Verify(c => c.BasicAck(41, false), Times.Once);
        await processor.DisposeAsync();
    }

    private static async Task WaitUntilAsync(Func<bool> condition, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (!condition())
        {
            await Task.Delay(50, cts.Token);
        }
    }

    private static BasicDeliverEventArgs CreateEventArgs(TestOrderEvent message, ulong deliveryTag)
    {
        byte[] body = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(message));
        var propertiesMock = new Mock<IBasicProperties>();
        return new BasicDeliverEventArgs
        {
            Body = body,
            DeliveryTag = deliveryTag,
            BasicProperties = propertiesMock.Object,
            RoutingKey = "orders",
            Exchange = "exchange"
        };
    }

    private sealed class TestBatchConsumer : IBatchConsumer<TestOrderEvent>
    {
        public Task<IEnumerable<IBatchMessageResult>?> ConsumeAsync(IBatchConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
        {
            IEnumerable<IBatchMessageResult> results = context.Messages
                .Select(m => (IBatchMessageResult)BatchMessageResult.Ack(m.DeliveryTag));
            return Task.FromResult<IEnumerable<IBatchMessageResult>?>(results);
        }
    }

    private sealed class PartialFailureBatchConsumer : IBatchConsumer<TestOrderEvent>
    {
        public Task<IEnumerable<IBatchMessageResult>?> ConsumeAsync(IBatchConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
        {
            IEnumerable<IBatchMessageResult> results = context.Messages.Select(m =>
                m.DeliveryTag % 2 == 1
                    ? (IBatchMessageResult)BatchMessageResult.Ack(m.DeliveryTag)
                    : BatchMessageResult.Nack(m.DeliveryTag, requeue: true));
            return Task.FromResult<IEnumerable<IBatchMessageResult>?>(results);
        }
    }

    private sealed class ThrowingBatchConsumer : IBatchConsumer<TestOrderEvent>
    {
        public Task<IEnumerable<IBatchMessageResult>?> ConsumeAsync(IBatchConsumeContext<TestOrderEvent> context, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("batch consumer failure");
        }
    }
}
