//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System.Reflection;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;
using CoreHasDomainEvents = Mvp24Hours.Core.Contract.Domain.Entity.IHasDomainEvents;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Dispatchers;

/// <summary>
/// Phase 24.2 — DomainEventDispatcher edge cases (null guards, failure, skip empty).
/// </summary>
[Trait("Category", "Unit")]
public class DomainEventDispatcherAdvancedTest
{
    [Fact]
    public async Task DispatchEventsAsync_WithNullEntity_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dispatcher = new DomainEventDispatcher(new RecordingPublisher());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.DispatchEventsAsync((CoreHasDomainEvents)null!));
    }

    [Fact]
    public async Task DispatchEventsAsync_WithNullEntities_ShouldThrowArgumentNullException()
    {
        // Arrange
        var dispatcher = new DomainEventDispatcher(new RecordingPublisher());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            dispatcher.DispatchEventsAsync((IEnumerable<CoreHasDomainEvents>)null!));
    }

    [Fact]
    public async Task DispatchEventsAsync_Entities_ShouldSkipEntitiesWithoutEvents()
    {
        // Arrange
        var publisher = new RecordingPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var empty = new TestAggregate { Id = 1 };
        var withEvents = new TestAggregate { Id = 2 };
        withEvents.Register("skip@test.com");

        // Act
        await dispatcher.DispatchEventsAsync([empty, withEvents]);

        // Assert
        Assert.Single(publisher.Published);
        Assert.Empty(withEvents.DomainEvents);
    }

    [Fact]
    public async Task DispatchEventsAsync_WhenPublishFails_ShouldNotClearEvents()
    {
        // Arrange
        var logger = new CollectingLogger<DomainEventDispatcher>();
        var dispatcher = new DomainEventDispatcher(new ThrowingPublisher { Message = "dlq" }, logger);
        var aggregate = new TestAggregate { Id = 10 };
        aggregate.Register("fail@test.com");

        // Act — reflection Invoke wraps publisher failures in TargetInvocationException
        TargetInvocationException ex = await Assert.ThrowsAsync<TargetInvocationException>(() =>
            dispatcher.DispatchEventsAsync(aggregate));

        // Assert
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("dlq", ex.InnerException!.Message);
        Assert.Single(aggregate.DomainEvents);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Failed"));
    }

    [Fact]
    public async Task DispatchEventsAsync_Entities_ShouldProcessInOrder()
    {
        // Arrange
        var publisher = new RecordingPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var first = new TestAggregate { Id = 1 };
        first.Register("first@test.com");
        var second = new TestAggregate { Id = 2 };
        second.Register("second@test.com");

        // Act
        await dispatcher.DispatchEventsAsync([first, second]);

        // Assert
        Assert.Equal(2, publisher.Published.Count);
        Assert.Equal("first@test.com", ((UserRegisteredEvent)publisher.Published[0]).Email);
        Assert.Equal("second@test.com", ((UserRegisteredEvent)publisher.Published[1]).Email);
    }

    [Fact]
    public async Task DispatchEventsAsync_ShouldLogDebugAndInformation()
    {
        // Arrange
        var logger = new CollectingLogger<DomainEventDispatcher>();
        var dispatcher = new DomainEventDispatcher(new RecordingPublisher(), logger);
        var aggregate = new TestAggregate { Id = 3 };
        aggregate.Register("log@test.com");

        // Act
        await dispatcher.DispatchEventsAsync(aggregate);

        // Assert
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("Dispatching"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Information && e.Message.Contains("Publishing"));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Debug && e.Message.Contains("Successfully"));
    }

    [Fact]
    public async Task DispatchEventsAsync_EntitiesWithNoEvents_ShouldReturnWithoutPublishing()
    {
        // Arrange
        var publisher = new RecordingPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);

        // Act
        await dispatcher.DispatchEventsAsync([new TestAggregate { Id = 1 }, new TestAggregate { Id = 2 }]);

        // Assert
        Assert.Empty(publisher.Published);
    }

    [Fact]
    public void Constructor_WithNullPublisher_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new DomainEventDispatcher(null!));
    }

    [Fact]
    public async Task DispatchEventsAsync_FailingDispatchEvent_ShouldPropagateViaReflection()
    {
        // Arrange
        var dispatcher = new DomainEventDispatcher(new ThrowingPublisher());
        var aggregate = new FailingTestAggregate();
        aggregate.RaiseFailingEvent("reflect");

        // Act & Assert
        TargetInvocationException ex = await Assert.ThrowsAsync<TargetInvocationException>(() =>
            dispatcher.DispatchEventsAsync(aggregate));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Single(aggregate.DomainEvents);
    }
}
