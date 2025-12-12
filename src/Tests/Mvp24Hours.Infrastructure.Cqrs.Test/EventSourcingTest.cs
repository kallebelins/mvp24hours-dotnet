//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.Test;

public class EventSourcingTest
{
    #region AggregateRoot Tests

    [Fact]
    public void Aggregate_Create_ShouldHaveOneUncommittedEvent()
    {
        // Arrange & Act
        var order = TestOrder.Create("test@example.com");

        // Assert
        Assert.Single(order.UncommittedEvents);
        Assert.Equal(1, order.Version);
        Assert.Equal("test@example.com", order.CustomerEmail);
        Assert.Equal(OrderStatus.Created, order.Status);
    }

    [Fact]
    public void Aggregate_AddItem_ShouldIncrementVersionAndTotal()
    {
        // Arrange
        var order = TestOrder.Create("test@example.com");
        var productId = Guid.NewGuid();

        // Act
        order.AddItem(productId, "Product 1", 2, 10.00m);

        // Assert
        Assert.Equal(2, order.UncommittedEvents.Count);
        Assert.Equal(2, order.Version);
        Assert.Equal(20.00m, order.TotalAmount);
        Assert.Single(order.Items);
    }

    [Fact]
    public void Aggregate_Pay_ShouldChangeStatus()
    {
        // Arrange
        var order = TestOrder.Create("test@example.com");
        order.AddItem(Guid.NewGuid(), "Product", 1, 50.00m);

        // Act
        order.Pay(Guid.NewGuid());

        // Assert
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Equal(3, order.Version);
    }

