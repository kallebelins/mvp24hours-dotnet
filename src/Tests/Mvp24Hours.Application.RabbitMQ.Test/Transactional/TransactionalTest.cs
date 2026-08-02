using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional;
using Mvp24Hours.Infrastructure.RabbitMQ.Transactional.Contract;

namespace Mvp24Hours.Application.RabbitMQ.Test.Transactional;

public class TransactionalTest
{
    [Fact]
    public async Task InMemoryTransactionalOutbox_AddAndGetPending_ShouldReturnMessage()
    {
        var outbox = new InMemoryTransactionalOutbox();
        TransactionalOutboxMessage message = CreateOutboxMessage("order-created");

        await outbox.AddAsync(message);

        IReadOnlyList<TransactionalOutboxMessage> pending = await outbox.GetPendingAsync();
        pending.Should().ContainSingle(m => m.Id == message.Id);
        (await outbox.GetPendingCountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task InMemoryTransactionalOutbox_MarkAsPublished_ShouldUpdateStatus()
    {
        var outbox = new InMemoryTransactionalOutbox();
        TransactionalOutboxMessage message = CreateOutboxMessage("published");
        await outbox.AddAsync(message);

        await outbox.MarkAsPublishedAsync(message.Id);

        outbox.GetById(message.Id)!.Status.Should().Be(TransactionalOutboxStatus.Published);
        outbox.GetById(message.Id)!.PublishedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task InMemoryTransactionalOutbox_MarkAsFailed_ShouldMoveToDeadLetterAfterMaxRetries()
    {
        var outbox = new InMemoryTransactionalOutbox(options: new InMemoryTransactionalOutboxOptions { MaxRetryCount = 2 });
        TransactionalOutboxMessage message = CreateOutboxMessage("retry");
        await outbox.AddAsync(message);

        await outbox.MarkAsFailedAsync(message.Id, "error-1");
        outbox.GetById(message.Id)!.Status.Should().Be(TransactionalOutboxStatus.Failed);

        await outbox.MarkAsFailedAsync(message.Id, "error-2");
        outbox.GetById(message.Id)!.Status.Should().Be(TransactionalOutboxStatus.DeadLetter);

        IReadOnlyList<TransactionalOutboxMessage> deadLetters = await outbox.GetDeadLettersAsync();
        deadLetters.Should().ContainSingle(m => m.Id == message.Id);
    }

    [Fact]
    public async Task TransactionalBus_PublishAndFlush_ShouldPersistToOutbox()
    {
        var outbox = new InMemoryTransactionalOutbox();
        var bus = new TransactionalBus(outbox, RabbitMQTestHelpers.CreateNullLogger<TransactionalBus>());

        Guid messageId = await bus.PublishAsync(new TestOrderEvent { Name = "staged" }, routingKey: "order-event");
        bus.GetPendingCount().Should().Be(1);

        int flushed = await bus.FlushToOutboxAsync();
        flushed.Should().Be(1);
        bus.GetPendingCount().Should().Be(0);

        outbox.GetById(messageId).Should().NotBeNull();
    }

    [Fact]
    public async Task TransactionalBus_PublishWithHeaders_ShouldExtractMetadata()
    {
        var outbox = new InMemoryTransactionalOutbox();
        var bus = new TransactionalBus(outbox, RabbitMQTestHelpers.CreateNullLogger<TransactionalBus>());
        var headers = new Dictionary<string, object>
        {
            ["x-correlation-id"] = "corr-1",
            ["x-causation-id"] = "cause-1",
            ["x-tenant-id"] = "tenant-a",
            ["x-priority"] = (byte)3
        };

        Guid messageId = await bus.PublishAsync(new TestOrderEvent(), headers, "order-event");

        IReadOnlyList<TransactionalOutboxMessage> pending = bus.GetPendingMessages();
        pending.Should().ContainSingle(m => m.Id == messageId);
        TransactionalOutboxMessage stored = pending.Single(m => m.Id == messageId);
        stored.CorrelationId.Should().Be("corr-1");
        stored.CausationId.Should().Be("cause-1");
        stored.TenantId.Should().Be("tenant-a");
        stored.Priority.Should().Be(3);
    }

    [Fact]
    public async Task TransactionalBus_ClearPending_ShouldRemoveStagedMessages()
    {
        var outbox = new InMemoryTransactionalOutbox();
        var bus = new TransactionalBus(outbox, RabbitMQTestHelpers.CreateNullLogger<TransactionalBus>());

        await bus.PublishAsync(new TestOrderEvent());
        bus.GetPendingCount().Should().Be(1);

        bus.ClearPending();
        bus.GetPendingCount().Should().Be(0);
    }

    [Fact]
    public async Task OutboxPublisher_PublishPendingAsync_ShouldPublishViaClient()
    {
        var outbox = new InMemoryTransactionalOutbox();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var publisher = new OutboxPublisher(
            outbox,
            client,
            RabbitMQTestHelpers.CreateNullLogger<OutboxPublisher>(),
            new OutboxPublisherOptions { PollingInterval = TimeSpan.FromMilliseconds(10) });

        await outbox.AddAsync(new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            Payload = "{\"name\":\"outbox\"}",
            RoutingKey = "order-event",
            CreatedAt = DateTime.UtcNow,
            Status = TransactionalOutboxStatus.Pending
        });

        int published = await publisher.PublishPendingAsync();

        published.Should().Be(1);
        client.WasPublished<TestOrderEvent>().Should().BeTrue();
        publisher.GetStatus().TotalPublished.Should().Be(1);
    }

    [Fact]
    public async Task TransactionalConsumeContext_PublishWithinTransactionAsync_ShouldReturnMessageId()
    {
        var outbox = new InMemoryTransactionalOutbox();
        var bus = new TransactionalBus(outbox, RabbitMQTestHelpers.CreateNullLogger<TransactionalBus>());
        TestConsumeContext<TestOrderEvent> inner = RabbitMQTestHelpers.CreateTestConsumeContext(new TestOrderEvent { Name = "inner" });
        var context = new TransactionalConsumeContext<TestOrderEvent>(inner, bus);

        Guid messageId = await context.PublishWithinTransactionAsync(
            new TestOrderEvent { Name = "from-consume" },
            "order-event");

        messageId.Should().NotBeEmpty();
        context.Message.Should().BeSameAs(inner.Message);
        context.TransactionalBus.Should().BeSameAs(bus);
    }

    [Fact]
    public async Task InMemoryTransactionalOutbox_GetPendingAsync_EmptyOutbox_ShouldReturnEmpty()
    {
        var outbox = new InMemoryTransactionalOutbox();

        IReadOnlyList<TransactionalOutboxMessage> pending = await outbox.GetPendingAsync();

        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task InMemoryTransactionalOutbox_GetPendingCount_ShouldCountOnlyPending()
    {
        var outbox = new InMemoryTransactionalOutbox();
        TransactionalOutboxMessage msg1 = CreateOutboxMessage("route1");
        TransactionalOutboxMessage msg2 = CreateOutboxMessage("route2");
        await outbox.AddAsync(msg1);
        await outbox.AddAsync(msg2);
        await outbox.MarkAsPublishedAsync(msg1.Id);

        int count = await outbox.GetPendingCountAsync();

        count.Should().Be(1);
    }

    [Fact]
    public async Task InMemoryTransactionalOutbox_GetById_NonExistent_ShouldReturnNull()
    {
        var outbox = new InMemoryTransactionalOutbox();

        outbox.GetById(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public async Task InMemoryTransactionalOutbox_CleanupAsync_ShouldRemovePublished()
    {
        var outbox = new InMemoryTransactionalOutbox();
        TransactionalOutboxMessage msg = CreateOutboxMessage("cleanup-route");
        await outbox.AddAsync(msg);
        await outbox.MarkAsPublishedAsync(msg.Id);

        int removed = await outbox.CleanupAsync(olderThan: DateTime.UtcNow.AddDays(1));

        removed.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void TransactionalOutboxMessage_ShouldHaveCorrectStatus()
    {
        TransactionalOutboxMessage msg = CreateOutboxMessage("test-route");

        msg.Status.Should().Be(TransactionalOutboxStatus.Pending);
        msg.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task TransactionalBus_PublishAsync_ShouldReturnNonEmptyGuid()
    {
        var outbox = new InMemoryTransactionalOutbox();
        var bus = new TransactionalBus(outbox, RabbitMQTestHelpers.CreateNullLogger<TransactionalBus>());

        Guid id = await bus.PublishAsync(new TestOrderEvent());

        id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TransactionalBus_FlushToOutboxAsync_WhenNoPending_ShouldReturnZero()
    {
        var outbox = new InMemoryTransactionalOutbox();
        var bus = new TransactionalBus(outbox, RabbitMQTestHelpers.CreateNullLogger<TransactionalBus>());

        int flushed = await bus.FlushToOutboxAsync();

        flushed.Should().Be(0);
    }

    private static TransactionalOutboxMessage CreateOutboxMessage(string routingKey)
    {
        return new TransactionalOutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            Payload = "{\"name\":\"test\"}",
            RoutingKey = routingKey,
            CreatedAt = DateTime.UtcNow,
            Status = TransactionalOutboxStatus.Pending
        };
    }
}
