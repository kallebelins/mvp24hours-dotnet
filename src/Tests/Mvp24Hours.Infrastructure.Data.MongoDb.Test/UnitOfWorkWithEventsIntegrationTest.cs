using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Cqrs;
using Mvp24Hours.Infrastructure.Data.MongoDb.Test.Support;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test;

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class UnitOfWorkWithEventsIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public void TrackEntity_And_GetEntitiesWithEvents_ShouldReturnTrackedEntities()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEvents(context, new Dictionary<Type, object>());
        DomainEventEntity entity = CreateEntityWithEvent("Tracked");

        unitOfWork.TrackEntity(entity);

        unitOfWork.GetEntitiesWithEvents().Should().ContainSingle(e => ReferenceEquals(e, entity));
        unitOfWork.TrackedEntitiesCount.Should().Be(1);
    }

    [DockerFact]
    public void SaveChangesWithEvents_ShouldDispatchEventsAndClearTracking()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        var dispatched = new List<IDomainEvent>();
        var dispatcherMock = new Mock<IDomainEventDispatcherMongoDb>();
        dispatcherMock
            .Setup(d => d.DispatchEvents(It.IsAny<IEnumerable<IHasDomainEvents>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IHasDomainEvents>, CancellationToken>((entities, _) =>
            {
                dispatched.AddRange(entities.SelectMany(e => e.DomainEvents));
                foreach (IHasDomainEvents entity in entities)
                {
                    entity.ClearDomainEvents();
                }
            });

        using var unitOfWork = new UnitOfWorkWithEvents(
            context,
            new Dictionary<Type, object>(),
            dispatcherMock.Object,
            NullLogger<UnitOfWorkWithEvents>.Instance);
        DomainEventEntity entity = CreateEntityWithEvent("Dispatched");
        unitOfWork.TrackEntity(entity);

        int result = unitOfWork.SaveChangesWithEvents();

        result.Should().Be(1);
        dispatched.Should().ContainSingle();
        dispatched.Single().Should().BeOfType<MongoTestDomainEvent>()
            .Which.Message.Should().Be("Dispatched");
        unitOfWork.TrackedEntitiesCount.Should().Be(0);
        entity.DomainEvents.Should().BeEmpty();
    }

    [DockerFact]
    public void SaveChangesWithEvents_WithoutDispatcher_ShouldClearEventsAndLogWarning()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEvents(
            context,
            new Dictionary<Type, object>(),
            eventDispatcher: null,
            NullLogger<UnitOfWorkWithEvents>.Instance);
        DomainEventEntity entity = CreateEntityWithEvent("NoDispatcher");
        unitOfWork.TrackEntity(entity);

        int result = unitOfWork.SaveChangesWithEvents();

        result.Should().Be(1);
        entity.DomainEvents.Should().BeEmpty();
        unitOfWork.TrackedEntitiesCount.Should().Be(0);
    }

    [DockerFact]
    public void SaveChangesWithEvents_WhenCancelled_ShouldRollbackAndReturnZero()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEvents(context, new Dictionary<Type, object>());
        unitOfWork.TrackEntity(CreateEntityWithEvent("Cancelled"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int result = unitOfWork.SaveChangesWithEvents(cts.Token);

        result.Should().Be(0);
        unitOfWork.TrackedEntitiesCount.Should().Be(0);
    }

    [DockerFact]
    public void Rollback_ShouldClearTrackedEntities()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEvents(context, new Dictionary<Type, object>());
        unitOfWork.TrackEntities([CreateEntityWithEvent("One"), CreateEntityWithEvent("Two")]);

        unitOfWork.Rollback();

        unitOfWork.TrackedEntitiesCount.Should().Be(0);
    }

    [DockerFact]
    public void UntrackEntity_ShouldRemoveTrackedEntity()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEvents(context, new Dictionary<Type, object>());
        DomainEventEntity entity = CreateEntityWithEvent("Untrack");
        unitOfWork.TrackEntity(entity);

        unitOfWork.UntrackEntity(entity);

        unitOfWork.GetEntitiesWithEvents().Should().BeEmpty();
    }

    [DockerFact]
    public void GetRepository_WithServiceProvider_ShouldResolveRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(MongoDbIntegrationTestHelper.CreateRepositoryOptions());
        services.AddSingleton<IRepository<TestEntity>, Repository<TestEntity>>();
        ServiceProvider provider = services.BuildServiceProvider();

        using var unitOfWork = new UnitOfWorkWithEvents(
            context,
            provider,
            eventDispatcher: null,
            NullLogger<UnitOfWorkWithEvents>.Instance);

        IRepository<TestEntity> repository = unitOfWork.GetRepository<TestEntity>();

        repository.Should().NotBeNull();
    }

    [DockerFact]
    public void TrackEntity_WithNonDomainEntity_ShouldNotTrack()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEvents(context, new Dictionary<Type, object>());

        unitOfWork.TrackEntity(new TestEntity { Name = "Plain" });

        unitOfWork.TrackedEntitiesCount.Should().Be(0);
    }

    private static DomainEventEntity CreateEntityWithEvent(string message)
    {
        var entity = new DomainEventEntity { Name = "EventEntity" };
        entity.Raise(new MongoTestDomainEvent(message));
        return entity;
    }
}

