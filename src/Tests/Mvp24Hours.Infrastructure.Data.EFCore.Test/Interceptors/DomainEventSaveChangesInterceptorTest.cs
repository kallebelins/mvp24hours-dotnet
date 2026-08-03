using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.EFCore.Cqrs;
using Mvp24Hours.Infrastructure.Data.EFCore.Interceptors;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test.Interceptors;

[Trait("Category", "Unit")]
public class DomainEventSaveChangesInterceptorTest
{
    [Fact]
    public void SaveChanges_DispatchesDomainEventsThroughDispatcher()
    {
        var dispatcher = new CapturingDomainEventDispatcher();
        var interceptor = new DomainEventSaveChangesInterceptor(dispatcher);

        using TestDbContext context = EfCoreTestHelpers.CreateContext(configure: options =>
            options.AddInterceptors(interceptor));

        var entity = new TestDomainEventEntity { Name = "WithEvents" };
        entity.Raise(new TestDomainEvent("created"));
        context.DomainEventEntities.Add(entity);
        context.SaveChanges();

        dispatcher.DispatchedEntities.Should().ContainSingle();
        dispatcher.DispatchedEvents.Should().ContainSingle()
            .Which.Should().BeOfType<TestDomainEvent>()
            .Which.Message.Should().Be("created");
        entity.DomainEvents.Should().BeEmpty();
    }

    private sealed class CapturingDomainEventDispatcher : IDomainEventDispatcherEFCore
    {
        public List<IHasDomainEvents> DispatchedEntities { get; } = [];
        public List<IDomainEvent> DispatchedEvents { get; } = [];

        public Task DispatchEventsAsync(IHasDomainEvents entity, CancellationToken cancellationToken = default)
        {
            DispatchedEntities.Add(entity);
            DispatchedEvents.AddRange(entity.DomainEvents);
            entity.ClearDomainEvents();
            return Task.CompletedTask;
        }

        public Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default)
        {
            foreach (IHasDomainEvents entity in entities)
            {
                DispatchedEntities.Add(entity);
                DispatchedEvents.AddRange(entity.DomainEvents);
                entity.ClearDomainEvents();
            }

            return Task.CompletedTask;
        }
    }
}
