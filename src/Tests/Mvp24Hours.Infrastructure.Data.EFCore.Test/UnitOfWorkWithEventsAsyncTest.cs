using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Test.Support;
using Mvp24Hours.Infrastructure.Data.EFCore.Testing;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Test;

[Trait("Category", "Unit")]
public class UnitOfWorkWithEventsAsyncTest : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly List<IDomainEvent> _dispatchedEvents = [];
    private readonly string _databaseName = $"UowEventsAsync_{Guid.NewGuid():N}";

    public UnitOfWorkWithEventsAsyncTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvp24HoursInMemoryDbContext<TestDbContext>(_databaseName);
        services.AddMvp24HoursRepositoryWithEvents(o => o.MaxQtyByQueryPage = 100);
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
    public async Task GetEntitiesWithEvents_ShouldReturnEntitiesWithPendingEvents()
    {
        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWorkWithEventsAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEventsAsync>();
        IRepositoryAsync<TestDomainEventEntity> repository = unitOfWork.GetRepository<TestDomainEventEntity>();

        TestDomainEventEntity entity = CreateEntityWithEvent("Pending");
        await repository.AddAsync(entity);

        IEnumerable<IHasDomainEvents> entitiesWithEvents = unitOfWork.GetEntitiesWithEvents();

        entitiesWithEvents.Should().ContainSingle(e => ReferenceEquals(e, entity));
        entity.DomainEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveChangesWithEventsAsync_ShouldDispatchEventsAndClearEntityEvents()
    {
        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWorkWithEventsAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEventsAsync>();
        IRepositoryAsync<TestDomainEventEntity> repository = unitOfWork.GetRepository<TestDomainEventEntity>();

        TestDomainEventEntity entity = CreateEntityWithEvent("Dispatched");
        await repository.AddAsync(entity);

        int rowsAffected = await unitOfWork.SaveChangesWithEventsAsync();

        rowsAffected.Should().BeGreaterThan(0);
        _dispatchedEvents.Should().ContainSingle();
        _dispatchedEvents.Single().Should().BeOfType<TestDomainEvent>()
            .Which.Message.Should().Be("Dispatched");
        entity.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldPersistWithoutDispatchingEvents()
    {
        _dispatchedEvents.Clear();

        using IServiceScope scope = _provider.CreateScope();
        IUnitOfWorkWithEventsAsync unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWorkWithEventsAsync>();
        IRepositoryAsync<TestDomainEventEntity> repository = unitOfWork.GetRepository<TestDomainEventEntity>();

        TestDomainEventEntity entity = CreateEntityWithEvent("NotDispatched");
        await repository.AddAsync(entity);
        await unitOfWork.SaveChangesAsync();

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