[Trait("Category", "Integration")]
[Collection(MongoDbIntegrationCollection.Name)]
public class UnitOfWorkWithEventsAsyncIntegrationTest(MongoDbIntegrationFixture fixture)
{
    [DockerFact]
    public async Task SaveChangesWithEventsAsync_ShouldDispatchEventsAndClearTracking()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        var dispatched = new List<IDomainEvent>();
        var dispatcherMock = new Mock<IDomainEventDispatcherMongoDb>();
        dispatcherMock
            .Setup(d => d.DispatchEventsAsync(It.IsAny<IEnumerable<IHasDomainEvents>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<IHasDomainEvents>, CancellationToken>((entities, _) =>
            {
                dispatched.AddRange(entities.SelectMany(e => e.DomainEvents));
                foreach (IHasDomainEvents entity in entities)
                {
                    entity.ClearDomainEvents();
                }
            })
            .Returns(Task.CompletedTask);

        using var unitOfWork = new UnitOfWorkWithEventsAsync(
            context,
            new Dictionary<Type, object>(),
            dispatcherMock.Object,
            NullLogger<UnitOfWorkWithEventsAsync>.Instance);
        DomainEventEntity entity = CreateEntityWithEvent("AsyncDispatched");
        unitOfWork.TrackEntity(entity);

        int result = await unitOfWork.SaveChangesWithEventsAsync();

        result.Should().Be(1);
        dispatched.Should().ContainSingle();
        dispatched.Single().Should().BeOfType<MongoTestDomainEvent>()
            .Which.Message.Should().Be("AsyncDispatched");
        unitOfWork.TrackedEntitiesCount.Should().Be(0);
    }

    [DockerFact]
    public async Task SaveChangesWithEventsAsync_WithoutDispatcher_ShouldClearEvents()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEventsAsync(context, new Dictionary<Type, object>());
        DomainEventEntity entity = CreateEntityWithEvent("AsyncNoDispatcher");
        unitOfWork.TrackEntity(entity);

        int result = await unitOfWork.SaveChangesWithEventsAsync();

        result.Should().Be(1);
        entity.DomainEvents.Should().BeEmpty();
    }

    [DockerFact]
    public async Task SaveChangesWithEventsAsync_WhenCancelled_ShouldRollbackAndReturnZero()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEventsAsync(context, new Dictionary<Type, object>());
        unitOfWork.TrackEntity(CreateEntityWithEvent("AsyncCancelled"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        int result = await unitOfWork.SaveChangesWithEventsAsync(cts.Token);

        result.Should().Be(0);
        unitOfWork.TrackedEntitiesCount.Should().Be(0);
    }

    [DockerFact]
    public async Task SaveChangesAsync_ShouldReturnOneOnSuccess()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEventsAsync(context, new Dictionary<Type, object>());

        int result = await unitOfWork.SaveChangesAsync();

        result.Should().Be(1);
    }

    [DockerFact]
    public async Task RollbackAsync_ShouldClearTrackedEntities()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        using var unitOfWork = new UnitOfWorkWithEventsAsync(context, new Dictionary<Type, object>());
        unitOfWork.TrackEntity(CreateEntityWithEvent("AsyncRollback"));

        await unitOfWork.RollbackAsync();

        unitOfWork.TrackedEntitiesCount.Should().Be(0);
    }

    [DockerFact]
    public async Task GetRepositoryAsync_WithServiceProvider_ShouldResolveRepository()
    {
        Mvp24HoursContext context = MongoDbIntegrationTestHelper.CreateContext(fixture);
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(MongoDbIntegrationTestHelper.CreateRepositoryOptions());
        services.AddSingleton<IRepositoryAsync<TestEntity>, RepositoryAsync<TestEntity>>();
        ServiceProvider provider = services.BuildServiceProvider();

        using var unitOfWork = new UnitOfWorkWithEventsAsync(
            context,
            provider,
            eventDispatcher: null,
            NullLogger<UnitOfWorkWithEventsAsync>.Instance);

        IRepositoryAsync<TestEntity> repository = unitOfWork.GetRepository<TestEntity>();

        repository.Should().NotBeNull();
        await Task.CompletedTask;
    }

    private static DomainEventEntity CreateEntityWithEvent(string message)
    {
        var entity = new DomainEventEntity { Name = "EventEntity" };
        entity.Raise(new MongoTestDomainEvent(message));
        return entity;
    }
}

internal sealed class DomainEventEntity : IEntityBase, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public ObjectId Id { get; set; } = ObjectId.GenerateNewId();

    public string Name { get; set; } = string.Empty;

    public object EntityKey => Id;

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyCollection<MessageResult> GetNotifications()
    {
        return [];
    }

    public bool HasNotifications()
    {
        return false;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void Raise(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}

internal sealed class MongoTestDomainEvent(string message) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public string Message { get; } = message;
}