    [Fact]
    public void Aggregate_Pay_WithNoItems_ShouldThrow()
    {
        // Arrange
        var order = TestOrder.Create("test@example.com");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Pay(Guid.NewGuid()));
    }

    [Fact]
    public void Aggregate_Ship_ShouldChangeStatus()
    {
        // Arrange
        var order = TestOrder.Create("test@example.com");
        order.AddItem(Guid.NewGuid(), "Product", 1, 50.00m);
        order.Pay(Guid.NewGuid());

        // Act
        order.Ship("TRACK-123");

        // Assert
        Assert.Equal(OrderStatus.Shipped, order.Status);
    }

    [Fact]
    public void Aggregate_CancelShippedOrder_ShouldThrow()
    {
        // Arrange
        var order = TestOrder.Create("test@example.com");
        order.AddItem(Guid.NewGuid(), "Product", 1, 50.00m);
        order.Pay(Guid.NewGuid());
        order.Ship("TRACK-123");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => order.Cancel("Changed mind"));
    }

    [Fact]
    public void Aggregate_LoadFromHistory_ShouldReconstructState()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var events = new List<CoreDomainEvent>
        {
            new OrderCreatedEvent
            {
                OrderId = orderId,
                CustomerEmail = "test@example.com",
                TotalAmount = 0
            },
            new OrderItemAddedEvent
            {
                OrderId = orderId,
                ProductId = productId,
                ProductName = "Product",
                Quantity = 2,
                UnitPrice = 25.00m
            },
            new OrderPaidEvent
            {
                OrderId = orderId,
                PaymentId = Guid.NewGuid(),
                Amount = 50.00m
            }
        };

        // Act
        var order = new TestOrder();
        order.LoadFromHistory(events);

        // Assert
        Assert.Equal(orderId, order.Id);
        Assert.Equal("test@example.com", order.CustomerEmail);
        Assert.Equal(50.00m, order.TotalAmount);
        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Single(order.Items);
        Assert.Equal(3, order.Version);
        Assert.Empty(order.UncommittedEvents); // History events are not uncommitted
    }

    [Fact]
    public void Aggregate_ClearUncommittedEvents_ShouldEmptyCollection()
    {
        // Arrange
        var order = TestOrder.Create("test@example.com");
        order.AddItem(Guid.NewGuid(), "Product", 1, 10.00m);

        // Act
        order.ClearUncommittedEvents();

        // Assert
        Assert.Empty(order.UncommittedEvents);
        Assert.Equal(2, order.Version); // Version remains
    }

    #endregion

    #region EventStream Tests

    [Fact]
    public void EventStream_NewStream_ShouldBeEmpty()
    {
        // Arrange & Act
        var stream = new EventStream(Guid.NewGuid(), "TestOrder");

        // Assert
        Assert.Empty(stream.Events);
        Assert.True(stream.IsEmpty);
        Assert.Equal(0, stream.Version);
    }

    [Fact]
    public void EventStream_GetEventsAfterVersion_ShouldReturnSubset()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var events = new CoreDomainEvent[]
        {
            new OrderCreatedEvent { OrderId = orderId },
            new OrderItemAddedEvent { OrderId = orderId },
            new OrderPaidEvent { OrderId = orderId }
        };
        var stream = new EventStream(orderId, "TestOrder", events, 3);

        // Act
        var eventsAfter1 = stream.GetEventsAfterVersion(1);

        // Assert
        Assert.Equal(2, eventsAfter1.Count);
    }

    #endregion

    #region InMemoryEventStore Tests

    [Fact]
    public async Task InMemoryEventStore_AppendEvents_ShouldStoreEvents()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var aggregateId = Guid.NewGuid();
        var events = new[] { new OrderCreatedEvent { OrderId = aggregateId } };

        // Act
        await store.AppendEventsAsync(aggregateId, events, 0);

        // Assert
        Assert.Equal(1, store.Count);
        var storedEvents = await store.GetEventsAsync(aggregateId);
        Assert.Single(storedEvents);
    }

    [Fact]
    public async Task InMemoryEventStore_ConcurrencyConflict_ShouldThrow()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var aggregateId = Guid.NewGuid();
        var events = new[] { new OrderCreatedEvent { OrderId = aggregateId } };
        await store.AppendEventsAsync(aggregateId, events, 0);

        // Act & Assert
        await Assert.ThrowsAsync<ConcurrencyException>(async () =>
            await store.AppendEventsAsync(aggregateId, events, 0)); // Wrong expected version
    }

    [Fact]
    public async Task InMemoryEventStore_GetCurrentVersion_ShouldReturnCorrectVersion()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var aggregateId = Guid.NewGuid();

        // Act - Initial
        var version0 = await store.GetCurrentVersionAsync(aggregateId);
        Assert.Equal(0, version0);

        // Add events
        await store.AppendEventsAsync(aggregateId, new[] { new OrderCreatedEvent { OrderId = aggregateId } }, 0);
        var version1 = await store.GetCurrentVersionAsync(aggregateId);
        Assert.Equal(1, version1);

        await store.AppendEventsAsync(aggregateId, new[] { new OrderItemAddedEvent { OrderId = aggregateId } }, 1);
        var version2 = await store.GetCurrentVersionAsync(aggregateId);
        Assert.Equal(2, version2);
    }

    [Fact]
    public async Task InMemoryEventStore_Exists_ShouldReturnTrueForExistingAggregate()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var aggregateId = Guid.NewGuid();

        // Act - Before
        var existsBefore = await store.ExistsAsync(aggregateId);
        Assert.False(existsBefore);

        // Add events
        await store.AppendEventsAsync(aggregateId, new[] { new OrderCreatedEvent { OrderId = aggregateId } }, 0);

        // Act - After
        var existsAfter = await store.ExistsAsync(aggregateId);
        Assert.True(existsAfter);
    }

    [Fact]
    public async Task InMemoryEventStore_GetEventsFromVersion_ShouldReturnEventsAfterVersion()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var aggregateId = Guid.NewGuid();
        await store.AppendEventsAsync(aggregateId, new[] { new OrderCreatedEvent { OrderId = aggregateId } }, 0);
        await store.AppendEventsAsync(aggregateId, new[] { new OrderItemAddedEvent { OrderId = aggregateId } }, 1);
        await store.AppendEventsAsync(aggregateId, new[] { new OrderPaidEvent { OrderId = aggregateId } }, 2);

        // Act
        var allEvents = await store.GetEventsAsync(aggregateId, 0);
        var eventsFromVersion2 = await store.GetEventsAsync(aggregateId, 2);

        // Assert
        Assert.Equal(3, allEvents.Count);
        Assert.Single(eventsFromVersion2);
        Assert.IsType<OrderPaidEvent>(eventsFromVersion2[0]);
    }

    #endregion

    #region EventStoreRepository Tests

    [Fact]
    public async Task Repository_Save_ShouldPersistEvents()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var repository = new EventStoreRepository<TestOrder>(store);
        var order = TestOrder.Create("test@example.com");
        order.AddItem(Guid.NewGuid(), "Product", 1, 25.00m);

        // Act
        await repository.SaveAsync(order);

        // Assert
        Assert.Empty(order.UncommittedEvents); // Should be cleared after save
        var storedEvents = await store.GetEventsAsync(order.Id);
        Assert.Equal(2, storedEvents.Count);
    }

    [Fact]
    public async Task Repository_GetById_ShouldReconstructAggregate()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var repository = new EventStoreRepository<TestOrder>(store);
        var order = TestOrder.Create("test@example.com");
        order.AddItem(Guid.NewGuid(), "Product", 2, 15.00m);
        await repository.SaveAsync(order);

        // Act
        var loadedOrder = await repository.GetByIdAsync(order.Id);

        // Assert
        Assert.NotNull(loadedOrder);
        Assert.Equal(order.Id, loadedOrder.Id);
        Assert.Equal("test@example.com", loadedOrder.CustomerEmail);
        Assert.Equal(30.00m, loadedOrder.TotalAmount);
        Assert.Single(loadedOrder.Items);
    }

    [Fact]
    public async Task Repository_GetByIdNonExisting_ShouldReturnNull()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var repository = new EventStoreRepository<TestOrder>(store);

        // Act
        var order = await repository.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(order);
    }

    [Fact]
    public async Task Repository_Exists_ShouldReturnCorrectResult()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var repository = new EventStoreRepository<TestOrder>(store);
        var order = TestOrder.Create("test@example.com");
        await repository.SaveAsync(order);

        // Act & Assert
        Assert.True(await repository.ExistsAsync(order.Id));
        Assert.False(await repository.ExistsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Repository_SaveMultipleTimes_ShouldMaintainCorrectVersion()
    {
        // Arrange
        var store = new InMemoryEventStore();
        var repository = new EventStoreRepository<TestOrder>(store);

        // Create and save
        var order = TestOrder.Create("test@example.com");
        await repository.SaveAsync(order);

        // Reload, modify, and save again
        var loadedOrder = await repository.GetByIdAsync(order.Id);
        loadedOrder!.AddItem(Guid.NewGuid(), "Product 1", 1, 10.00m);
        await repository.SaveAsync(loadedOrder);

        // Reload again
        var reloadedOrder = await repository.GetByIdAsync(order.Id);

        // Assert
        Assert.Equal(2, reloadedOrder!.Version);
        Assert.Equal(10.00m, reloadedOrder.TotalAmount);
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public async Task SnapshotStore_Save_ShouldPersistSnapshot()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();
        var snapshot = new Snapshot
        {
            AggregateId = aggregateId,
            AggregateType = "TestOrder",
            Version = 10,
            Data = "{}",
            SnapshotType = typeof(OrderSnapshot).AssemblyQualifiedName!
        };

        // Act
        await store.SaveSnapshotAsync(snapshot);

        // Assert
        var loaded = await store.GetLatestSnapshotAsync(aggregateId);
        Assert.NotNull(loaded);
        Assert.Equal(10, loaded.Version);
    }

    [Fact]
    public async Task SnapshotStore_GetAtVersion_ShouldReturnCorrectSnapshot()
    {
        // Arrange
        var store = new InMemorySnapshotStore();
        var aggregateId = Guid.NewGuid();

        await store.SaveSnapshotAsync(new Snapshot
        {
            AggregateId = aggregateId,
            AggregateType = "TestOrder",
            Version = 5,
            Data = "{\"version\":5}",
            SnapshotType = typeof(OrderSnapshot).AssemblyQualifiedName!
        });

        await store.SaveSnapshotAsync(new Snapshot
        {
            AggregateId = aggregateId,
            AggregateType = "TestOrder",
            Version = 10,
            Data = "{\"version\":10}",
            SnapshotType = typeof(OrderSnapshot).AssemblyQualifiedName!
        });

        // Act
        var snapshotAt7 = await store.GetSnapshotAtVersionAsync(aggregateId, 7);
        var snapshotAt15 = await store.GetSnapshotAtVersionAsync(aggregateId, 15);

        // Assert
        Assert.NotNull(snapshotAt7);
        Assert.Equal(5, snapshotAt7.Version);
        Assert.NotNull(snapshotAt15);
        Assert.Equal(10, snapshotAt15.Version);
    }

    [Fact]
    public void EventCountSnapshotStrategy_ShouldReturnTrueAfterThreshold()
    {
        // Arrange
        var strategy = new EventCountSnapshotStrategy(5);
        var order = TestOrder.Create("test@example.com");

        // Version 1, last snapshot 0 → not yet
        Assert.False(strategy.ShouldTakeSnapshot(order, 0));

        // Simulate more events
        for (int i = 0; i < 4; i++)
        {
            order.AddItem(Guid.NewGuid(), $"Product {i}", 1, 10.00m);
        }

        // Version 5, last snapshot 0 → should take
        Assert.True(strategy.ShouldTakeSnapshot(order, 0));
    }

    [Fact]
    public void NeverSnapshotStrategy_ShouldAlwaysReturnFalse()
    {
        // Arrange
        var strategy = NeverSnapshotStrategy.Instance;
        var order = TestOrder.Create("test@example.com");
        for (int i = 0; i < 100; i++)
        {
            order.AddItem(Guid.NewGuid(), $"Product {i}", 1, 10.00m);
        }

        // Act & Assert
        Assert.False(strategy.ShouldTakeSnapshot(order, 0));
    }

    [Fact]
    public void AlwaysSnapshotStrategy_ShouldReturnTrueWhenVersionIncreased()
    {
        // Arrange
        var strategy = AlwaysSnapshotStrategy.Instance;
        var order = TestOrder.Create("test@example.com");

        // Act & Assert
        Assert.True(strategy.ShouldTakeSnapshot(order, 0));
        Assert.False(strategy.ShouldTakeSnapshot(order, 1)); // Same version
    }

    #endregion

    #region Serialization Tests

    [Fact]
    public void JsonEventSerializer_ShouldSerializeAndDeserialize()
    {
        // Arrange
        var serializer = new JsonEventSerializer();
        var original = new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = "test@example.com",
            TotalAmount = 99.99m
        };

        // Act
        var json = serializer.Serialize(original);
        var deserialized = serializer.Deserialize(
            typeof(OrderCreatedEvent).AssemblyQualifiedName!,
            json);

        // Assert
        var result = Assert.IsType<OrderCreatedEvent>(deserialized);
        Assert.Equal(original.OrderId, result.OrderId);
        Assert.Equal(original.CustomerEmail, result.CustomerEmail);
        Assert.Equal(original.TotalAmount, result.TotalAmount);
    }

    [Fact]
    public void RegistryEventTypeResolver_ShouldResolveRegisteredTypes()
    {
        // Arrange
        var resolver = new RegistryEventTypeResolver();
        resolver.Register<OrderCreatedEvent>("order.created");

        // Act
        var type = resolver.Resolve("order.created");
        var name = resolver.GetTypeName(typeof(OrderCreatedEvent));

        // Assert
        Assert.Equal(typeof(OrderCreatedEvent), type);
        Assert.Equal("order.created", name);
    }

    #endregion

    #region ConcurrencyException Tests

    [Fact]
    public void ConcurrencyException_ShouldContainDetails()
    {
        // Arrange & Act
        var aggregateId = Guid.NewGuid();
        var exception = new ConcurrencyException(aggregateId, 5, 10);

        // Assert
        Assert.Equal(aggregateId, exception.AggregateId);
        Assert.Equal(5, exception.ExpectedVersion);
        Assert.Equal(10, exception.ActualVersion);
        Assert.Contains("expected version 5", exception.Message);
        Assert.Contains("found 10", exception.Message);
    }

    #endregion
}

