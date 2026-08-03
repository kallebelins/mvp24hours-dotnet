using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class UnitOfWorkWithEventsTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly List<IDomainEvent> _dispatchedEvents = [];
    private readonly string _databaseName = $"UowEventsSync_{Guid.NewGuid():N}";

    public UnitOfWorkWithEventsTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>(_databaseName);
        services.AddMvp24HoursRepository(o => o.MaxQtyByQueryPage = 100, unitOfWork: typeof(UnitOfWorkWithEvents));
        services.AddScoped<IUnitOfWorkWithEvents, UnitOfWorkWithEvents>();
        services.AddMvp24HoursDomainEventDispatcher(async (events, _) =>
        {
            _dispatchedEvents.AddRange(events);
            await Task.CompletedTask;
        });
        _provider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _provider.Dispose();
    }

    [Fact]
    public void GetEntitiesWithEvents_ShouldReturnEntitiesWithPendingEvents()
    {
        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWorkWithEvents unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEvents>();
        IRepository<TestDomainEventEntity> repository = unitOfWork.GetRepository<TestDomainEventEntity>();

        TestDomainEventEntity entity = CreateEntityWithEvent("PendingSync");
        repository.Add(entity);

        IEnumerable<IHasDomainEvents> entitiesWithEvents = unitOfWork.GetEntitiesWithEvents();

        entitiesWithEvents.Should().ContainSingle(e => ReferenceEquals(e, entity));
        entity.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public void SaveChangesWithEvents_ShouldDispatchEventsAndClearEntityEvents()
    {
        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWorkWithEvents unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEvents>();
        IRepository<TestDomainEventEntity> repository = unitOfWork.GetRepository<TestDomainEventEntity>();

        TestDomainEventEntity entity = CreateEntityWithEvent("DispatchedSync");
        repository.Add(entity);

        int rowsAffected = unitOfWork.SaveChangesWithEvents();

        rowsAffected.Should().BeGreaterThan(0);
        _dispatchedEvents.Should().ContainSingle();
        _dispatchedEvents.Single().Should().BeOfType<TestDomainEvent>()
            .Which.Message.Should().Be("DispatchedSync");
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void SaveChanges_ShouldPersistWithoutDispatchingEvents()
    {
        _dispatchedEvents.Clear();

        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWorkWithEvents unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEvents>();
        IRepository<TestDomainEventEntity> repository = unitOfWork.GetRepository<TestDomainEventEntity>();

        TestDomainEventEntity entity = CreateEntityWithEvent("NotDispatchedSync");
        repository.Add(entity);
        unitOfWork.SaveChanges();

        _dispatchedEvents.Should().BeEmpty();
        entity.DomainEvents.Should().HaveCount(1);
    }

    private static TestDomainEventEntity CreateEntityWithEvent(string message)
    {
        var entity = new TestDomainEventEntity { Name = "EventEntity" };
        entity.Raise(new TestDomainEvent(message));
        return entity;
    }
}
