//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Microsoft.Extensions.DependencyInjection;
using Mvp24Hours.Infrastructure.Cqrs.EventSourcing;
using Mvp24Hours.Infrastructure.Cqrs.Test.Support;
using CoreDomainEvent = Mvp24Hours.Core.Contract.Domain.Entity.IDomainEvent;

namespace Mvp24Hours.Infrastructure.Cqrs.Test.EventSourcing;

[Trait("Category", "Unit")]
public class EventSourcingAdvancedTest
{
    [Fact]
    public void DefaultEventTypeResolver_ShouldResolveAssemblyQualifiedName()
    {
        var resolver = new DefaultEventTypeResolver();
        string typeName = resolver.GetTypeName(typeof(OrderCreatedEvent));

        Type? resolved = resolver.Resolve(typeName);

        Assert.Equal(typeof(OrderCreatedEvent), resolved);
        Assert.Contains("OrderCreatedEvent", typeName);
    }

    [Fact]
    public void CompositeSnapshotStrategy_ShouldCombineStrategiesWithOrLogic()
    {
        var order = TestOrder.Create("test@example.com");
        var composite = new CompositeSnapshotStrategy(
        [
            new EventCountSnapshotStrategy(100),
            AlwaysSnapshotStrategy.Instance
        ]);

        Assert.True(composite.ShouldTakeSnapshot(order, 0));
        Assert.False(NeverSnapshotStrategy.Instance.ShouldTakeSnapshot(order, order.Version));
    }

    [Fact]
    public void EventCountSnapshotStrategy_InvalidThreshold_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EventCountSnapshotStrategy(0));
    }

    [Fact]
    public void EventSourcingOptions_ShouldHaveExpectedDefaults()
    {
        var options = new EventSourcingOptions();

        Assert.Equal(100, options.SnapshotThreshold);
        Assert.True(options.EnableSnapshots);
        Assert.True(options.UseJsonSerialization);
    }

    [Fact]
    public void AddEventSourcingInMemory_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddEventSourcingInMemory();
        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IEventStore>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStore>());
        Assert.NotNull(provider.GetRequiredService<ISnapshotStrategy>());
        Assert.NotNull(provider.GetRequiredService<IEventTypeResolver>());
        Assert.IsType<DefaultEventTypeResolver>(provider.GetRequiredService<IEventTypeResolver>());
    }

    [Fact]
    public void AddEventStoreRepository_ShouldRegisterRepository()
    {
        var services = new ServiceCollection();
        services.AddEventSourcingInMemory();
        services.AddEventStoreRepository<TestOrder>();
        ServiceProvider provider = services.BuildServiceProvider();

        IEventStoreRepository<TestOrder> repository = provider.GetRequiredService<IEventStoreRepository<TestOrder>>();
        Assert.NotNull(repository);
    }

    [Fact]
    public void AddEventCountSnapshotStrategy_ShouldOverrideDefaultStrategy()
    {
        var services = new ServiceCollection();
        services.AddEventSourcingInMemory();
        services.AddEventCountSnapshotStrategy(25);
        ServiceProvider provider = services.BuildServiceProvider();

        ISnapshotStrategy strategy = provider.GetRequiredService<ISnapshotStrategy>();
        var order = TestOrder.Create("test@example.com");
        for (int i = 0; i < 23; i++)
        {
            order.AddItem(Guid.NewGuid(), $"P{i}", 1, 1);
        }

        Assert.False(strategy.ShouldTakeSnapshot(order, 0));
        order.AddItem(Guid.NewGuid(), "P24", 1, 1);
        Assert.True(strategy.ShouldTakeSnapshot(order, 0));
    }

    [Fact]
    public void AddNoSnapshotStrategy_ShouldNeverSnapshot()
    {
        var services = new ServiceCollection();
        services.AddEventSourcingInMemory();
        services.AddNoSnapshotStrategy();
        ServiceProvider provider = services.BuildServiceProvider();

        ISnapshotStrategy strategy = provider.GetRequiredService<ISnapshotStrategy>();
        var order = TestOrder.Create("test@example.com");

        Assert.False(strategy.ShouldTakeSnapshot(order, 0));
    }

    [Fact]
    public void AddEventTypeResolver_ShouldRegisterCustomResolver()
    {
        var services = new ServiceCollection();
        services.AddEventTypeResolver(r => r.Register<OrderCreatedEvent>("order.created"));
        ServiceProvider provider = services.BuildServiceProvider();

        IEventTypeResolver resolver = provider.GetRequiredService<IEventTypeResolver>();
        Assert.Equal(typeof(OrderCreatedEvent), resolver.Resolve("order.created"));
    }

    [Fact]
    public void SnapshotAggregateRoot_CreateAndRestoreSnapshot_ShouldPersistState()
    {
        var order = SnapshotTestOrder.Create("snapshot@test.com");
        order.AddItem(Guid.NewGuid(), "Item", 1, 25m);

        SnapshotTestOrderSnapshot snapshot = order.CreateSnapshot();
        var restored = new SnapshotTestOrder();
        restored.RestoreFromSnapshot(snapshot, order.Version);

        Assert.Equal(order.Id, restored.Id);
        Assert.Equal(order.CustomerEmail, restored.CustomerEmail);
        Assert.Equal(order.TotalAmount, restored.TotalAmount);
        Assert.True(restored.WasRestoredFromSnapshot);
    }

    [Fact]
    public void JsonEventSerializer_InvalidArguments_ShouldThrow()
    {
        var serializer = new JsonEventSerializer();

        Assert.Throws<ArgumentException>(() => serializer.Deserialize("", "{}"));
        Assert.Throws<ArgumentException>(() => serializer.Deserialize(typeof(OrderCreatedEvent).AssemblyQualifiedName!, ""));
    }

    public sealed class SnapshotTestOrderSnapshot
    {
        public Guid Id { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public sealed class SnapshotTestOrder : SnapshotAggregateRoot<SnapshotTestOrderSnapshot>
    {
        public string CustomerEmail { get; private set; } = string.Empty;
        public decimal TotalAmount { get; private set; }

        public static SnapshotTestOrder Create(string email)
        {
            var order = new SnapshotTestOrder();
            order.Raise(new OrderCreatedEvent
            {
                OrderId = Guid.NewGuid(),
                CustomerEmail = email,
                TotalAmount = 0
            });
            return order;
        }

        public void AddItem(Guid productId, string name, int qty, decimal price)
        {
            Raise(new OrderItemAddedEvent
            {
                OrderId = Id,
                ProductId = productId,
                ProductName = name,
                Quantity = qty,
                UnitPrice = price
            });
        }

        public override SnapshotTestOrderSnapshot CreateSnapshot()
        {
            return new()
            {
                Id = Id,
                CustomerEmail = CustomerEmail,
                TotalAmount = TotalAmount
            };
        }

        public override void RestoreFromSnapshot(SnapshotTestOrderSnapshot snapshot, long version)
        {
            Id = snapshot.Id;
            CustomerEmail = snapshot.CustomerEmail;
            TotalAmount = snapshot.TotalAmount;
            SetVersion(version);
        }

        protected override void Apply(CoreDomainEvent @event)
        {
            switch (@event)
            {
                case OrderCreatedEvent created:
                    Id = created.OrderId;
                    CustomerEmail = created.CustomerEmail;
                    TotalAmount = created.TotalAmount;
                    break;
                case OrderItemAddedEvent item:
                    TotalAmount += item.Quantity * item.UnitPrice;
                    break;
            }
        }
    }
}
