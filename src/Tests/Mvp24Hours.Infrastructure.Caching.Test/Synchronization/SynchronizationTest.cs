using Mvp24Hours.Infrastructure.Caching.Synchronization;

namespace Mvp24Hours.Infrastructure.Caching.Test.Synchronization;

[Trait("Category", "Unit")]
public class InMemoryCacheSynchronizerTest
{
    [Fact]
    public async Task PublishInvalidationAsync_ShouldNotifySubscribers()
    {
        var synchronizer = new InMemoryCacheSynchronizer();
        var receivedKeys = new List<string>();
        await synchronizer.SubscribeAsync((key, _) =>
        {
            receivedKeys.Add(key);
            return Task.CompletedTask;
        });

        await synchronizer.PublishInvalidationAsync("key-1");

        receivedKeys.Should().ContainSingle(k => k == "key-1");
    }

    [Fact]
    public async Task PublishInvalidationManyAsync_ShouldNotifyForEachKey()
    {
        var synchronizer = new InMemoryCacheSynchronizer();
        var receivedKeys = new List<string>();
        await synchronizer.SubscribeAsync((key, _) =>
        {
            lock (receivedKeys)
            {
                receivedKeys.Add(key);
            }

            return Task.CompletedTask;
        });

        await synchronizer.PublishInvalidationManyAsync(["a", "b"]);

        receivedKeys.Should().BeEquivalentTo(["a", "b"]);
    }

    [Fact]
    public async Task PublishInvalidationAsync_EmptyKey_ShouldNoOp()
    {
        var synchronizer = new InMemoryCacheSynchronizer();
        bool called = false;
        await synchronizer.SubscribeAsync((_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await synchronizer.PublishInvalidationAsync(" ");

        called.Should().BeFalse();
    }

    [Fact]
    public async Task SubscribeAsync_NullHandler_ShouldThrow()
    {
        var synchronizer = new InMemoryCacheSynchronizer();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            synchronizer.SubscribeAsync(null!));
    }

    [Fact]
    public async Task UnsubscribeAsync_ShouldClearAllSubscribers()
    {
        var synchronizer = new InMemoryCacheSynchronizer();
        bool called = false;
        await synchronizer.SubscribeAsync((_, _) =>
        {
            called = true;
            return Task.CompletedTask;
        });

        await synchronizer.UnsubscribeAsync();
        await synchronizer.PublishInvalidationAsync("key");

        called.Should().BeFalse();
    }

    [Fact]
    public async Task PublishInvalidationAsync_SubscriberThrows_ShouldNotBreakOtherSubscribers()
    {
        var synchronizer = new InMemoryCacheSynchronizer();
        bool secondCalled = false;
        await synchronizer.SubscribeAsync((_, _) => throw new InvalidOperationException("subscriber failed"));
        await synchronizer.SubscribeAsync((_, _) =>
        {
            secondCalled = true;
            return Task.CompletedTask;
        });

        await synchronizer.PublishInvalidationAsync("key");

        secondCalled.Should().BeTrue();
    }
}
