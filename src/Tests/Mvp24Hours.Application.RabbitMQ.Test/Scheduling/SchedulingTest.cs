using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.Scheduling;

[Trait("Category", "Unit")]
public class SchedulingTest
{
    [Fact]
    public void CronExpressionHelper_IsValid_ShouldAcceptCommonExpressions()
    {
        CronExpressionHelper.IsValid("*/5 * * * *").Should().BeTrue();
        CronExpressionHelper.IsValid("0 9 * * 1-5").Should().BeTrue();
        CronExpressionHelper.IsValid("invalid").Should().BeFalse();
    }

    [Fact]
    public void CronExpressionHelper_GetDescription_ShouldReturnKnownText()
    {
        CronExpressionHelper.GetDescription("0 9 * * *").Should().Be("Every day at 9:00 AM");
        CronExpressionHelper.GetDescription("custom expr").Should().Contain("Custom:");
    }

    [Fact]
    public void CronExpressionHelper_GetNextOccurrence_WithInvalidFieldCount_ShouldThrow()
    {
        Action act = () => CronExpressionHelper.GetNextOccurrence("* * *");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*5 fields*");
    }

    [Fact]
    public void CronExpressionHelper_GetNextOccurrence_ShouldReturnFutureUtcTime()
    {
        DateTimeOffset from = new(2026, 1, 1, 10, 0, 0, TimeSpan.Zero);

        DateTimeOffset? next = CronExpressionHelper.GetNextOccurrence("0 11 * * *", from, "UTC");

        next.Should().NotBeNull();
        next!.Value.Hour.Should().Be(11);
        next.Value.Should().BeAfter(from);
    }

