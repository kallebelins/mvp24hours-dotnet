using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Configuration;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using Mvp24Hours.Infrastructure.RabbitMQ.Testing;

namespace Mvp24Hours.Application.RabbitMQ.Test.Scheduling;

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
}
