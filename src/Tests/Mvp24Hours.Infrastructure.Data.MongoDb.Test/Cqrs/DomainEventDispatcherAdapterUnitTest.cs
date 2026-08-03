using Microsoft.Extensions.Logging.Abstractions;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.MongoDb.Cqrs;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Cqrs;

[Trait("Category", "Unit")]
public class DomainEventDispatcherAdapterUnitTest
{
    [Fact]
    public void Constructor_WithNullDispatchFunc_ShouldThrow()
    {
        Action act = () => _ = new DomainEventDispatcherAdapter(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithNullEntity_ShouldReturnWithoutCallingDispatch()
    {
        int dispatchCount = 0;
        var adapter = new DomainEventDispatcherAdapter((_, _) =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        });

        await adapter.DispatchEventsAsync((IHasDomainEvents)null!);

        dispatchCount.Should().Be(0);
    }

    [Fact]
    public async Task DispatchEventsAsync_WithNoEvents_ShouldNotCallDispatch()
    {
        int dispatchCount = 0;
        var adapter = new DomainEventDispatcherAdapter((_, _) =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        });
        var entity = new TestDomainEventEntity();

        await adapter.DispatchEventsAsync(entity);

        dispatchCount.Should().Be(0);
    }

    [Fact]
    public async Task DispatchEventsAsync_WithEvents_ShouldDispatchAndClearEvents()
    {
        var dispatched = new List<IDomainEvent>();
        var adapter = new DomainEventDispatcherAdapter((events, _) =>
        {
            dispatched.AddRange(events);
            return Task.CompletedTask;
        }, NullLogger<DomainEventDispatcherAdapter>.Instance);
        var entity = new TestDomainEventEntity();
        entity.Raise(new TestDomainEvent { Message = "created" });

        await adapter.DispatchEventsAsync(entity);

        dispatched.Should().ContainSingle().Which.Should().BeOfType<TestDomainEvent>();
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WhenDispatchFails_ShouldNotClearEvents()
    {
        var adapter = new DomainEventDispatcherAdapter((_, _) =>
            throw new InvalidOperationException("dispatch failed"));
        var entity = new TestDomainEventEntity();
        entity.Raise(new TestDomainEvent());

        Func<Task> act = () => adapter.DispatchEventsAsync(entity);

        await act.Should().ThrowAsync<InvalidOperationException>();
        entity.DomainEvents.Should().ContainSingle();
    }

    [Fact]
    public async Task DispatchEventsAsync_ForMultipleEntities_ShouldDispatchAllEvents()
    {
        int totalEvents = 0;
        var adapter = new DomainEventDispatcherAdapter((events, _) =>
        {
            totalEvents += events.Count();
            return Task.CompletedTask;
        });
        var first = new TestDomainEventEntity();
        var second = new TestDomainEventEntity();
        first.Raise(new TestDomainEvent());
        second.Raise(new TestDomainEvent());
        second.Raise(new TestDomainEvent());

        await adapter.DispatchEventsAsync([first, second]);

        totalEvents.Should().Be(3);
        first.DomainEvents.Should().BeEmpty();
        second.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithNullEntities_ShouldReturnWithoutCallingDispatch()
    {
        int dispatchCount = 0;
        var adapter = new DomainEventDispatcherAdapter((_, _) =>
        {
            dispatchCount++;
            return Task.CompletedTask;
        });

        await adapter.DispatchEventsAsync((IEnumerable<IHasDomainEvents>)null!);

        dispatchCount.Should().Be(0);
    }

    [Fact]
    public void DispatchEvents_SyncOverload_ShouldDispatchEvents()
    {
        int dispatchCount = 0;
        var adapter = new DomainEventDispatcherAdapter((events, _) =>
        {
            dispatchCount += events.Count();
            return Task.CompletedTask;
        });
        var entity = new TestDomainEventEntity();
        entity.Raise(new TestDomainEvent());

        adapter.DispatchEvents(entity);

        dispatchCount.Should().Be(1);
        entity.DomainEvents.Should().BeEmpty();
    }
}

public class TestDomainEventEntity : IHasDomainEvents
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

    public void Raise(IDomainEvent domainEvent)
    {
        _events.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _events.Clear();
    }
}

public class TestDomainEvent : IDomainEvent
{
    public string Message { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid EventId { get; set; } = Guid.NewGuid();
}
