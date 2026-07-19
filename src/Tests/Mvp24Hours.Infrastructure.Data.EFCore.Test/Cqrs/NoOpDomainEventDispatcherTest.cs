using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.EFCore.Cqrs;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Cqrs;

[Trait("Category", "Unit")]
public class NoOpDomainEventDispatcherTest
{
    [Fact]
    public async Task DispatchEventsAsync_WithSingleEntity_ShouldClearEventsWithoutThrowing()
    {
        var dispatcher = new NoOpDomainEventDispatcher();
        var entity = new TestDomainEventEntity { Name = "Test" };
        entity.Raise(new TestDomainEvent("evt-1"));

        await dispatcher.DispatchEventsAsync(entity);

        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithNullEntity_ShouldNotThrow()
    {
        var dispatcher = new NoOpDomainEventDispatcher();

        Func<Task> act = () => dispatcher.DispatchEventsAsync((IHasDomainEvents?)null!);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithMultipleEntities_ShouldClearAllEvents()
    {
        var dispatcher = new NoOpDomainEventDispatcher();
        var first = new TestDomainEventEntity();
        var second = new TestDomainEventEntity();
        first.Raise(new TestDomainEvent("a"));
        second.Raise(new TestDomainEvent("b"));

        await dispatcher.DispatchEventsAsync([first, second]);

        first.DomainEvents.Should().BeEmpty();
        second.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DispatchEventsAsync_WithNullCollection_ShouldNotThrow()
    {
        var dispatcher = new NoOpDomainEventDispatcher();

        await dispatcher.DispatchEventsAsync((IEnumerable<IHasDomainEvents>?)null!);
    }
}
