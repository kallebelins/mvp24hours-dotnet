using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.EFCore.Cqrs;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Cqrs;

[Trait("Category", "Unit")]
public class DomainEventDispatcherAdapterTest
{
    [Fact]
    public async Task DispatchEventsAsync_ShouldInvokeDelegateAndClearEvents()
    {
        List<IDomainEvent> captured = [];
        var adapter = new DomainEventDispatcherAdapter(async (events, _) =>
        {
            captured.AddRange(events);
            await Task.CompletedTask;
        });

        var entity = new TestDomainEventEntity();
        entity.Raise(new TestDomainEvent("dispatched"));

        await adapter.DispatchEventsAsync(entity);

        captured.Should().ContainSingle(e => ((TestDomainEvent)e).Message == "dispatched");
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithMultipleEntities_ShouldDispatchAllAndClear()
    {
        int dispatchedCount = 0;
        var adapter = new DomainEventDispatcherAdapter((events, _) =>
        {
            dispatchedCount += events.Count();
            return Task.CompletedTask;
        });

        var first = new TestDomainEventEntity();
        var second = new TestDomainEventEntity();
        first.Raise(new TestDomainEvent("one"));
        second.Raise(new TestDomainEvent("two"));

        await adapter.DispatchEventsAsync([first, second]);

        dispatchedCount.Should().Be(2);
        first.DomainEvents.Should().BeEmpty();
        second.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WhenDelegateThrows_ShouldNotClearEvents()
    {
        var adapter = new DomainEventDispatcherAdapter((_, _) => throw new InvalidOperationException("fail"));
        var entity = new TestDomainEventEntity();
        entity.Raise(new TestDomainEvent("keep"));

        Func<Task> act = () => adapter.DispatchEventsAsync(entity);

        await act.Should().ThrowAsync<InvalidOperationException>();
        entity.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public void Constructor_WithNullDelegate_ShouldThrow()
    {
        Action act = () => new DomainEventDispatcherAdapter(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
