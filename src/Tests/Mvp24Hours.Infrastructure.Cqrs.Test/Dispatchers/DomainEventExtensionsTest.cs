//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.Test.Support;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.Dispatchers;

/// <summary>
/// Phase 24.2 / 24.4 — DomainEventExtensions (UoW + dispatcher integration).
/// </summary>
[Trait("Category", "Unit")]
public class DomainEventExtensionsTest
{
    [Fact]
    public async Task SaveChangesWithEventsAsync_ShouldSaveThenDispatch()
    {
        // Arrange
        var uow = new MockUnitOfWorkAsync { RowsAffected = 3 };
        var publisher = new RecordingPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var entity = new TestAggregate { Id = 1 };
        entity.Register("async@test.com");

        // Act
        int rows = await uow.SaveChangesWithEventsAsync(dispatcher, entity);

        // Assert
        Assert.Equal(3, rows);
        Assert.Equal(1, uow.SaveChangesCallCount);
        Assert.Equal(["SaveChanges"], uow.OperationsLog);
        Assert.Single(publisher.Published);
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public async Task SaveChangesWithEventsAsync_WithEntities_ShouldDispatchAll()
    {
        // Arrange
        var uow = new MockUnitOfWorkAsync();
        var publisher = new RecordingPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var a = new TestAggregate { Id = 1 };
        a.Register("a@test.com");
        var b = new TestAggregate { Id = 2 };
        b.Register("b@test.com");

        // Act
        await uow.SaveChangesWithEventsAsync(dispatcher, [a, b]);

        // Assert
        Assert.Equal(2, publisher.Published.Count);
    }

    [Fact]
    public async Task SaveChangesWithEventsAsync_WhenSaveFails_ShouldNotDispatch()
    {
        // Arrange
        var uow = new MockUnitOfWorkAsync { ShouldThrowOnSave = true };
        var publisher = new RecordingPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var entity = new TestAggregate { Id = 1 };
        entity.Register("nosave@test.com");

        // Act
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            uow.SaveChangesWithEventsAsync(dispatcher, entity));

        // Assert
        Assert.Empty(publisher.Published);
        Assert.Single(entity.DomainEvents);
    }

    [Fact]
    public async Task SaveChangesWithEventsAsync_WithNullUnitOfWork_ShouldThrow()
    {
        // Arrange
        MockUnitOfWorkAsync? uow = null;
        var dispatcher = new DomainEventDispatcher(new RecordingPublisher());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            uow!.SaveChangesWithEventsAsync(dispatcher, new TestAggregate()));
    }

    [Fact]
    public async Task SaveChangesWithEventsAsync_WithNullDispatcher_ShouldThrow()
    {
        // Arrange
        var uow = new MockUnitOfWorkAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            uow.SaveChangesWithEventsAsync(null!, new TestAggregate()));
    }

    [Fact]
    public void SaveChangesWithEvents_Sync_ShouldSaveThenDispatch()
    {
        // Arrange
        var uow = new MockUnitOfWork { RowsAffected = 2 };
        var publisher = new RecordingPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var entity = new TestAggregate { Id = 5 };
        entity.Register("sync@test.com");

        // Act
        int rows = uow.SaveChangesWithEvents(dispatcher, entity);

        // Assert
        Assert.Equal(2, rows);
        Assert.Equal(1, uow.SaveChangesCallCount);
        Assert.Single(publisher.Published);
        Assert.Empty(entity.DomainEvents);
    }

    [Fact]
    public void SaveChangesWithEvents_Sync_WithEntities_ShouldDispatchAll()
    {
        // Arrange
        var uow = new MockUnitOfWork();
        var publisher = new RecordingPublisher();
        var dispatcher = new DomainEventDispatcher(publisher);
        var entity = new TestAggregate { Id = 6 };
        entity.Register("sync-multi@test.com");

        // Act
        uow.SaveChangesWithEvents(dispatcher, [entity]);

        // Assert
        Assert.Single(publisher.Published);
    }

    [Fact]
    public void SaveChangesWithEvents_Sync_WithNullUnitOfWork_ShouldThrow()
    {
        // Arrange
        MockUnitOfWork? uow = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            uow!.SaveChangesWithEvents(new DomainEventDispatcher(new RecordingPublisher()), new TestAggregate()));
    }

    [Fact]
    public void WithDomainEvents_ShouldReturnOnlyEntitiesWithPendingEvents()
    {
        // Arrange
        var withEvents = new TestAggregate { Id = 1 };
        withEvents.Register("filter@test.com");
        var without = new TestAggregate { Id = 2 };
        object[] mixed = [withEvents, without, "not-an-entity"];

        // Act
        var result = mixed.WithDomainEvents().ToList();

        // Assert
        Assert.Single(result);
        Assert.Same(withEvents, result[0]);
    }

    [Fact]
    public void WithDomainEvents_WhenNoneHaveEvents_ShouldReturnEmpty()
    {
        // Arrange
        object[] entities = [new TestAggregate { Id = 1 }, new TestAggregate { Id = 2 }];

        // Act
        var result = entities.WithDomainEvents().ToList();

        // Assert
        Assert.Empty(result);
    }
}
