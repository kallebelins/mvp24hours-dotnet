using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Consumers;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mvp24Hours.Application.RabbitMQ.Test.Consumers;

[Trait("Category", "Unit")]
public class ConsumeContextExtendedTest
{
    [Fact]
    public void Constructor_WithNullMessage_ShouldThrow()
    {
        Action act = () => new ConsumeContext<TestOrderEvent>(
            null!,
            RabbitMQTestHelpers.CreateDeliverEventArgs(),
            new ServiceCollection().BuildServiceProvider());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullServiceProvider_ShouldThrow()
    {
        Action act = () => new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent(),
            RabbitMQTestHelpers.CreateDeliverEventArgs(),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ConsumeContext_ShouldExposeMetadataFromDeliveryArgs()
    {
        var headers = new Dictionary<string, object>
        {
            ["x-causation-id"] = "cause-1",
            ["x-redelivered-count"] = 2
        };

        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Object.MessageId = "msg-42";
        propertiesMock.Object.CorrelationId = "corr-42";
        propertiesMock.Object.Headers = headers;
        propertiesMock.Object.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        var eventArgs = new BasicDeliverEventArgs(
            consumerTag: "consumer-1",
            deliveryTag: 99,
            redelivered: true,
            exchange: "orders-exchange",
            routingKey: "orders.created",
            properties: propertiesMock.Object,
            body: ReadOnlyMemory<byte>.Empty);

        var context = new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent { Name = "meta" },
            eventArgs,
            new ServiceCollection().BuildServiceProvider(),
            queueName: "orders");

        context.MessageId.Should().Be("msg-42");
        context.CorrelationId.Should().Be("corr-42");
        context.CausationId.Should().Be("cause-1");
        context.RedeliveryCount.Should().Be(2);
        context.Exchange.Should().Be("orders-exchange");
        context.RoutingKey.Should().Be("orders.created");
        context.QueueName.Should().Be("orders");
        context.DeliveryTag.Should().Be(99ul);
        context.Redelivered.Should().BeTrue();
        context.SentAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_WithClient_ShouldPublishWithCorrelationHeaders()
    {
        var clientMock = new Mock<IMvpRabbitMQClient>();
        var services = new ServiceCollection();
        ServiceProvider provider = services.BuildServiceProvider();

        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Object.CorrelationId = "corr-publish";

        var eventArgs = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 1,
            redelivered: false,
            exchange: "ex",
            routingKey: "rk",
            properties: propertiesMock.Object,
            body: ReadOnlyMemory<byte>.Empty);

        var context = new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent(),
            eventArgs,
            provider,
            clientMock.Object);

        await context.PublishAsync(new TestOrderEvent { Name = "child" }, "child-route");

        clientMock.Verify(c => c.Publish(
            It.IsAny<TestOrderEvent>(),
            "child-route",
            It.Is<IDictionary<string, object>>(h =>
                h.ContainsKey("x-correlation-id") && h.ContainsKey("x-causation-id"))),
            Times.Once);
    }

    [Fact]
    public async Task RespondAsync_WithReplyToAndClient_ShouldPublishToReplyQueue()
    {
        var clientMock = new Mock<IMvpRabbitMQClient>();
        var propertiesMock = new Mock<IBasicProperties>();
        propertiesMock.SetupAllProperties();
        propertiesMock.Object.ReplyTo = "reply-queue";
        propertiesMock.Object.CorrelationId = "corr-reply";

        var eventArgs = new BasicDeliverEventArgs(
            consumerTag: "tag",
            deliveryTag: 1,
            redelivered: false,
            exchange: "ex",
            routingKey: "rk",
            properties: propertiesMock.Object,
            body: ReadOnlyMemory<byte>.Empty);

        var context = new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent(),
            eventArgs,
            new ServiceCollection().BuildServiceProvider(),
            clientMock.Object);

        await context.RespondAsync(new TestOrderResponse { Success = true });

        clientMock.Verify(c => c.Publish(
            It.IsAny<TestOrderResponse>(),
            "reply-queue",
            It.IsAny<IDictionary<string, object>>()),
            Times.Once);
    }

    [Fact]
    public void CreateScope_ShouldReturnDisposableScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<ScopedDependency>();
        ServiceProvider provider = services.BuildServiceProvider();

        var context = new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent(),
            RabbitMQTestHelpers.CreateDeliverEventArgs(),
            provider);

        using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract.IServiceScope scope = context.CreateScope();
        scope.ServiceProvider.GetRequiredService<ScopedDependency>().Should().NotBeNull();
    }

    [Fact]
    public void GetHeader_WithTypedValue_ShouldReturnConvertedValue()
    {
        var headers = new Dictionary<string, object> { ["count"] = 7 };
        var context = new ConsumeContext<TestOrderEvent>(
            new TestOrderEvent(),
            RabbitMQTestHelpers.CreateDeliverEventArgs(headers: headers),
            new ServiceCollection().BuildServiceProvider());

        context.GetHeader<int>("count").Should().Be(7);
        context.GetHeader<string>("missing").Should().BeNull();
    }

    private sealed class ScopedDependency;
}
