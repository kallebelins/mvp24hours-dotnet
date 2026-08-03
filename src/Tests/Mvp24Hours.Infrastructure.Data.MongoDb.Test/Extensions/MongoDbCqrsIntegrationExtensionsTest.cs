using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Cqrs;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;
using CoreHasDomainEvents = Mvp24Hours.Core.Contract.Domain.Entity.IHasDomainEvents;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Extensions;

[Trait("Category", "Unit")]
public class MongoDbCqrsIntegrationExtensionsTest
{
    [Fact]
    public void AddMvp24HoursMongoDbNoOpEventDispatcher_ShouldRegisterNoOpDispatcher()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursMongoDbNoOpEventDispatcher();

        ServiceDescriptor descriptor = services.Should().ContainSingle(d => d.ServiceType == typeof(IDomainEventDispatcherMongoDb)).Subject;
        descriptor.ImplementationType.Should().Be(typeof(NoOpDomainEventDispatcher));
    }

    [Fact]
    public void AddMvp24HoursMongoDbEventDispatcher_WithNullFactory_ShouldThrowArgumentNullException()
    {
        var services = new ServiceCollection();

        Action act = () => services.AddMvp24HoursMongoDbEventDispatcher(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddMvp24HoursMongoDbEventDispatcher_ShouldInvokeCustomDispatchFunction()
    {
        var services = new ServiceCollection();
        bool dispatched = false;

        services.AddMvp24HoursMongoDbEventDispatcher(_ => (events, ct) =>
        {
            dispatched = true;
            return Task.CompletedTask;
        });

        IDomainEventDispatcherMongoDb dispatcher = services.BuildServiceProvider()
            .GetRequiredService<IDomainEventDispatcherMongoDb>();

        var entity = new TestEntityWithEvents();
        entity.Raise(new TestDomainEvent());
        dispatcher.DispatchEvents(entity, CancellationToken.None);

        dispatched.Should().BeTrue();
    }

    [Fact]
    public void AddMvp24HoursMongoDbCqrsIntegration_WithoutCqrsDispatcher_ShouldRegisterNoOp()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursMongoDbCqrsIntegration();

        IDomainEventDispatcherMongoDb dispatcher = services.BuildServiceProvider()
            .GetRequiredService<IDomainEventDispatcherMongoDb>();

        dispatcher.Should().BeOfType<NoOpDomainEventDispatcher>();
    }

    [Fact]
    public async Task AddMvp24HoursMongoDbCqrsIntegration_WithCqrsDispatcher_ShouldBridgeDispatchCalls()
    {
        var services = new ServiceCollection();
        var cqrsDispatcher = new Mock<IDomainEventDispatcher>();
        cqrsDispatcher
            .Setup(d => d.DispatchEventsAsync(It.IsAny<CoreHasDomainEvents>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(cqrsDispatcher.Object);
        services.AddMvp24HoursMongoDbCqrsIntegration();

        IDomainEventDispatcherMongoDb dispatcher = services.BuildServiceProvider()
            .GetRequiredService<IDomainEventDispatcherMongoDb>();

        var entity = new TestEntityWithEvents();
        await dispatcher.DispatchEventsAsync(entity);

        cqrsDispatcher.Verify(
            d => d.DispatchEventsAsync(entity, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void AddMvp24HoursRepositoryWithEvents_ShouldRegisterUnitOfWorkAndRepository()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRepositoryWithEvents();

        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWorkWithEvents));
        services.Should().Contain(d => d.ServiceType == typeof(IRepository<>));
    }

    [Fact]
    public void AddMvp24HoursRepositoryAsyncWithEvents_ShouldRegisterAsyncUnitOfWorkAndRepository()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRepositoryAsyncWithEvents();

        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWorkAsync));
        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWorkWithEventsAsync));
        services.Should().Contain(d => d.ServiceType == typeof(IRepositoryAsync<>));
    }

    [Fact]
    public void AddMvp24HoursRepositoriesWithEvents_ShouldRegisterSyncAndAsyncServices()
    {
        var services = new ServiceCollection();

        services.AddMvp24HoursRepositoriesWithEvents();

        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));
        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWorkAsync));
    }

    [Fact]
    public void AddMvp24HoursMongoDbReadWriteSeparation_ShouldConfigureOptionsAndRepositories()
    {
        var services = new ServiceCollection();
        MongoDbCqrsIntegrationExtensions.MongoDbReadWriteSeparationOptions? captured = null;

        services.AddMvp24HoursMongoDbReadWriteSeparation(options =>
        {
            options.ReadPreference = "secondary";
            options.UseSeparateConnection = true;
            options.ReadConnectionString = "mongodb://read:27017";
            captured = options;
        });

        captured!.ReadPreference.Should().Be("secondary");
        captured.UseSeparateConnection.Should().BeTrue();
        services.Should().Contain(d => d.ServiceType == typeof(IRepositoryAsync<>));
    }

    [Fact]
    public void AddMvp24HoursMongoDbOutboxIntegration_WithoutOutbox_ShouldRegisterNoOp()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursMongoDbOutboxIntegration();

        IDomainEventDispatcherMongoDb dispatcher = services.BuildServiceProvider()
            .GetRequiredService<IDomainEventDispatcherMongoDb>();

        dispatcher.Should().BeOfType<NoOpDomainEventDispatcher>();
    }

    [Fact]
    public async Task AddMvp24HoursMongoDbOutboxIntegration_WithOutbox_ShouldAddEventsToOutbox()
    {
        var services = new ServiceCollection();
        var outbox = new Mock<IIntegrationEventOutbox>();
        outbox.Setup(o => o.AddAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(outbox.Object);
        services.AddMvp24HoursMongoDbOutboxIntegration();

        IDomainEventDispatcherMongoDb dispatcher = services.BuildServiceProvider()
            .GetRequiredService<IDomainEventDispatcherMongoDb>();

        var entity = new TestEntityWithEvents();
        entity.Raise(new TestDomainEvent());
        await dispatcher.DispatchEventsAsync(entity);

        outbox.Verify(o => o.AddAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AddMvp24HoursMongoDbFullCqrsSetup_ShouldRegisterAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddMvp24HoursMongoDbFullCqrsSetup(useOutbox: false);

        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWorkAsync));
        services.Should().Contain(d => d.ServiceType == typeof(IReadOnlyRepositoryAsync<>));
        services.Should().Contain(d => d.ServiceType == typeof(IDomainEventDispatcherMongoDb));
    }

    [Fact]
    public void AddMvp24HoursMongoDbFullCqrsSetup_WithOutbox_ShouldRegisterOutboxDispatcher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var outbox = new Mock<IIntegrationEventOutbox>();
        services.AddSingleton(outbox.Object);

        services.AddMvp24HoursMongoDbFullCqrsSetup(useOutbox: true);

        services.BuildServiceProvider().GetRequiredService<IDomainEventDispatcherMongoDb>()
            .Should().NotBeOfType<NoOpDomainEventDispatcher>();
    }

    private sealed class TestEntityWithEvents : CoreHasDomainEvents
    {
        private readonly List<CoreDomainEvent> _events = [];

        public IReadOnlyCollection<CoreDomainEvent> DomainEvents => _events.AsReadOnly();

        public void ClearDomainEvents()
        {
            _events.Clear();
        }

        public void Raise(CoreDomainEvent domainEvent)
        {
            _events.Add(domainEvent);
        }
    }

    private sealed class TestDomainEvent : CoreDomainEvent
    {
        public DateTime OccurredAt { get; } = DateTime.UtcNow;

        public Guid EventId { get; } = Guid.NewGuid();
    }
}
