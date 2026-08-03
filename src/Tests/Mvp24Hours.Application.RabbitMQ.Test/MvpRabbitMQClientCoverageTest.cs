using System.Net.Sockets;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
using Mvp24Hours.Infrastructure.RabbitMQ.Deduplication;
using Mvp24Hours.Infrastructure.RabbitMQ.Logging;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace Mvp24Hours.Application.RabbitMQ.Test;

[Trait("Category", "Unit")]
public class MvpRabbitMQClientCoverageTest
{
    [Fact]
    public void Publish_WithRetryOnBrokerUnreachable_ShouldEventuallySucceed()
    {
        int attempts = 0;
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.BasicPublish(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()))
            .Callback(() =>
            {
                if (Interlocked.Increment(ref attempts) == 1)
                {
                    throw new BrokerUnreachableException(new Exception("broker down"));
                }
            });

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions { RetryCount = 2 });

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(connection: connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        string messageId = client.Publish(new CustomerEvent { Id = 1, Name = "retry" }, routingKey: "route");

        messageId.Should().NotBeNullOrWhiteSpace();
        attempts.Should().Be(2);
    }

    [Fact]
    public void Publish_WithPublisherConfirmWaitForConfirms_ShouldConfirm()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.WaitForConfirms(It.IsAny<TimeSpan>())).Returns(true);

        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.PublisherConfirm = new PublisherConfirmOptions
        {
            Enabled = true,
            WaitForConfirmsOrDie = false,
            TimeoutMilliseconds = 1000
        };

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(options, connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        client.Publish(new CustomerEvent { Id = 2, Name = "confirm" }, routingKey: "route");

        channelMock.Verify(c => c.ConfirmSelect(), Times.Once);
        channelMock.Verify(c => c.WaitForConfirms(It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public void Publish_WithEnabledOptions_ShouldApplyPriorityTtlAndHeaders()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        IBasicProperties? captured = null;
        channelMock.Setup(c => c.BasicPublish(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()))
            .Callback<string, string, bool, IBasicProperties, ReadOnlyMemory<byte>>((_, _, _, props, _) => captured = props);

        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.PriorityQueue = new PriorityQueueOptions { Enabled = true, DefaultPriority = 7 };
        options.MessageTtl = new MessageTtlOptions { Enabled = true, DefaultTtlMilliseconds = 5000 };
        options.HeadersExchange = new HeadersExchangeOptions
        {
            DefaultMessageHeaders = new Dictionary<string, object> { ["x-default"] = "default" }
        };

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(options, connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        client.Publish(
            new CustomerEvent { Id = 3, Name = "opts" },
            routingKey: "route",
            headers: new Dictionary<string, object> { ["x-custom"] = "value" });

        captured.Should().NotBeNull();
        captured!.Priority.Should().Be(7);
        captured.Expiration.Should().Be("5000");
        captured.Headers.Should().ContainKey("x-default");
        captured.Headers.Should().ContainKey("x-custom");
    }

    [Fact]
    public void Publish_WithMetricsAndStructuredLogging_ShouldNotThrow()
    {
        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.EnableMetrics = true;
        options.EnableStructuredLogging = true;

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        services.AddSingleton<IRabbitMQMetrics, RabbitMQMetrics>();
        services.AddSingleton<IRabbitMQStructuredLogger>(sp =>
            new RabbitMQStructuredLogger(sp.GetRequiredService<ILogger<RabbitMQStructuredLogger>>()));
        services.AddLogging();
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();

        Action act = () => client.Publish(new CustomerEvent { Id = 4, Name = "metrics" }, routingKey: "route");

        act.Should().NotThrow();
    }

    [Fact]
    public void PublishBatch_WhenPublishFails_ShouldPropagateException()
    {
        var batchMock = new Mock<IBasicPublishBatch>();
        batchMock.Setup(b => b.Publish()).Throws(new InvalidOperationException("batch failed"));

        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.CreateBasicPublishBatch()).Returns(batchMock.Object);
        channelMock.Setup(c => c.ConfirmSelect());
        channelMock.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()));

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(connection: connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        Action act = () => client.PublishBatch([(new CustomerEvent { Id = 5, Name = "fail" }, "route")]);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Consume_WithAsyncConsumer_ShouldSetupAsyncConsumer()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.ChannelNumber).Returns(2);

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions
        {
            RetryCount = 0,
            DispatchConsumersAsync = true
        });

        var services = new ServiceCollection();
        services.AddSingleton(RabbitMQTestHelpers.CreateClientOptions());
        services.AddSingleton(Options.Create(RabbitMQTestHelpers.CreateClientOptions()));
        services.AddSingleton(connectionMock.Object);
        services.AddSingleton<CustomerConsumer>();
        services.AddSingleton<MvpRabbitMQClient>();
        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        client.Register<CustomerConsumer>();

        Action act = () => client.Consume();

        act.Should().NotThrow();
    }

    [Fact]
    public void Consume_WithDeadLetterConfigured_ShouldDeclareDeadLetterTopology()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.ChannelNumber).Returns(3);

        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.DeadLetter = new RabbitMQClientOptions
        {
            Exchange = "dlx-exchange",
            RoutingKey = "dlx-route",
            QueueName = "dlx-queue"
        };

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions { RetryCount = 0, DispatchConsumersAsync = true });

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(connectionMock.Object);
        services.AddSingleton<CustomerConsumer>();
        services.AddSingleton<MvpRabbitMQClient>();
        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        client.Register<CustomerConsumer>();

        client.Consume();

        channelMock.Verify(c => c.ExchangeDeclare("dlx-exchange", It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Consume_AsyncEnabledWithSyncConsumer_ShouldThrow()
    {
        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions
        {
            DispatchConsumersAsync = true
        });

        var services = new ServiceCollection();
        services.AddSingleton(RabbitMQTestHelpers.CreateClientOptions());
        services.AddSingleton(Options.Create(RabbitMQTestHelpers.CreateClientOptions()));
        services.AddSingleton(connectionMock.Object);
        services.AddSingleton<MvpRabbitMQClient>();
        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        client.Register<RecoverySyncConsumer>();

        Action act = () => client.Consume();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*DispatchConsumersAsync is enabled*");
    }

    [Fact]
    public void Consume_SyncEnabledWithAsyncConsumer_ShouldThrow()
    {
        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions
        {
            DispatchConsumersAsync = false
        });

        var services = new ServiceCollection();
        services.AddSingleton(RabbitMQTestHelpers.CreateClientOptions());
        services.AddSingleton(Options.Create(RabbitMQTestHelpers.CreateClientOptions()));
        services.AddSingleton(connectionMock.Object);
        services.AddSingleton<MvpRabbitMQClient>();
        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        client.Register<CustomerConsumer>();

        Action act = () => client.Consume();

        act.Should().Throw<ArgumentException>()
            .WithMessage("*DispatchConsumersAsync is disabled*");
    }

    [Fact]
    public async Task HandleConsume_DuplicateMessage_ShouldAckWithoutProcessing()
    {
        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.Deduplication = new MessageDeduplicationOptions { Enabled = true, ExpirationMinutes = 60 };

        var store = new InMemoryMessageDeduplicationStore();
        await store.MarkAsProcessedAsync("dup-token");

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        services.AddSingleton<IMessageDeduplicationStore>(store);
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
        var consumer = new SuccessSyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 6, Name = "dup" }, messageId: "dup-token");

        InvokeHandleConsume(client, args, channelMock.Object, consumer);

        consumer.ReceivedCalled.Should().BeFalse();
        channelMock.Verify(c => c.BasicAck(1, false), Times.Once);
    }

    [Fact]
    public async Task HandleConsumeAsync_DuplicateMessage_ShouldAckWithoutProcessing()
    {
        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.Deduplication = new MessageDeduplicationOptions { Enabled = true, ExpirationMinutes = 60 };

        var store = new InMemoryMessageDeduplicationStore();
        await store.MarkAsProcessedAsync("dup-async");

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        services.AddSingleton<IMessageDeduplicationStore>(store);
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
        var consumer = new SuccessAsyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 7, Name = "dup-async" }, messageId: "dup-async");

        await InvokeHandleConsumeAsync(client, args, channelMock.Object, consumer);

        consumer.ReceivedCalled.Should().BeFalse();
        channelMock.Verify(c => c.BasicAck(1, false), Times.Once);
    }

    [Fact]
    public async Task HandleConsume_SuccessWithDedup_ShouldMarkProcessed()
    {
        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.Deduplication = new MessageDeduplicationOptions { Enabled = true, ExpirationMinutes = 60 };

        var store = new InMemoryMessageDeduplicationStore();
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        services.AddSingleton<IMessageDeduplicationStore>(store);
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
        var consumer = new SuccessSyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 8, Name = "mark" }, messageId: "mark-token");

        InvokeHandleConsume(client, args, channelMock.Object, consumer);

        (await store.IsProcessedAsync("mark-token")).Should().BeTrue();
    }

    [Fact]
    public void HandleConsume_FailureBelowMaxRedelivery_ShouldRepublishAndAck()
    {
        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.MaxRedeliveredCount = 5;

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
        channelMock.Setup(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()));

        var consumer = new ThrowOnceSyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 9, Name = "redeliver" }, redeliveredCount: 0);

        InvokeHandleConsume(client, args, channelMock.Object, consumer);

        channelMock.Verify(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
        channelMock.Verify(c => c.BasicAck(1, false), Times.Once);
    }

    [Fact]
    public async Task HandleConsumeAsync_FailureBelowMaxRedelivery_ShouldRepublishAndAck()
    {
        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.MaxRedeliveredCount = 5;

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
        channelMock.Setup(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()));

        var consumer = new ThrowOnceAsyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(new CustomerEvent { Id = 10, Name = "async-redeliver" }, redeliveredCount: 1);

        await InvokeHandleConsumeAsync(client, args, channelMock.Object, consumer);

        channelMock.Verify(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
    }

    [Fact]
    public void GetService_SyncConsumerFromDi_ShouldResolveRegisteredInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton(RabbitMQTestHelpers.CreateClientOptions());
        services.AddSingleton(Options.Create(RabbitMQTestHelpers.CreateClientOptions()));
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        var expected = new SuccessSyncConsumer();
        services.AddSingleton(expected);
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        IMvpRabbitMQConsumer resolved = InvokeGetService(client, typeof(SuccessSyncConsumer));

        resolved.Should().BeSameAs(expected);
    }

    [Fact]
    public void GetService_AsyncConsumerViaActivatorUtilities_ShouldCreateInstance()
    {
        var services = new ServiceCollection();
        services.AddSingleton(RabbitMQTestHelpers.CreateClientOptions());
        services.AddSingleton(Options.Create(RabbitMQTestHelpers.CreateClientOptions()));
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        services.AddSingleton(new CustomerEvent { Id = 1, Name = "event", Active = true });
        services.AddLogging();
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        IMvpRabbitMQConsumer resolved = InvokeGetService(client, typeof(CustomerWithCtorConsumer));

        resolved.Should().BeOfType<CustomerWithCtorConsumer>();
    }

    [Fact]
    public void GetService_InvalidConsumerType_ShouldThrow()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        Action act = () => InvokeGetService(client, typeof(string));

        act.Should().Throw<TargetInvocationException>()
            .WithInnerException<ArgumentException>()
            .WithMessage("*Invalid type for consumers*");
    }

    [Fact]
    public void Publish_WithWaitForConfirmsOrDie_ShouldConfirmOrDie()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()));

        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.PublisherConfirm = new PublisherConfirmOptions
        {
            Enabled = true,
            WaitForConfirmsOrDie = true,
            TimeoutMilliseconds = 1000
        };

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(options, connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        client.Publish(new CustomerEvent { Id = 12, Name = "confirm-die" }, routingKey: "route");

        channelMock.Verify(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public void Publish_WhenPublishFails_ShouldIncrementErrorMetric()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.BasicPublish(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<IBasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>()))
            .Throws(new InvalidOperationException("publish failed"));

        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.EnableMetrics = true;
        options.EnableStructuredLogging = true;

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        services.AddSingleton(connectionMock.Object);
        services.AddSingleton<IRabbitMQMetrics, RabbitMQMetrics>();
        services.AddSingleton<IRabbitMQStructuredLogger>(sp =>
            new RabbitMQStructuredLogger(sp.GetRequiredService<ILogger<RabbitMQStructuredLogger>>()));
        services.AddLogging();
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();

        Action act = () => client.Publish(new CustomerEvent { Id = 13, Name = "fail" }, routingKey: "route");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Consume_WithMetricsAndStructuredLogging_ShouldNotThrow()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.ChannelNumber).Returns(5);

        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.EnableMetrics = true;
        options.EnableStructuredLogging = true;

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions
        {
            RetryCount = 0,
            DispatchConsumersAsync = true
        });

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(connectionMock.Object);
        services.AddSingleton<IRabbitMQMetrics, RabbitMQMetrics>();
        services.AddSingleton<IRabbitMQStructuredLogger>(sp =>
            new RabbitMQStructuredLogger(sp.GetRequiredService<ILogger<RabbitMQStructuredLogger>>()));
        services.AddLogging();
        services.AddSingleton<CustomerConsumer>();
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        client.Register<CustomerConsumer>();

        Action act = () => client.Consume();

        act.Should().NotThrow();
    }

    [Fact]
    public void HandleConsume_WithRedeliveredCountAboveOne_ShouldTrackRedeliveryMetrics()
    {
        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.EnableMetrics = true;
        options.EnableStructuredLogging = true;
        options.MaxRedeliveredCount = 10;

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        services.AddSingleton(RabbitMQTestHelpers.CreateMockConnection().Object);
        services.AddSingleton<IRabbitMQMetrics, RabbitMQMetrics>();
        services.AddSingleton<IRabbitMQStructuredLogger>(sp =>
            new RabbitMQStructuredLogger(sp.GetRequiredService<ILogger<RabbitMQStructuredLogger>>()));
        services.AddLogging();
        services.AddSingleton<MvpRabbitMQClient>();

        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        var channelMock = new Mock<IModel>();
        channelMock.Setup(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()));
        var consumer = new SuccessSyncConsumer();
        BasicDeliverEventArgs args = CreateBusinessEventArgs(
            new CustomerEvent { Id = 14, Name = "redelivered" },
            redeliveredCount: 2);

        InvokeHandleConsume(client, args, channelMock.Object, consumer);

        consumer.ReceivedCalled.Should().BeTrue();
    }

    [Fact]
    public void Consume_ReusingSameQueue_ShouldReuseOpenChannel()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.ChannelNumber).Returns(6);

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
        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        client.Register<RecoverySyncConsumer>();

        client.Consume();
        client.Consume();

        connectionMock.Verify(c => c.CreateModel(), Times.Once);
    }

    [Fact]
    public void Publish_WithConfiguredBasicProperties_ShouldUseConfiguredProperties()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        IBasicProperties? configuredProperties = null;
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(() =>
        {
            var propertiesMock = new Mock<IBasicProperties>();
            propertiesMock.SetupAllProperties();
            configuredProperties = propertiesMock.Object;
            return configuredProperties;
        });

        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.BasicProperties = channelMock.Object.CreateBasicProperties();

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);

        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider(options, connectionMock.Object);
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        client.Publish(new CustomerEvent { Id = 15, Name = "basic-props" }, routingKey: "route");

        channelMock.Verify(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            options.BasicProperties,
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
    }

    [Fact]
    public void GetService_RecoverySyncConsumer_ShouldResolveViaRecoveryInterface()
    {
        IServiceProvider provider = RabbitMQTestHelpers.CreateClientServiceProvider();
        MvpRabbitMQClient client = provider.GetRequiredService<MvpRabbitMQClient>();

        IMvpRabbitMQConsumer resolved = InvokeGetService(client, typeof(RecoverySyncConsumer));

        resolved.Should().BeOfType<RecoverySyncConsumer>();
    }

    [Fact]
    public void Consume_WithHeadersExchangeBinding_ShouldBindWithMatchHeader()
    {
        Mock<IModel> channelMock = CreateChannelMock();
        channelMock.Setup(c => c.ChannelNumber).Returns(4);

        RabbitMQClientOptions options = RabbitMQTestHelpers.CreateClientOptions();
        options.HeadersExchange = new HeadersExchangeOptions
        {
            Enabled = true,
            MatchType = "all",
            BindingHeaders = new Dictionary<string, object> { ["region"] = "us" }
        };
        options.PriorityQueue = new PriorityQueueOptions { Enabled = true, MaxPriority = 10 };
        options.MessageTtl = new MessageTtlOptions
        {
            Enabled = true,
            QueueTtlMilliseconds = 1000,
            QueueExpiresMilliseconds = 2000
        };
        options.DeadLetter = new RabbitMQClientOptions
        {
            Exchange = "main-dlx",
            RoutingKey = "main-dlq-route"
        };

        Mock<IMvpRabbitMQConnection> connectionMock = RabbitMQTestHelpers.CreateMockConnection();
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions { RetryCount = 0, DispatchConsumersAsync = true });

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(connectionMock.Object);
        services.AddSingleton<CustomerConsumer>();
        services.AddSingleton<MvpRabbitMQClient>();
        MvpRabbitMQClient client = services.BuildServiceProvider().GetRequiredService<MvpRabbitMQClient>();
        client.Register<CustomerConsumer>();

        client.Consume();

        channelMock.Verify(c => c.QueueBind(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.Is<IDictionary<string, object>?>(args => args != null && args.ContainsKey("x-match"))), Times.AtLeastOnce);
    }

    private static Mock<IModel> CreateChannelMock()
    {
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);
        channelMock.Setup(c => c.IsOpen).Returns(true);
        return channelMock;
    }

    private static BasicDeliverEventArgs CreateBusinessEventArgs(
        CustomerEvent message,
        int redeliveredCount = 0,
        string messageId = "recovery-token")
    {
        string payload = message.ToBusinessEvent(messageId).ToSerialize(JsonHelper.JsonBusinessEventSettings());
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Object.Headers = new Dictionary<string, object>
        {
            ["x-redelivered-count"] = redeliveredCount
        };
        propertiesMock.Object.MessageId = messageId;
        propertiesMock.Object.CorrelationId = messageId;

        return new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 1,
            redelivered: false,
            exchange: "test-exchange",
            routingKey: "customer-event",
            properties: propertiesMock.Object,
            body: System.Text.Encoding.UTF8.GetBytes(payload));
    }

    private static IMvpRabbitMQConsumer InvokeGetService(MvpRabbitMQClient client, Type consumerType)
    {
        MethodInfo? method = typeof(MvpRabbitMQClient).GetMethod(
            "GetService",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        return (IMvpRabbitMQConsumer)method!.Invoke(client, [consumerType])!;
    }

    private static async Task InvokeHandleConsumeAsync(
        MvpRabbitMQClient client,
        BasicDeliverEventArgs args,
        IModel channel,
        IMvpRabbitMQConsumerAsync consumer)
    {
        MethodInfo? method = typeof(MvpRabbitMQClient).GetMethod(
            "HandleConsumeAsync",
            BindingFlags.Instance | BindingFlags.NonPublic);
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
        MethodInfo? method = typeof(MvpRabbitMQClient).GetMethod(
            "HandleConsume",
            BindingFlags.Instance | BindingFlags.NonPublic);
        method.Should().NotBeNull();
        method!.Invoke(client, [args, channel, consumer]);
    }
}

internal sealed class ThrowOnceSyncConsumer : IMvpRabbitMQConsumerSync
{
    public string RoutingKey => "throw-once-sync";
    public string QueueName => "throw-once-sync-queue";

    public void Received(object message, string token)
    {
        throw new InvalidOperationException("transient");
    }
}

internal sealed class ThrowOnceAsyncConsumer : IMvpRabbitMQConsumerAsync
{
    public string RoutingKey => "throw-once-async";
    public string QueueName => "throw-once-async-queue";

    public Task ReceivedAsync(object message, string token)
    {
        throw new InvalidOperationException("transient async");
    }
}
