using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Metrics;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.Observability;

[Trait("Category", "Unit")]
public class RabbitMQPrometheusMetricsTest
{
    [Fact]
    public void IncrementMethods_ShouldUpdateCounters()
    {
        using var metrics = new RabbitMQPrometheusMetrics();

        metrics.IncrementMessagesSent("orders");
        metrics.IncrementMessagesReceived("queue-a");
        metrics.IncrementMessagesAcked();
        metrics.IncrementMessagesNacked();
        metrics.IncrementMessagesRejected();
        metrics.IncrementMessagesRedelivered();
        metrics.IncrementPublisherConfirms();
        metrics.IncrementPublisherNacks();
        metrics.IncrementConnectionFailures();
        metrics.IncrementChannelCreations();
        metrics.IncrementDuplicateMessagesSkipped();
        metrics.IncrementError("InvalidOperationException");

        metrics.MessagesSent.Should().Be(1);
        metrics.MessagesReceived.Should().Be(1);
        metrics.MessagesAcked.Should().Be(1);
        metrics.MessagesNacked.Should().Be(1);
        metrics.MessagesRejected.Should().Be(1);
        metrics.MessagesRedelivered.Should().Be(1);
        metrics.PublisherConfirms.Should().Be(1);
        metrics.PublisherNacks.Should().Be(1);
        metrics.ConnectionFailures.Should().Be(1);
        metrics.ChannelCreations.Should().Be(1);
        metrics.DuplicateMessagesSkipped.Should().Be(1);
    }

    [Fact]
    public void GetSnapshot_ShouldReturnCopyOfState()
    {
        using var metrics = new RabbitMQPrometheusMetrics();
        metrics.IncrementMessagesSent("orders");
        metrics.IncrementMessagesReceived("queue-a");
        metrics.IncrementError("TimeoutException");

        RabbitMQMetricsSnapshot snapshot = metrics.GetSnapshot();

        snapshot.MessagesSent.Should().Be(1);
        snapshot.MessagesReceived.Should().Be(1);
        snapshot.MessagesByExchange["orders"].Should().Be(1);
        snapshot.MessagesByQueue["queue-a"].Should().Be(1);
        snapshot.ErrorsByType["TimeoutException"].Should().Be(1);
        snapshot.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Reset_ShouldClearAllCounters()
    {
        using var metrics = new RabbitMQPrometheusMetrics();
        metrics.IncrementMessagesSent("orders");
        metrics.IncrementMessagesReceived("queue-a");
        metrics.IncrementError("Exception");

        metrics.Reset();

        metrics.MessagesSent.Should().Be(0);
        metrics.MessagesReceived.Should().Be(0);
        metrics.GetSnapshot().ErrorsByType.Should().BeEmpty();
    }

    [Fact]
    public async Task ConsumeObserverMethods_ShouldUpdateMetrics()
    {
        using var metrics = new RabbitMQPrometheusMetrics();
        var context = new ConsumeObserverContext
        {
            MessageId = "msg-1",
            MessageType = "OrderCreated",
            QueueName = "orders",
            PayloadSize = 100,
            Duration = TimeSpan.FromMilliseconds(50),
            Redelivered = true
        };

        await metrics.PreConsumeAsync(context);
        await metrics.PostConsumeAsync(context);
        await metrics.ConsumeFaultAsync(context, new InvalidOperationException("fail"));

        metrics.MessagesReceived.Should().Be(1);
        metrics.MessagesRedelivered.Should().Be(1);
        metrics.MessagesAcked.Should().Be(1);
        metrics.GetSnapshot().ErrorsByType.Should().ContainKey(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task PublishObserverMethods_ShouldUpdateMetrics()
    {
        using var metrics = new RabbitMQPrometheusMetrics();
        var context = new PublishObserverContext
        {
            MessageId = "msg-2",
            MessageType = "OrderCreated",
            Exchange = "orders",
            RoutingKey = "order.created",
            PayloadSize = 200,
            Duration = TimeSpan.FromMilliseconds(20),
            Confirmed = true
        };

        await metrics.PrePublishAsync(context);
        await metrics.PostPublishAsync(context);
        await metrics.PublishFaultAsync(context, new InvalidOperationException("publish fail"));

        metrics.MessagesSent.Should().Be(1);
        metrics.PublisherConfirms.Should().Be(1);
        metrics.PublisherNacks.Should().Be(1);
    }

    [Fact]
    public void RecordConnectionEvent_ShouldNotThrow()
    {
        using var metrics = new RabbitMQPrometheusMetrics();

        Action act = () => metrics.RecordConnectionEvent("connected", "localhost");

        act.Should().NotThrow();
    }

    [Fact]
    public void MeterName_ShouldBeStable()
    {
        RabbitMQPrometheusMetrics.MeterName.Should().Be("Mvp24Hours.RabbitMQ");
    }
}

[Trait("Category", "Unit")]
public class MessageSchedulerExtendedTest
{
    private static MessageScheduler CreateScheduler(
        InMemoryScheduledMessageStore? store = null,
        InMemoryBus? bus = null,
        MessageSchedulerOptions? options = null)
    {
        return new MessageScheduler(
            store ?? new InMemoryScheduledMessageStore(),
            bus ?? RabbitMQTestHelpers.CreateInMemoryBus(),
            Options.Create(options ?? new MessageSchedulerOptions()));
    }

    [Fact]
    public async Task ScheduleMessageAsync_WithDelay_ShouldPersistMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);

        Guid id = await scheduler.ScheduleMessageAsync(
            TimeSpan.FromMinutes(5),
            new TestOrderEvent { Name = "delayed" },
            routingKey: "order");

        ScheduledMessage? message = await store.GetByIdAsync(id);
        message.Should().NotBeNull();
        message!.RoutingKey.Should().Be("order");
    }

    [Fact]
    public async Task ScheduleRecurringMessageAsync_WithInterval_ShouldPersistActiveMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);

