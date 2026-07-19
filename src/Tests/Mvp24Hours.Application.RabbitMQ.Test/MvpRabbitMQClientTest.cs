using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Consumers;
using Mvp24Hours.Application.RabbitMQ.Test.Support.Dto;
using Mvp24Hours.Infrastructure.RabbitMQ;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test;

public class MvpRabbitMQClientTest
{
    [Fact]
    public void Publish_WithoutRoutingKeyOrDefault_ShouldThrow()
    {
        var options = RabbitMQTestHelpers.CreateClientOptions(defaultRoutingKey: string.Empty);
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
}
