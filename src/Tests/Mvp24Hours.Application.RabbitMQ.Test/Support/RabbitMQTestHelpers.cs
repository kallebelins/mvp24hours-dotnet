using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Application.RabbitMQ.Test.Support;

internal static class RabbitMQTestHelpers
{
    public static RabbitMQClientOptions CreateClientOptions(string? defaultRoutingKey = "default-route")
    {
        return new RabbitMQClientOptions
        {
            Exchange = "test-exchange",
            RoutingKey = defaultRoutingKey ?? string.Empty,
            PublisherConfirm = new PublisherConfirmOptions { Enabled = false },
            Deduplication = new MessageDeduplicationOptions { Enabled = false },
            EnableMetrics = false,
            EnableStructuredLogging = false
        };
    }

    public static IServiceProvider CreateClientServiceProvider(
        RabbitMQClientOptions? options = null,
        IMvpRabbitMQConnection? connection = null)
    {
        options ??= CreateClientOptions();
        connection ??= CreateMockConnection().Object;

        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(options));
        services.AddSingleton(options);
        services.AddSingleton(connection);
        services.AddSingleton<MvpRabbitMQClient>();
        return services.BuildServiceProvider();
    }

    public static Mock<IMvpRabbitMQConnection> CreateMockConnection(bool isConnected = true)
    {
        var channelMock = new Mock<IModel>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();

        channelMock.Setup(c => c.CreateBasicProperties()).Returns(propertiesMock.Object);
        channelMock.Setup(c => c.IsOpen).Returns(true);

        var connectionMock = new Mock<IMvpRabbitMQConnection>();
        connectionMock.Setup(c => c.IsConnected).Returns(isConnected);
        connectionMock.Setup(c => c.CreateModel()).Returns(channelMock.Object);
        connectionMock.Setup(c => c.Options).Returns(new RabbitMQConnectionOptions { RetryCount = 0 });
        connectionMock.Setup(c => c.TryConnect()).Returns(isConnected);

        return connectionMock;
    }

    public static BasicDeliverEventArgs CreateDeliverEventArgs(
        string exchange = "test-exchange",
        string routingKey = "test-route",
        ulong deliveryTag = 1,
        bool redelivered = false,
        IDictionary<string, object>? headers = null,
        string? replyTo = null,
        string? correlationId = null)
    {
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Setup(p => p.Headers).Returns(headers ?? new Dictionary<string, object>());
        propertiesMock.Setup(p => p.CorrelationId).Returns(correlationId ?? string.Empty);
        propertiesMock.Setup(p => p.ReplyTo).Returns(replyTo ?? string.Empty);

        return new BasicDeliverEventArgs(
            consumerTag: "consumer-tag",
            deliveryTag: deliveryTag,
            redelivered: redelivered,
            exchange: exchange,
            routingKey: routingKey,
            properties: propertiesMock.Object,
            body: ReadOnlyMemory<byte>.Empty);
    }

    public static TestConsumeContext<TMessage> CreateTestConsumeContext<TMessage>(
        TMessage message,
        Action<TestConsumeContextBuilder<TMessage>>? configure = null) where TMessage : class
    {
        var builder = new TestConsumeContextBuilder<TMessage>();
        configure?.Invoke(builder);
        return builder.Build(message);
    }

    public static ConsumeFilterContext<TMessage> CreateConsumeFilterContext<TMessage>(
        TMessage message,
        Action<TestConsumeContextBuilder<TMessage>>? configure = null) where TMessage : class
    {
        TestConsumeContext<TMessage> context = CreateTestConsumeContext(message, configure);
        return new ConsumeFilterContext<TMessage>(context);
    }

    public static PublishFilterContext<TMessage> CreatePublishFilterContext<TMessage>(
        TMessage message,
        string exchange = "test-exchange",
        string routingKey = "test-route",
        IServiceProvider? serviceProvider = null) where TMessage : class
    {
        serviceProvider ??= new ServiceCollection().BuildServiceProvider();
        return new PublishFilterContext<TMessage>(message, exchange, routingKey, serviceProvider);
    }

    public static InMemoryBus CreateInMemoryBus(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return new InMemoryBus(services.BuildServiceProvider());
    }

    public static ILogger<T> CreateNullLogger<T>()
    {
        return NullLogger<T>.Instance;
    }
}
