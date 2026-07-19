using Mvp24Hours.Infrastructure.RabbitMQ.Deduplication;

namespace Mvp24Hours.Application.RabbitMQ.Test.Deduplication;

public class DeduplicationTest
{
    [Fact]
    public async Task InMemoryMessageDeduplicationStore_IsProcessedAsync_NewMessage_ShouldReturnFalse()
    {
        var store = new InMemoryMessageDeduplicationStore();

        bool result = await store.IsProcessedAsync("msg-001");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_MarkAsProcessedAndCheck_ShouldReturnTrue()
    {
        var store = new InMemoryMessageDeduplicationStore();
        const string messageId = "msg-002";

        await store.MarkAsProcessedAsync(messageId);
        bool result = await store.IsProcessedAsync(messageId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_IsProcessedAsync_NullMessageId_ShouldThrow()
    {
        var store = new InMemoryMessageDeduplicationStore();

        Func<Task> act = () => store.IsProcessedAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_MarkAsProcessed_NullMessageId_ShouldThrow()
    {
        var store = new InMemoryMessageDeduplicationStore();

        Func<Task> act = () => store.MarkAsProcessedAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_Remove_NullMessageId_ShouldThrow()
    {
        var store = new InMemoryMessageDeduplicationStore();

        Func<Task> act = () => store.RemoveAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_RemoveAsync_ShouldMakeMessageUnprocessed()
    {
        var store = new InMemoryMessageDeduplicationStore();
        const string messageId = "msg-003";

        await store.MarkAsProcessedAsync(messageId);
        await store.RemoveAsync(messageId);
        bool result = await store.IsProcessedAsync(messageId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_MarkAsProcessed_WithExpiry_ShouldExpireAfterTime()
    {
        var store = new InMemoryMessageDeduplicationStore();
        const string messageId = "msg-004";

        DateTimeOffset past = DateTimeOffset.UtcNow.AddSeconds(-1);
        await store.MarkAsProcessedAsync(messageId, expiresAt: past);
        bool result = await store.IsProcessedAsync(messageId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_MarkAsProcessed_WithFutureExpiry_ShouldRemainProcessed()
    {
        var store = new InMemoryMessageDeduplicationStore();
        const string messageId = "msg-005";

        DateTimeOffset future = DateTimeOffset.UtcNow.AddHours(1);
        await store.MarkAsProcessedAsync(messageId, expiresAt: future);
        bool result = await store.IsProcessedAsync(messageId);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_CleanupExpiredAsync_ShouldRemoveExpiredEntries()
    {
        var store = new InMemoryMessageDeduplicationStore();

        await store.MarkAsProcessedAsync("expired-1", expiresAt: DateTimeOffset.UtcNow.AddSeconds(-5));
        await store.MarkAsProcessedAsync("expired-2", expiresAt: DateTimeOffset.UtcNow.AddSeconds(-5));
        await store.MarkAsProcessedAsync("valid-1", expiresAt: DateTimeOffset.UtcNow.AddHours(1));

        await store.CleanupExpiredAsync();

        store.Count.Should().Be(1);
        (await store.IsProcessedAsync("valid-1")).Should().BeTrue();
        (await store.IsProcessedAsync("expired-1")).Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_MultipleMessages_ShouldTrackIndependently()
    {
        var store = new InMemoryMessageDeduplicationStore();

        await store.MarkAsProcessedAsync("msg-a");
        await store.MarkAsProcessedAsync("msg-b");

        (await store.IsProcessedAsync("msg-a")).Should().BeTrue();
        (await store.IsProcessedAsync("msg-b")).Should().BeTrue();
        (await store.IsProcessedAsync("msg-c")).Should().BeFalse();

        store.Count.Should().Be(2);
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_MarkAsProcessed_ShouldOverwriteExistingEntry()
    {
        var store = new InMemoryMessageDeduplicationStore();
        const string messageId = "msg-006";

        DateTimeOffset past = DateTimeOffset.UtcNow.AddSeconds(-1);
        await store.MarkAsProcessedAsync(messageId, expiresAt: past);
        (await store.IsProcessedAsync(messageId)).Should().BeFalse();

        DateTimeOffset future = DateTimeOffset.UtcNow.AddHours(1);
        await store.MarkAsProcessedAsync(messageId, expiresAt: future);
        (await store.IsProcessedAsync(messageId)).Should().BeTrue();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_RemoveNonExistent_ShouldNotThrow()
    {
        var store = new InMemoryMessageDeduplicationStore();

        Func<Task> act = () => store.RemoveAsync("nonexistent-id");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_CleanupExpiredAsync_WithCancellation_ShouldNotThrow()
    {
        var store = new InMemoryMessageDeduplicationStore();
        await store.MarkAsProcessedAsync("msg-1");

        using var cts = new CancellationTokenSource();

        Func<Task> act = () => store.CleanupExpiredAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void InMemoryMessageDeduplicationStore_Count_Initial_ShouldBeZero()
    {
        var store = new InMemoryMessageDeduplicationStore();

        store.Count.Should().Be(0);
    }

    [Fact]
    public async Task InMemoryMessageDeduplicationStore_Count_AfterMarking_ShouldReflectEntries()
    {
        var store = new InMemoryMessageDeduplicationStore();

        await store.MarkAsProcessedAsync("msg-x");
        await store.MarkAsProcessedAsync("msg-y");

        store.Count.Should().Be(2);
    }
}