        Guid id = await scheduler.ScheduleRecurringMessageAsync(
            TimeSpan.FromMinutes(2),
            new TestOrderEvent { Name = "recurring" },
            routingKey: "order",
            maxExecutions: 3);

        ScheduledMessage? message = await store.GetByIdAsync(id);
        message.Should().NotBeNull();
        message!.IsRecurring.Should().BeTrue();
        message.Status.Should().Be(ScheduledMessageStatus.Active);
    }

    [Fact]
    public async Task ScheduleRecurringMessageAsync_WithCron_ShouldPersistMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);

        Guid id = await scheduler.ScheduleRecurringMessageAsync(
            "0 10 * * *",
            new TestOrderEvent { Name = "cron" },
            routingKey: "order");

        ScheduledMessage? message = await store.GetByIdAsync(id);
        message.Should().NotBeNull();
        message!.RecurringSchedule!.Type.Should().Be(RecurringScheduleType.Cron);
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_ShouldReturnFalse_WhenMessageMissing()
    {
        MessageScheduler scheduler = CreateScheduler();

        bool cancelled = await scheduler.CancelScheduledMessageAsync(Guid.NewGuid());

        cancelled.Should().BeFalse();
    }

    [Fact]
    public async Task CancelScheduledMessageAsync_ShouldCancelPendingMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        Guid id = await scheduler.ScheduleMessageAsync(
            DateTimeOffset.UtcNow.AddMinutes(10),
            new TestOrderEvent { Name = "cancel-me" },
            new ScheduleMessageOptions { RoutingKey = "order" });

        bool cancelled = await scheduler.CancelScheduledMessageAsync(id);

        cancelled.Should().BeTrue();
        (await store.GetByIdAsync(id))!.Status.Should().Be(ScheduledMessageStatus.Cancelled);
    }

    [Fact]
    public async Task PauseAndResumeRecurringMessageAsync_ShouldToggleStatus()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        Guid id = await scheduler.ScheduleRecurringMessageAsync(
            TimeSpan.FromMinutes(2),
            new TestOrderEvent { Name = "pause-me" },
            routingKey: "order");

        (await scheduler.PauseRecurringMessageAsync(id)).Should().BeTrue();
        (await store.GetByIdAsync(id))!.Status.Should().Be(ScheduledMessageStatus.Paused);

        (await scheduler.ResumeRecurringMessageAsync(id)).Should().BeTrue();
        (await store.GetByIdAsync(id))!.Status.Should().Be(ScheduledMessageStatus.Active);
    }

    [Fact]
    public async Task ProcessDueMessagesAsync_ShouldPublishDueMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus bus = RabbitMQTestHelpers.CreateInMemoryBus();
        MessageScheduler scheduler = CreateScheduler(store, bus);
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = System.Text.Json.JsonSerializer.Serialize(new TestOrderEvent { Name = "due" }),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            RoutingKey = "order",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = ScheduledMessageStatus.Pending
        };
        await store.AddAsync(message);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(1);
        bus.PublishedMessages.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldDelegateToStore()
    {
        var store = new InMemoryScheduledMessageStore();
        MessageScheduler scheduler = CreateScheduler(store);
        await scheduler.ScheduleMessageAsync(
            DateTimeOffset.UtcNow.AddMinutes(10),
            new TestOrderEvent { Name = "pending" },
            new ScheduleMessageOptions { RoutingKey = "order" });

        IEnumerable<ScheduledMessage> pending = await scheduler.GetPendingMessagesAsync();

        pending.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ScheduleRecurringMessageAsync_WithTooSmallInterval_ShouldThrow()
    {
        MessageScheduler scheduler = CreateScheduler();

        Func<Task> act = () => scheduler.ScheduleRecurringMessageAsync(
            TimeSpan.FromSeconds(10),
            new TestOrderEvent(),
            routingKey: "order");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ScheduleRecurringMessageAsync_WithInvalidCron_ShouldThrow()
    {
        MessageScheduler scheduler = CreateScheduler();

        Func<Task> act = () => scheduler.ScheduleRecurringMessageAsync(
            "not-a-cron",
            new TestOrderEvent(),
            routingKey: "order");

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
