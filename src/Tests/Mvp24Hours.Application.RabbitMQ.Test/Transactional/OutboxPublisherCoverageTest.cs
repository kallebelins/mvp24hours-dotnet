using Microsoft.Extensions.Hosting;
using Moq;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;

namespace Mvp24Hours.Application.RabbitMQ.Test.Transactional;

[Trait("Category", "Unit")]
public class OutboxPublisherCoverageTest
{
    [Fact]
    public void Constructor_WithNullOutbox_ShouldThrow()
    {
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();

        Action act = () => new OutboxPublisher(
            null!,
            client,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("outbox");
    }

    [Fact]
    public void Constructor_WithNullClient_ShouldThrow()
    {
        var outbox = new InMemoryTransactionalOutbox();

        Action act = () => new OutboxPublisher(
            outbox,
            null!,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>());

        act.Should().Throw<ArgumentNullException>().WithParameterName("rabbitMQClient");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrow()
    {
        var outbox = new InMemoryTransactionalOutbox();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();

        Action act = () => new OutboxPublisher(outbox, client, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void OutboxPublisherOptions_Defaults_ShouldHaveExpectedValues()
    {
        var options = new OutboxPublisherOptions();

        options.PollingInterval.Should().Be(TimeSpan.FromSeconds(1));
        options.BatchSize.Should().Be(100);
        options.EnableCleanup.Should().BeTrue();
        options.CleanupInterval.Should().Be(TimeSpan.FromHours(1));
        options.RetentionPeriod.Should().Be(TimeSpan.FromDays(7));
    }

    [Fact]
    public async Task PublishPendingAsync_WithHeadersAndCorrelation_ShouldPublishViaClient()
    {
        var outbox = new InMemoryTransactionalOutbox();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var publisher = new OutboxPublisher(
            outbox,
            client,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>());

        await outbox.AddAsync(new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            Payload = "{\"name\":\"headers\"}",
            RoutingKey = "order-event",
            CorrelationId = "corr-1",
            CausationId = "cause-1",
            Headers = "{\"x-region\":\"us\"}",
            CreatedAt = DateTime.UtcNow,
            Status = TransactionalOutboxStatus.Pending
        });

        int published = await publisher.PublishPendingAsync();

        published.Should().Be(1);
        client.WasPublished<TestOrderEvent>().Should().BeTrue();
    }

    [Fact]
    public async Task PublishPendingAsync_WhenClientThrows_ShouldMarkFailedAndIncrementTotalFailed()
    {
        var outbox = new InMemoryTransactionalOutbox();
        var clientMock = new Mock<IMvpRabbitMQClient>();
        clientMock.Setup(c => c.Publish(
                It.IsAny<object>(),
                It.IsAny<string>(),
                It.IsAny<IDictionary<string, object>>()))
            .Throws(new InvalidOperationException("publish failed"));

        var publisher = new OutboxPublisher(
            outbox,
            clientMock.Object,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>());

        TransactionalOutboxMessage message = CreateMessage();
        await outbox.AddAsync(message);

        int published = await publisher.PublishPendingAsync();

        published.Should().Be(0);
        outbox.GetById(message.Id)!.Status.Should().Be(TransactionalOutboxStatus.Failed);
        OutboxPublisherStatus status = publisher.GetStatus();
        status.TotalFailed.Should().Be(1);
        status.LastError.Should().Contain("publish failed");
        status.LastErrorAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishPendingAsync_WithUnknownMessageType_ShouldPublishJsonElementPayload()
    {
        var outbox = new InMemoryTransactionalOutbox();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var publisher = new OutboxPublisher(
            outbox,
            client,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>());

        await outbox.AddAsync(new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "Unknown.Type, Unknown.Assembly",
            Payload = "{\"name\":\"fallback\"}",
            RoutingKey = "fallback-route",
            CreatedAt = DateTime.UtcNow,
            Status = TransactionalOutboxStatus.Pending
        });

        int published = await publisher.PublishPendingAsync();

        published.Should().Be(1);
    }

    [Fact]
    public async Task PublishPendingAsync_WithNullPayload_ShouldMarkFailed()
    {
        var outbox = new InMemoryTransactionalOutbox();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var publisher = new OutboxPublisher(
            outbox,
            client,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>());

        TransactionalOutboxMessage message = CreateMessage(payload: "null");
        await outbox.AddAsync(message);

        int published = await publisher.PublishPendingAsync();

        published.Should().Be(0);
        outbox.GetById(message.Id)!.Status.Should().Be(TransactionalOutboxStatus.Failed);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPollAndPublishPendingMessages()
    {
        var outbox = new InMemoryTransactionalOutbox();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var publisher = new OutboxPublisher(
            outbox,
            client,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>(),
            new OutboxPublisherOptions { PollingInterval = TimeSpan.FromMilliseconds(50), BatchSize = 10 });

        await outbox.AddAsync(CreateMessage());

        await publisher.StartAsync(CancellationToken.None);

        await RabbitMQTestHelpers.WaitUntilAsync(
            () => publisher.GetStatus().TotalPublished >= 1,
            TimeSpan.FromSeconds(5));

        OutboxPublisherStatus status = publisher.GetStatus();
        status.IsRunning.Should().BeTrue();
        status.TotalPublished.Should().BeGreaterThanOrEqualTo(1);
        status.PendingCount.Should().Be(0);

        await publisher.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task GetStatus_ShouldExposePendingCountAndLastPublishedAt()
    {
        var outbox = new InMemoryTransactionalOutbox();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var publisher = new OutboxPublisher(
            outbox,
            client,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>());

        await outbox.AddAsync(CreateMessage());
        await publisher.PublishPendingAsync();

        OutboxPublisherStatus status = publisher.GetStatus();

        status.TotalPublished.Should().Be(1);
        status.LastPublishedAt.Should().NotBeNull();
        status.PendingCount.Should().Be(0);
    }

    private static TransactionalOutboxMessage CreateMessage(string payload = "{\"name\":\"outbox\"}")
    {
        return new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            Payload = payload,
            RoutingKey = "order-event",
            CreatedAt = DateTime.UtcNow,
            Status = TransactionalOutboxStatus.Pending
        };
    }
}
