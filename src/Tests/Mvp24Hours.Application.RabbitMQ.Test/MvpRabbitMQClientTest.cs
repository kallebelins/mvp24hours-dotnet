using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Dto;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Application.RabbitMQ.Test;

[Trait("Category", "Unit")]
public class MvpRabbitMQClientTest
{
    [Fact]
    public void Publish_WithoutRoutingKeyOrDefault_ShouldThrow()
    {
        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions(defaultRoutingKey: string.Empty);
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(options);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        Action act = () => client.Publish(new CustomerEvent { Id = 1, Name = "x" }, routingKey: string.Empty);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("routingKey");
    }

    [Fact]
    public void Publish_WithMockConnection_ShouldReturnMessageId()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = client.Publish(
            new CustomerEvent { Id = 1, Name = "publish" },
            routingKey: "customer-event");

        messageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Publish_WithDefaultRoutingKey_ShouldUseConfiguredRoute()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = client.Publish(new CustomerEvent { Id = 2, Name = "default" }, routingKey: string.Empty);

        messageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Publish_WithCustomToken_ShouldPreserveToken()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();
        const string token = "custom-token-123";

        string messageId = client.Publish(
            new CustomerEvent { Id = 3, Name = "token" },
            routingKey: "customer-event",
            tokenDefault: token);

