using Mvp24Hours.Application.Contract.Events;
using Mvp24Hours.Application.Logic.Events;
using Mvp24Hours.Application.Test.Support;

namespace Mvp24Hours.Application.Test.Logic.Events;

[Trait("Category", "Unit")]
public class InMemoryApplicationEventOutboxTest
{
    [Fact]
    public async Task AddAsync_ShouldStorePendingEntry()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        var @event = new TestApplicationEvent { Payload = "stored", CorrelationId = "c-1" };

        await outbox.AddAsync(@event);

        outbox.GetAll().Should().ContainSingle(e =>
            e.Status == ApplicationEventOutboxStatus.Pending &&
            e.CorrelationId == "c-1");
    }

    [Fact]
    public async Task AddRangeAsync_ShouldStoreAllEvents()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        IApplicationEvent[] events =
        [
            new TestApplicationEvent { Payload = "1" },
            new TestApplicationEvent { Payload = "2" }
        ];

        await outbox.AddRangeAsync(events);

        outbox.GetAll().Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPendingAsync_ShouldReturnPendingAndFailedEntries()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        await outbox.AddAsync(new TestApplicationEvent());
        ApplicationEventOutboxEntry entry = outbox.GetAll().Single();
        await outbox.MarkAsFailedAsync(entry.Id, "error");

        IReadOnlyList<ApplicationEventOutboxEntry> pending = await outbox.GetPendingAsync();

        pending.Should().ContainSingle(e => e.Id == entry.Id && e.Status == ApplicationEventOutboxStatus.Failed);
    }

    [Fact]
    public async Task MarkAsDispatchedAsync_ShouldUpdateStatus()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        await outbox.AddAsync(new TestApplicationEvent());
        Guid entryId = outbox.GetAll().Single().Id;

        await outbox.MarkAsDispatchedAsync(entryId);

        outbox.GetByStatus(ApplicationEventOutboxStatus.Dispatched).Should().ContainSingle(e => e.Id == entryId);
    }

    [Fact]
    public async Task MarkAsFailedAsync_AfterMaxRetries_ShouldMoveToDeadLetter()
    {
        var outbox = new InMemoryApplicationEventOutbox(maxRetries: 2);
        await outbox.AddAsync(new TestApplicationEvent());
        Guid entryId = outbox.GetAll().Single().Id;

        await outbox.MarkAsFailedAsync(entryId, "fail-1");
        await outbox.MarkAsFailedAsync(entryId, "fail-2");

        outbox.GetByStatus(ApplicationEventOutboxStatus.DeadLetter).Should().ContainSingle(e => e.Id == entryId);
    }

    [Fact]
    public async Task CleanupAsync_ShouldRemoveOldDispatchedEntries()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        await outbox.AddAsync(new TestApplicationEvent());
        ApplicationEventOutboxEntry entry = outbox.GetAll().Single();
        await outbox.MarkAsDispatchedAsync(entry.Id);
        entry.DispatchedAt = DateTime.UtcNow.AddDays(-10);

        int removed = await outbox.CleanupAsync(DateTime.UtcNow.AddDays(-1));

        removed.Should().Be(1);
        outbox.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Clear_ShouldRemoveAllEntries()
    {
        var outbox = new InMemoryApplicationEventOutbox();
        outbox.AddAsync(new TestApplicationEvent()).GetAwaiter().GetResult();

        outbox.Clear();

        outbox.GetAll().Should().BeEmpty();
    }
}
