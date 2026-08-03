using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Mvp24Hours.Application.RabbitMQ.Test.Support;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;

namespace Mvp24Hours.Application.RabbitMQ.Test.Scheduling;

[Trait("Category", "Unit")]
public class RedisScheduledMessageStoreTest
{
    private static RedisScheduledMessageStore CreateStore(string prefix = "test:scheduled:")
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new RedisScheduledMessageStore(cache, prefix, TimeSpan.FromHours(1));
    }

    private static ScheduledMessage CreateMessage(
        Guid? id = null,
        ScheduledMessageStatus status = ScheduledMessageStatus.Pending,
        DateTimeOffset? scheduledTime = null,
        bool recurring = false)
    {
        var message = new ScheduledMessage
        {
            Id = id ?? Guid.NewGuid(),
            Payload = "{\"name\":\"test\"}",
            MessageType = typeof(TestOrderEvent).FullName!,
            RoutingKey = "order",
            ScheduledTime = scheduledTime ?? DateTimeOffset.UtcNow.AddMinutes(-5),
            Status = status
        };

        if (recurring)
        {
            message.RecurringSchedule = new RecurringSchedule { Type = RecurringScheduleType.Interval, Interval = TimeSpan.FromMinutes(5) };
            message.NextExecutionTime = DateTimeOffset.UtcNow.AddMinutes(-1);
            message.Status = ScheduledMessageStatus.Active;
        }

        return message;
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrow()
    {
        Action act = () => new RedisScheduledMessageStore(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task AddAsync_WithNullMessage_ShouldThrow()
    {
        RedisScheduledMessageStore store = CreateStore();

        Func<Task> act = () => store.AddAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateAsync_WithNullMessage_ShouldThrow()
    {
        RedisScheduledMessageStore store = CreateStore();

        Func<Task> act = () => store.UpdateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetDueMessagesAsync_ShouldReturnPastPendingMessages()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage due = CreateMessage(scheduledTime: DateTimeOffset.UtcNow.AddMinutes(-10));
        ScheduledMessage future = CreateMessage(scheduledTime: DateTimeOffset.UtcNow.AddHours(2));

        await store.AddAsync(due);
        await store.AddAsync(future);

        IEnumerable<ScheduledMessage> result = await store.GetDueMessagesAsync();

        result.Should().ContainSingle(m => m.Id == due.Id);
    }

    [Fact]
    public async Task GetDueMessagesAsync_RecurringActive_ShouldUseNextExecutionTime()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage recurring = CreateMessage(recurring: true);
        await store.AddAsync(recurring);

        IEnumerable<ScheduledMessage> result = await store.GetDueMessagesAsync();

        result.Should().ContainSingle(m => m.Id == recurring.Id);
    }

    [Fact]
    public async Task GetPendingMessagesAsync_ShouldReturnOnlyPending()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage pending = CreateMessage(status: ScheduledMessageStatus.Pending);
        ScheduledMessage completed = CreateMessage(status: ScheduledMessageStatus.Completed);

        await store.AddAsync(pending);
        await store.AddAsync(completed);
        await store.UpdateAsync(completed);

        IEnumerable<ScheduledMessage> result = await store.GetPendingMessagesAsync();

        result.Should().ContainSingle(m => m.Id == pending.Id);
    }

    [Fact]
    public async Task GetActiveRecurringMessagesAsync_ShouldReturnActiveRecurringOnly()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage recurring = CreateMessage(recurring: true);
        ScheduledMessage pending = CreateMessage();

        await store.AddAsync(recurring);
        await store.AddAsync(pending);

        IEnumerable<ScheduledMessage> result = await store.GetActiveRecurringMessagesAsync();

        result.Should().ContainSingle(m => m.Id == recurring.Id);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldFilterByStatus()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage failed = CreateMessage(status: ScheduledMessageStatus.Failed);
        await store.AddAsync(failed);
        await store.UpdateAsync(failed);

        IEnumerable<ScheduledMessage> result = await store.GetByStatusAsync(ScheduledMessageStatus.Failed);

        result.Should().ContainSingle(m => m.Id == failed.Id);
    }

    [Fact]
    public async Task MarkAsProcessingAsync_ShouldUpdateStatusOnce()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage message = CreateMessage();
        await store.AddAsync(message);

        bool first = await store.MarkAsProcessingAsync(message.Id);
        bool second = await store.MarkAsProcessingAsync(message.Id);

        first.Should().BeTrue();
        second.Should().BeFalse();
        (await store.GetByIdAsync(message.Id))!.Status.Should().Be(ScheduledMessageStatus.Processing);
    }

    [Fact]
    public async Task MarkAsProcessingAsync_MissingMessage_ShouldReturnFalse()
    {
        RedisScheduledMessageStore store = CreateStore();

        bool result = await store.MarkAsProcessingAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task MarkAsCompletedAsync_ShouldSetProcessedAt()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage message = CreateMessage();
        await store.AddAsync(message);

        await store.MarkAsCompletedAsync(message.Id);

        ScheduledMessage? updated = await store.GetByIdAsync(message.Id);
        updated!.Status.Should().Be(ScheduledMessageStatus.Completed);
        updated.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkAsFailedAsync_ShouldPersistError()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage message = CreateMessage();
        await store.AddAsync(message);

        await store.MarkAsFailedAsync(message.Id, "publish failed");

        ScheduledMessage? updated = await store.GetByIdAsync(message.Id);
        updated!.Status.Should().Be(ScheduledMessageStatus.Failed);
        updated.LastError.Should().Be("publish failed");
    }

    [Fact]
    public async Task RemoveAsync_ShouldDeleteMessage()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage message = CreateMessage();
        await store.AddAsync(message);

        bool removed = await store.RemoveAsync(message.Id);

        removed.Should().BeTrue();
        (await store.GetByIdAsync(message.Id)).Should().BeNull();
    }

    [Fact]
    public async Task CleanupOldMessagesAsync_ShouldRemoveStaleCompletedMessages()
    {
        RedisScheduledMessageStore store = CreateStore();
        ScheduledMessage oldCompleted = CreateMessage(status: ScheduledMessageStatus.Completed);
        await store.AddAsync(oldCompleted);
        oldCompleted.ProcessedAt = DateTimeOffset.UtcNow.AddDays(-10);
        await store.UpdateAsync(oldCompleted);

        ScheduledMessage recentCompleted = CreateMessage(status: ScheduledMessageStatus.Completed);
        await store.AddAsync(recentCompleted);
        recentCompleted.ProcessedAt = DateTimeOffset.UtcNow;
        await store.UpdateAsync(recentCompleted);

        int removed = await store.CleanupOldMessagesAsync(DateTimeOffset.UtcNow.AddDays(-1));

        removed.Should().Be(1);
        (await store.GetByIdAsync(oldCompleted.Id)).Should().BeNull();
        (await store.GetByIdAsync(recentCompleted.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task GetStatusCountsAsync_ShouldIncludeAllStatuses()
    {
        RedisScheduledMessageStore store = CreateStore();
        await store.AddAsync(CreateMessage(status: ScheduledMessageStatus.Pending));
        await store.AddAsync(CreateMessage(status: ScheduledMessageStatus.Failed));

        Dictionary<ScheduledMessageStatus, int> counts = await store.GetStatusCountsAsync();

        counts.Should().ContainKey(ScheduledMessageStatus.Pending);
        counts.Should().ContainKey(ScheduledMessageStatus.Failed);
        counts[ScheduledMessageStatus.Pending].Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoadIndexAsync_ShouldRestoreLocalIndex()
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        var store1 = new RedisScheduledMessageStore(cache, "test:index:");
        ScheduledMessage message = CreateMessage();
        await store1.AddAsync(message);

        var store2 = new RedisScheduledMessageStore(cache, "test:index:");
        await store2.LoadIndexAsync();

        ScheduledMessage? loaded = await store2.GetByIdAsync(message.Id);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDueMessagesAsync_MissingIndexedMessage_ShouldPruneIndex()
    {
        RedisScheduledMessageStore store = CreateStore();
        var orphanId = Guid.NewGuid();

        System.Reflection.FieldInfo? indexField = typeof(RedisScheduledMessageStore)
            .GetField("_messageIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        indexField.Should().NotBeNull();
        object index = indexField!.GetValue(store)!;
        index.GetType().GetMethod("TryAdd")!.Invoke(index, [orphanId, true]);

        IEnumerable<ScheduledMessage> result = await store.GetDueMessagesAsync();

        result.Should().NotContain(m => m.Id == orphanId);
    }
}
