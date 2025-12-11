# Domain Events

## Overview

Domain Events are events that represent something significant that happened in the domain. They are raised by aggregates/entities and processed synchronously within the same transaction.

## Interfaces

### IDomainEvent

```csharp
public interface IDomainEvent : IMediatorNotification
{
}
```

### IDomainEventHandler

```csharp
public interface IDomainEventHandler<in TEvent> : IMediatorNotificationHandler<TEvent>
    where TEvent : IDomainEvent
{
}
```

### IHasDomainEvents

Interface for entities that raise events:

```csharp
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent eventItem);
    void RemoveDomainEvent(IDomainEvent eventItem);
    void ClearDomainEvents();
}
```

## Creating Domain Events

### Defining the Event

```csharp
public record OrderCreatedDomainEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderCancelledDomainEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

### Using Base Record

```csharp
public abstract record DomainEventBase : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderCreatedDomainEvent : DomainEventBase
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
}
```

## Implementing in Entities

### Entity with Domain Events

```csharp
public class Order : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public Guid Id { get; private set; }
    public string CustomerEmail { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Private constructor for EF Core
    private Order() { }

    // Factory method
    public static Order Create(string customerEmail, IEnumerable<OrderItem> items)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerEmail = customerEmail,
            Status = OrderStatus.Pending,
            TotalAmount = items.Sum(i => i.TotalPrice)
        };

        // Raise creation event
        order.AddDomainEvent(new OrderCreatedDomainEvent
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail
        });

        return order;
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Cancelled)
            throw new DomainException("Order is already cancelled");

        Status = OrderStatus.Cancelled;

        // Raise cancellation event
        AddDomainEvent(new OrderCancelledDomainEvent
        {
            OrderId = Id,
            Reason = reason
        });
    }

    public void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void RemoveDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Remove(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

## Creating Handlers

```csharp
public class OrderCreatedDomainEventHandler 
    : IDomainEventHandler<OrderCreatedDomainEvent>
{
    private readonly ILogger<OrderCreatedDomainEventHandler> _logger;
    private readonly IEmailService _emailService;

    public OrderCreatedDomainEventHandler(
        ILogger<OrderCreatedDomainEventHandler> logger,
        IEmailService emailService)
    {
        _logger = logger;
        _emailService = emailService;
    }

    public async Task Handle(
        OrderCreatedDomainEvent notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Order {OrderId} created. Sending confirmation email...", 
            notification.OrderId);

        await _emailService.SendOrderConfirmationAsync(
            notification.CustomerEmail,
            notification.OrderId);
    }
}
```

## Dispatching Domain Events

### Using DomainEventDispatcher

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IDomainEventDispatcher _dispatcher;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        // Create order (raises events internally)
        var order = Order.Create(request.CustomerEmail, request.Items);
        
        await _repository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Dispatch events after saving
        await _dispatcher.DispatchAsync(order.DomainEvents, cancellationToken);
        order.ClearDomainEvents();

        return new OrderDto { Id = order.Id };
    }
}
```

### Using Extension Method

```csharp
// Extension for IUnitOfWork
await _unitOfWork.SaveChangesWithEventsAsync(_dispatcher, cancellationToken);
```

## Domain Events vs Notifications

| Aspect | Domain Event | Notification |
|--------|-------------|--------------|
| Origin | Domain entity | Anywhere |
| Timing | During domain operation | After operation |
| Transaction | Inside transaction | Can be outside |
| Semantics | "Something happened in domain" | "Notify interested parties" |

## Domain Events vs Integration Events

| Aspect | Domain Event | Integration Event |
|--------|-------------|-------------------|
| Scope | Bounded Context | Between Bounded Contexts |
| Transport | In-process (memory) | Message Broker |
| Guarantee | Synchronous | Asynchronous (eventual) |
| Failure | Transaction rollback | Retry/Dead Letter |

## Best Practices

1. **Immutability**: Domain Events should be immutable
2. **Naming**: Use past tense (OrderCreated, PaymentProcessed)
3. **Consistency**: Events should be in the same transaction
4. **Idempotency**: Handlers should be idempotent
5. **Auditing**: Events are great for audit trail

