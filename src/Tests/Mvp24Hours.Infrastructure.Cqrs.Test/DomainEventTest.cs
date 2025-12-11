//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

/// <summary>
/// Unit tests for Domain Events.
/// </summary>
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Name)]
public class DomainEventTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDomainEventDispatcher _dispatcher;

    public DomainEventTest()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMvpMediator(typeof(UserRegisteredEvent).Assembly);
        _serviceProvider = services.BuildServiceProvider();
        _dispatcher = _serviceProvider.GetRequiredService<IDomainEventDispatcher>();

        // Clear static handlers
        UserRegisteredEventHandler.HandledEvents.Clear();
        WelcomeEmailHandler.HandledEvents.Clear();
        OrderPlacedEventHandler.HandledEvents.Clear();
    }

    [Fact, Priority(1)]
    public void DomainEventBase_ShouldHaveDefaultOccurredAt()
    {
        // Arrange & Act
        var @event = new UserRegisteredEvent { UserId = 1, Email = "test@example.com" };

        // Assert
        Assert.True(@event.OccurredAt <= DateTime.UtcNow);
        Assert.True(@event.OccurredAt > DateTime.UtcNow.AddMinutes(-1));
        Assert.NotEqual(Guid.Empty, @event.EventId);
    }

    [Fact, Priority(2)]
    public async Task DispatchEventsAsync_ShouldDispatchAllEventsFromEntity()
    {
        // Arrange
        var aggregate = new TestAggregate { Id = 1 };
        aggregate.Register("john@example.com");

        // Act
        await _dispatcher.DispatchEventsAsync(aggregate);

        // Assert
        Assert.Single(UserRegisteredEventHandler.HandledEvents);
        Assert.Single(WelcomeEmailHandler.HandledEvents);
        Assert.Contains("john@example.com", UserRegisteredEventHandler.HandledEvents[0]);
    }

    [Fact, Priority(3)]
    public async Task DispatchEventsAsync_ShouldClearEventsAfterDispatch()
    {
        // Arrange
        var aggregate = new TestAggregate { Id = 2 };
        aggregate.Register("jane@example.com");

        // Act
        await _dispatcher.DispatchEventsAsync(aggregate);

        // Assert
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact, Priority(4)]
    public async Task DispatchEventsAsync_WithMultipleEvents_ShouldDispatchAll()
    {
        // Arrange - Aggregate with multiple events
        var aggregate = new TestAggregate { Id = 400 };
        aggregate.Register("multitest@example.com");
        aggregate.PlaceOrder(500.00m);

        // Before dispatch - aggregate should have 2 events
        Assert.Equal(2, aggregate.DomainEvents.Count);

        // Act
        await _dispatcher.DispatchEventsAsync(aggregate);

        // Assert - Events should be cleared after dispatch
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact, Priority(5)]
    public async Task DispatchEventsAsync_WithNoEvents_ShouldNotThrow()
    {
        // Arrange
        var aggregate = new TestAggregate { Id = 4 };

        // Act & Assert - Should not throw
        await _dispatcher.DispatchEventsAsync(aggregate);
    }

    [Fact, Priority(6)]
    public async Task DispatchEventsAsync_WithMultipleEntities_ShouldDispatchFromAll()
    {
        // Arrange
        UserRegisteredEventHandler.HandledEvents.Clear();
        WelcomeEmailHandler.HandledEvents.Clear();

        var entities = new List<IHasDomainEvents>
        {
            CreateAggregateWithRegistration(1, "user1@example.com"),
            CreateAggregateWithRegistration(2, "user2@example.com"),
            CreateAggregateWithRegistration(3, "user3@example.com")
        };

        // Act
        await _dispatcher.DispatchEventsAsync(entities);

        // Assert
        Assert.Equal(3, UserRegisteredEventHandler.HandledEvents.Count);
        Assert.Equal(3, WelcomeEmailHandler.HandledEvents.Count);
    }

    [Fact, Priority(7)]
    public async Task DispatchEventsAsync_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _dispatcher.DispatchEventsAsync((IHasDomainEvents)null!));
    }

    [Fact, Priority(8)]
    public async Task DispatchEventsAsync_WithEmptyEnumerable_ShouldNotThrow()
    {
        // Arrange
        var entities = new List<IHasDomainEvents>();

        // Act & Assert - Should not throw
        await _dispatcher.DispatchEventsAsync(entities);
    }

    [Fact, Priority(9)]
    public void IDomainEventHandler_ShouldInheritFromIMediatorNotificationHandler()
    {
        // Assert - Type check
        Assert.True(typeof(IDomainEventHandler<UserRegisteredEvent>)
            .IsAssignableTo(typeof(IMediatorNotificationHandler<UserRegisteredEvent>)));
    }

    [Fact, Priority(10)]
    public void IDomainEvent_ShouldInheritFromIMediatorNotification()
    {
        // Assert
        Assert.True(typeof(IDomainEvent).IsAssignableTo(typeof(IMediatorNotification)));
    }

    private static TestAggregate CreateAggregateWithRegistration(int id, string email)
    {
        var aggregate = new TestAggregate { Id = id };
        aggregate.Register(email);
        return aggregate;
    }
}

