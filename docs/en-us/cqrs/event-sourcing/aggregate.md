# Aggregates with Event Sourcing

## Overview

Aggregates in Event Sourcing maintain their state through event application, instead of storing state directly.

## Base Interface

### IAggregate

```csharp
public interface IAggregate
{
    Guid Id { get; }
    long Version { get; }
    IReadOnlyCollection<IDomainEvent> UncommittedEvents { get; }
    void ClearUncommittedEvents();
}
```

## Base Class

### EventSourcedAggregate

```csharp
public abstract class EventSourcedAggregate : IAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    
    public Guid Id { get; protected set; }
    public long Version { get; private set; }
    
    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => 
        _uncommittedEvents.AsReadOnly();

    // Apply event to reconstruct state
    protected abstract void Apply(IDomainEvent @event);

    // Emit new event
    protected void Raise(IDomainEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    // Reconstruct from historical events
    public void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
            Version++;
        }
    }

    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }
}
```

## Complete Example: Order

### Events

```csharp
public record OrderCreatedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderItemAddedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderPaidEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderShippedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderCancelledEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

### Order Aggregate

```csharp
public class Order : EventSourcedAggregate
{
    private readonly List<OrderItem> _items = new();
    
    public string CustomerEmail { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Private constructor for reconstruction
    private Order() { }

    // Factory method for creation
    public static Order Create(string customerEmail)
    {
        var order = new Order();
        order.Raise(new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = customerEmail,
            TotalAmount = 0
        });
        return order;
    }

    // Business methods
    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Created)
            throw new DomainException("Cannot add items to a non-pending order");

        Raise(new OrderItemAddedEvent
        {
            OrderId = Id,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
    }

    public void Pay(Guid paymentId)
    {
        if (Status != OrderStatus.Created)
            throw new DomainException("Order is not in a payable state");

        if (!_items.Any())
            throw new DomainException("Cannot pay for an empty order");

        Raise(new OrderPaidEvent
        {
            OrderId = Id,
            PaymentId = paymentId,
            Amount = TotalAmount
        });
    }

    public void Ship(string trackingNumber)
    {
        if (Status != OrderStatus.Paid)
            throw new DomainException("Order must be paid before shipping");

        Raise(new OrderShippedEvent
        {
            OrderId = Id,
            TrackingNumber = trackingNumber
        });
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped)
            throw new DomainException("Cannot cancel a shipped order");

        Raise(new OrderCancelledEvent
        {
            OrderId = Id,
            Reason = reason
        });
    }

    // Apply events to reconstruct state
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                Id = e.OrderId;
                CustomerEmail = e.CustomerEmail;
                TotalAmount = e.TotalAmount;
                Status = OrderStatus.Created;
                break;

            case OrderItemAddedEvent e:
                _items.Add(new OrderItem(e.ProductId, e.Quantity, e.UnitPrice));
                TotalAmount += e.Quantity * e.UnitPrice;
                break;

            case OrderPaidEvent:
                Status = OrderStatus.Paid;
                break;

            case OrderShippedEvent:
                Status = OrderStatus.Shipped;
                break;

            case OrderCancelledEvent:
                Status = OrderStatus.Cancelled;
                break;
        }
    }
}
```

## Event Sourced Repository

```csharp
public class EventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate>
    where TAggregate : EventSourcedAggregate, new()
{
    private readonly IEventStore _eventStore;

    public async Task<TAggregate?> GetByIdAsync(Guid id)
    {
        var events = await _eventStore.GetEventsAsync(id);
        
        if (!events.Any())
            return null;

        var aggregate = new TAggregate();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    public async Task SaveAsync(TAggregate aggregate)
    {
        var uncommittedEvents = aggregate.UncommittedEvents;
        
        if (!uncommittedEvents.Any())
            return;

        var expectedVersion = aggregate.Version - uncommittedEvents.Count;
        
        await _eventStore.AppendEventsAsync(
            aggregate.Id, 
            uncommittedEvents, 
            expectedVersion);
        
        aggregate.ClearUncommittedEvents();
    }
}
```

## Command Handler

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IEventSourcedRepository<Order> _repository;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerEmail);
        
        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
        }

        await _repository.SaveAsync(order);

        return new OrderDto
        {
            Id = order.Id,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount
        };
    }
}
```

## Best Practices

1. **Immutable Events**: Use records for events
2. **Past Tense Naming**: OrderCreated, not OrderCreate
3. **Complete Events**: Include all necessary data
4. **Validate in Aggregate**: Validate before emitting events
5. **One Aggregate per Transaction**: Avoid modifying multiple
6. **Version for Concurrency**: Use optimistic versioning