    [Fact]
    public async Task InMemoryScheduledMessageStore_AddAndGetDue_ShouldReturnScheduledMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = "{}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "order",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = ScheduledMessageStatus.Pending
        };

        await store.AddAsync(message);

        IEnumerable<ScheduledMessage> due = await store.GetDueMessagesAsync();
        due.Should().ContainSingle(m => m.Id == message.Id);
    }

    [Fact]
    public async Task InMemoryScheduledMessageStore_MarkAsProcessing_ShouldBeIdempotent()
    {
        var store = new InMemoryScheduledMessageStore();
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = "{}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "order",
            ScheduledTime = DateTimeOffset.UtcNow,
            Status = ScheduledMessageStatus.Pending
        };
        await store.AddAsync(message);

        bool first = await store.MarkAsProcessingAsync(message.Id);
        bool second = await store.MarkAsProcessingAsync(message.Id);

        first.Should().BeTrue();
        second.Should().BeFalse();
    }

    [Fact]
    public async Task MessageScheduler_ScheduleInFuture_ShouldPersistMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(
            store,
            client,
            Options.Create(new MessageSchedulerOptions()));

        Guid id = await scheduler.ScheduleMessageAsync(
            DateTimeOffset.UtcNow.AddMinutes(5),
            new TestOrderEvent { Name = "future" },
            new ScheduleMessageOptions { RoutingKey = "order-event" });

        ScheduledMessage? stored = await store.GetByIdAsync(id);
        stored.Should().NotBeNull();
        stored!.RoutingKey.Should().Be("order-event");
    }

    [Fact]
    public async Task MessageScheduler_ScheduleInPast_ShouldThrow()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(store, client, Options.Create(new MessageSchedulerOptions()));

        Func<Task> act = () => scheduler.ScheduleMessageAsync(
            DateTimeOffset.UtcNow.AddMinutes(-1),
            new TestOrderEvent(),
            new ScheduleMessageOptions { RoutingKey = "order" });

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*future*");
    }

    [Fact]
    public async Task RedisScheduledMessageStore_AddAndGetById_ShouldRoundTrip()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var store = new RedisScheduledMessageStore(cache);
        var message = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = "{\"name\":\"redis\"}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "order",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(10),
            Status = ScheduledMessageStatus.Pending
        };

        await store.AddAsync(message);

        ScheduledMessage? loaded = await store.GetByIdAsync(message.Id);
        loaded.Should().NotBeNull();
        loaded!.RoutingKey.Should().Be("order");
    }

    [Fact]
    public async Task InMemoryScheduledMessageStore_FullLifecycle_ShouldSupportAllQueries()
    {
        var store = new InMemoryScheduledMessageStore();
        var pending = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = "{}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "order",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = ScheduledMessageStatus.Pending
        };
        var recurring = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = "{}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "recurring",
            ScheduledTime = DateTimeOffset.UtcNow,
            Status = ScheduledMessageStatus.Active,
            RecurringSchedule = new RecurringSchedule { Type = RecurringScheduleType.Interval, Interval = TimeSpan.FromMinutes(1) },
            NextExecutionTime = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        await store.AddAsync(pending);
        await store.AddAsync(recurring);

        (await store.GetPendingMessagesAsync()).Should().ContainSingle(m => m.Id == pending.Id);
        (await store.GetActiveRecurringMessagesAsync()).Should().ContainSingle(m => m.Id == recurring.Id);
        (await store.GetDueMessagesAsync()).Should().HaveCount(2);
        (await store.GetByStatusAsync(ScheduledMessageStatus.Pending)).Should().ContainSingle(m => m.Id == pending.Id);

        await store.MarkAsProcessingAsync(pending.Id);
        await store.MarkAsCompletedAsync(pending.Id);
        await store.MarkAsFailedAsync(recurring.Id, "failed");

        Dictionary<ScheduledMessageStatus, int> counts = await store.GetStatusCountsAsync();
        counts[ScheduledMessageStatus.Completed].Should().Be(1);
        counts[ScheduledMessageStatus.Failed].Should().Be(1);

        pending.ProcessedAt = DateTimeOffset.UtcNow.AddDays(-10);
        await store.UpdateAsync(pending);
        int removed = await store.CleanupOldMessagesAsync(DateTimeOffset.UtcNow.AddDays(-1));
        removed.Should().Be(1);
    }

    [Fact]
    public async Task MessageScheduler_ScheduleWithDelay_ShouldPersistMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(store, client, Options.Create(new MessageSchedulerOptions()));

        Guid id = await scheduler.ScheduleMessageAsync(
            TimeSpan.FromMinutes(10),
            new TestOrderEvent { Name = "delay" },
            routingKey: "order-event");

        ScheduledMessage? stored = await store.GetByIdAsync(id);
        stored.Should().NotBeNull();
        stored!.RoutingKey.Should().Be("order-event");
    }

    [Fact]
    public async Task MessageScheduler_ScheduleWithZeroDelay_ShouldThrow()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(store, client, Options.Create(new MessageSchedulerOptions()));

        Func<Task> act = () => scheduler.ScheduleMessageAsync(
            TimeSpan.Zero,
            new TestOrderEvent(),
            routingKey: "order");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Delay must be positive*");
    }

    [Fact]
    public async Task MessageScheduler_CancelScheduledMessage_ShouldMarkAsCancelled()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(store, client, Options.Create(new MessageSchedulerOptions()));

        Guid id = await scheduler.ScheduleMessageAsync(
            DateTimeOffset.UtcNow.AddMinutes(5),
            new TestOrderEvent { Name = "cancel" },
            new ScheduleMessageOptions { RoutingKey = "order" });

        bool cancelled = await scheduler.CancelScheduledMessageAsync(id);

        cancelled.Should().BeTrue();
        ScheduledMessage? stored = await store.GetByIdAsync(id);
        stored.Should().NotBeNull();
        stored!.Status.Should().Be(ScheduledMessageStatus.Cancelled);
    }

    [Fact]
    public async Task MessageScheduler_ProcessDueMessagesAsync_ShouldPublishDueMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(store, client, Options.Create(new MessageSchedulerOptions()));
        var dueMessage = new ScheduledMessage
        {
            Id = Guid.NewGuid(),
            Payload = System.Text.Json.JsonSerializer.Serialize(new TestOrderEvent { Name = "due" }),
            MessageType = typeof(TestOrderEvent).AssemblyQualifiedName!,
            RoutingKey = "order-event",
            ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = ScheduledMessageStatus.Pending
        };
        await store.AddAsync(dueMessage);

        int processed = await scheduler.ProcessDueMessagesAsync();

        processed.Should().Be(1);
        client.WasPublished<TestOrderEvent>().Should().BeTrue();
        (await store.GetByIdAsync(dueMessage.Id))!.Status.Should().Be(ScheduledMessageStatus.Completed);
    }

    [Fact]
    public async Task MessageScheduler_ScheduleRecurringInterval_ShouldPersistActiveMessage()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(
            store,
            client,
            Options.Create(new MessageSchedulerOptions { MinimumRecurringInterval = TimeSpan.FromMinutes(1) }));

        Guid id = await scheduler.ScheduleRecurringMessageAsync(
            TimeSpan.FromMinutes(5),
            new TestOrderEvent { Name = "recurring" },
            routingKey: "order-event",
            maxExecutions: 3);

        ScheduledMessage? stored = await store.GetByIdAsync(id);
        stored.Should().NotBeNull();
        stored!.IsRecurring.Should().BeTrue();
        stored.Status.Should().Be(ScheduledMessageStatus.Active);
    }

    [Fact]
    public async Task MessageScheduler_PauseAndResumeRecurring_ShouldToggleStatus()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(
            store,
            client,
            Options.Create(new MessageSchedulerOptions { MinimumRecurringInterval = TimeSpan.FromMinutes(1) }));

        Guid id = await scheduler.ScheduleRecurringMessageAsync(
            TimeSpan.FromMinutes(5),
            new TestOrderEvent { Name = "pause-resume" },
            routingKey: "order-event");

        (await scheduler.PauseRecurringMessageAsync(id)).Should().BeTrue();
        (await store.GetByIdAsync(id))!.Status.Should().Be(ScheduledMessageStatus.Paused);

        (await scheduler.ResumeRecurringMessageAsync(id)).Should().BeTrue();
        (await store.GetByIdAsync(id))!.Status.Should().Be(ScheduledMessageStatus.Active);
    }

    [Fact]
    public async Task MessageScheduler_ScheduleRecurringCron_WithInvalidExpression_ShouldThrow()
    {
        var store = new InMemoryScheduledMessageStore();
        InMemoryBus client = RabbitMQTestHelpers.CreateInMemoryBus();
        var scheduler = new MessageScheduler(store, client, Options.Create(new MessageSchedulerOptions()));

        Func<Task> act = () => scheduler.ScheduleRecurringMessageAsync(
            "not-a-cron",
            new TestOrderEvent(),
            routingKey: "order");

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid CRON*");
    }
}