        messageId.Should().Be(token);
    }

    [Fact]
    public async Task PublishAsync_ShouldReturnMessageId()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = await client.PublishAsync(
            new CustomerEvent { Id = 4, Name = "async" },
            routingKey: "customer-event");

        messageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InMemoryBus_PublishBatch_ShouldReturnAllMessageIds()
    {
        InMemoryBus bus = RabbitMQTestHelpers.CreateInMemoryBus();

        IEnumerable<string> ids = bus.PublishBatch([
            (new TestOrderEvent { Name = "batch-1" }, "route-1"),
            (new TestOrderEvent { Name = "batch-2" }, "route-2")
        ]);

        ids.Should().HaveCount(2);
        bus.PublishedCount<TestOrderEvent>().Should().Be(2);
    }

    [Fact]
    public void Register_AndUnregister_ShouldNotThrow()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        Action act = () =>
        {
            client.Register<CustomerConsumer>();
            client.Unregister<CustomerConsumer>();
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void Consume_WithoutRegisteredConsumers_ShouldThrow()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        Action act = () => client.Consume();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*didn't find consumers*");
    }

    [Fact]
    public void Register_WithNullType_ShouldThrow()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        Action act = () => client.Register((Type)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Unregister_WithNullType_ShouldThrow()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        Action act = () => client.Unregister((Type)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PublishWithTtl_ShouldReturnMessageId()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = client.PublishWithTtl(
            new CustomerEvent { Id = 7, Name = "ttl" },
            routingKey: "customer-event",
            ttlMilliseconds: 60_000);

        messageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Publish_WithHeaders_ShouldReturnMessageId()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = client.Publish(
            new CustomerEvent { Id = 8, Name = "headers" },
            routingKey: "customer-event",
            headers: new Dictionary<string, object> { ["x-custom"] = "value" });

        messageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Publish_WithPriority_ShouldReturnMessageId()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = client.Publish(
            new CustomerEvent { Id = 9, Name = "priority" },
            routingKey: "customer-event",
            priority: 5);

        messageId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void InMemoryBus_AsClientAlternative_ShouldTrackPublishedMessages()
    {
        InMemoryBus bus = RabbitMQTestHelpers.CreateInMemoryBus();

        string id = bus.Publish(new TestOrderEvent { Name = "in-memory" }, "order-event");

        id.Should().NotBeNullOrWhiteSpace();
        bus.WasPublished<TestOrderEvent>().Should().BeTrue();
        bus.PublishedCount<TestOrderEvent>().Should().Be(1);
    }

    [Fact]
    public void PublishBatch_WithMockConnection_ShouldReturnAllMessageIds()
    {
        var batchMock = new Mock<IBasicPublishBatch>();
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);
        channelMock.Setup(c => c.IsOpen).Returns(true);
        channelMock.Setup(c => c.CreateBasicPublishBatch()).Returns(batchMock.Object);
        channelMock.Setup(c => c.ConfirmSelect());
        channelMock.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()));

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(connection: connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        IEnumerable<string> ids = client.PublishBatch([
            (new CustomerEvent { Id = 1, Name = "b1", Active = true }, "route-1"),
            (new CustomerEvent { Id = 2, Name = "b2", Active = true }, "route-2")
        ]);

        ids.Should().HaveCount(2);
        batchMock.Verify(b => b.Publish(), Times.Once);
    }

    [Fact]
    public async Task PublishBatchAsync_ShouldReturnAllMessageIds()
    {
        var batchMock = new Mock<IBasicPublishBatch>();
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);
        channelMock.Setup(c => c.IsOpen).Returns(true);
        channelMock.Setup(c => c.CreateBasicPublishBatch()).Returns(batchMock.Object);
        channelMock.Setup(c => c.ConfirmSelect());
        channelMock.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()));

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(connection: connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        IEnumerable<string> ids = await client.PublishBatchAsync([
            (new CustomerEvent { Id = 3, Name = "async-b1", Active = true }, "route-1"),
            (new CustomerEvent { Id = 4, Name = "async-b2", Active = true }, "route-2")
        ]);

        ids.Should().HaveCount(2);
    }

    [Fact]
    public void Publish_WhenDisconnected_ShouldTryConnectBeforePublishing()
    {
        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection(isConnected: false);
        bool connected = false;
        connectionMock.Setup(c => c.IsConnected).Returns(() => connected);
        connectionMock.Setup(c => c.TryConnect()).Callback(() => connected = true).Returns(true);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(connection: connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = client.Publish(new CustomerEvent { Id = 10, Name = "reconnect" }, routingKey: "customer-event");

        messageId.Should().NotBeNullOrWhiteSpace();
        connectionMock.Verify(c => c.TryConnect(), Times.AtLeastOnce);
    }

    [Fact]
    public void PublishBatch_WhenDisconnected_ShouldTryConnectBeforePublishing()
    {
        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection(isConnected: false);
        bool connected = false;
        connectionMock.Setup(c => c.IsConnected).Returns(() => connected);
        connectionMock.Setup(c => c.TryConnect()).Callback(() => connected = true).Returns(true);

        var batchMock = new Mock<IBasicPublishBatch>();
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);
        channelMock.Setup(c => c.IsOpen).Returns(true);
        channelMock.Setup(c => c.CreateBasicPublishBatch()).Returns(batchMock.Object);
        channelMock.Setup(c => c.ConfirmSelect());
        channelMock.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()));
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(connection: connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        IEnumerable<string> ids = client.PublishBatch([
            (new CustomerEvent { Id = 11, Name = "batch-reconnect", Active = true }, "route-1")
        ]);

        ids.Should().HaveCount(1);
        connectionMock.Verify(c => c.TryConnect(), Times.AtLeastOnce);
    }

    [Fact]
    public void Consume_WithSyncRecoveryConsumer_ShouldSetupChannelAndConsumers()
    {
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);
        channelMock.Setup(c => c.IsOpen).Returns(true);
        channelMock.Setup(c => c.ChannelNumber).Returns(1);

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions
        {
            RetryCount = 0,
            DispatchConsumersAsync = false
        });

        var services = new ServiceCollection();
        services.AddSingleton(RabbitMQTestHelpers.CreateClientOptions());
        services.AddSingleton(Options.Create(RabbitMQTestHelpers.CreateClientOptions()));
        services.AddSingleton(connectionMock.Object);
        services.AddSingleton<RecoverySyncConsumer>();
        services.AddSingleton<MvpRabbitMQClient>();
        IServiceProvider provider = services.BuildServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();
        client.Register<RecoverySyncConsumer>();

        Action act = () => client.Consume();

        act.Should().NotThrow();
    }

    [Fact]
    public async Task HandleConsumeAsync_RecoveryConsumerFailure_ShouldInvokeRecoveryHooks()
    {
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
        channelMock.Setup(c => c.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()));
        channelMock.Setup(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()));

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();
        var consumer = new RecoveryAsyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 99, Name = "recover" }, redeliveredCount: 5);

        await InvokeHandleConsumeAsync(client, args, channelMock.Object, consumer);

        consumer.FailureCalled.Should().BeTrue();
        consumer.RejectedCalled.Should().BeTrue();
    }

    [Fact]
    public async Task HandleConsumeAsync_SuccessfulConsumer_ShouldAckMessage()
    {
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();
        var consumer = new SuccessAsyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 1, Name = "success" });

        await InvokeHandleConsumeAsync(client, args, channelMock.Object, consumer);

        channelMock.Verify(c => c.BasicAck(1, false), Times.Once);
        consumer.ReceivedCalled.Should().BeTrue();
    }

    [Fact]
    public void HandleConsume_SyncConsumerSuccess_ShouldAckMessage()
    {
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();
        var consumer = new SuccessSyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 2, Name = "sync-success" });

        InvokeHandleConsume(client, args, channelMock.Object, consumer);

        channelMock.Verify(c => c.BasicAck(1, false), Times.Once);
        consumer.ReceivedCalled.Should().BeTrue();
    }

    [Fact]
    public void HandleConsume_SyncConsumerFailure_ShouldInvokeRecoveryHooks()
    {
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
        channelMock.Setup(c => c.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()));
        channelMock.Setup(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()));

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();
        var consumer = new RecoverySyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 3, Name = "sync-fail" }, redeliveredCount: 5);

        InvokeHandleConsume(client, args, channelMock.Object, consumer);

        consumer.FailureCalled.Should().BeTrue();
        consumer.RejectedCalled.Should().BeTrue();
    }

    private static BasicDeliverEventArgs CreateBusinessEventArgs(CustomerEvent message, int redeliveredCount = 0)
    {
        string payload = message.ToBusinessEvent("recovery-token").ToSerialize(JsonHelper.JsonBusinessEventSettings());
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Object.Headers = new Dictionary<string, object>
        {
            ["x-redelivered-count"] = redeliveredCount
        };
        propertiesMock.Object.MessageId = "recovery-token";
        propertiesMock.Object.CorrelationId = "recovery-token";

        return new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 1,
            redelivered: false,
            exchange: "test-exchange",
            routingKey: "customer-event",
            properties: propertiesMock.Object,
            body: System.Text.Encoding.UTF8.GetBytes(payload));
    }

    private static async Task InvokeHandleConsumeAsync(
        MvpRabbitMQClient client,
        BasicDeliverEventArgs args,
        IModel channel,
        IMvpRabbitMQConsumerAsync consumer)
    {
        System.Reflection.MethodInfo? method = typeof(MvpRabbitMQClient).GetMethod(
            "HandleConsumeAsync",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method.Should().NotBeNull();
        var task = (Task)method!.Invoke(client, [args, channel, consumer])!;
        await task;
    }

    private static void InvokeHandleConsume(
        MvpRabbitMQClient client,
        BasicDeliverEventArgs args,
        IModel channel,
        IMvpRabbitMQConsumerSync consumer)
    {
        System.Reflection.MethodInfo? method = typeof(MvpRabbitMQClient).GetMethod(
            "HandleConsume",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(client, [args, channel, consumer]);
    }
}

internal sealed class RecoverySyncConsumer : IMvpRabbitMQConsumerSync, IMvpRabbitMQConsumerRecoverySync
{
    public bool FailureCalled { get; private set; }
    public bool RejectedCalled { get; private set; }

    public string RoutingKey => "recovery-sync";
    public string QueueName => "recovery-sync-queue";

    public void Received(object message, string token)
    {
        throw new InvalidOperationException("forced failure");
    }

    public void Failure(Exception ex, string token)
    {
        FailureCalled = true;
    }

    public void Rejected(object message, string token)
    {
        RejectedCalled = true;
    }
}

internal sealed class SuccessSyncConsumer : IMvpRabbitMQConsumerSync
{
    public bool ReceivedCalled { get; private set; }

    public string RoutingKey => "success-sync";
    public string QueueName => "success-sync-queue";

    public void Received(object message, string token)
    {
        ReceivedCalled = true;
    }
}

internal sealed class SuccessAsyncConsumer : IMvpRabbitMQConsumerAsync
{
    public bool ReceivedCalled { get; private set; }

    public string RoutingKey => "success-async";
    public string QueueName => "success-async-queue";

    public Task ReceivedAsync(object message, string token)
    {
        ReceivedCalled = true;
        return Task.CompletedTask;
    }
}

internal sealed class RecoveryAsyncConsumer : IMvpRabbitMQConsumerAsync, IMvpRabbitMQConsumerRecoveryAsync
{
    public bool FailureCalled { get; private set; }
    public bool RejectedCalled { get; private set; }

    public string RoutingKey => "recovery-async";
    public string QueueName => "recovery-async-queue";

    public Task ReceivedAsync(object message, string token)
    {
        throw new InvalidOperationException("forced async failure");
    }

    public Task FailureAsync(Exception ex, string token)
    {
        FailureCalled = true;
        return Task.CompletedTask;
    }

    public Task RejectedAsync(object message, string token)
    {
        RejectedCalled = true;
        return Task.CompletedTask;
    }
}
