# Notifications

## Overview

Notifications are in-process events that can have multiple handlers. Unlike Commands and Queries (which have a single handler), notifications are published to all registered handlers.

## Interfaces

### IMediatorNotification

```csharp
public interface IMediatorNotification
{
}
```

### IMediatorNotificationHandler

```csharp
public interface IMediatorNotificationHandler<in TNotification>
    where TNotification : IMediatorNotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}
```

## Creating Notifications

```csharp
public record OrderCreatedNotification : IMediatorNotification
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record OrderCancelledNotification : IMediatorNotification
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime CancelledAt { get; init; }
}
```

## Creating Handlers

### Multiple Handlers for a Notification

```csharp
// Handler 1: Send email
public class SendOrderConfirmationEmailHandler 
    : IMediatorNotificationHandler<OrderCreatedNotification>
{
    private readonly IEmailService _emailService;

    public SendOrderConfirmationEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task Handle(
        OrderCreatedNotification notification, 
        CancellationToken cancellationToken)
    {
        await _emailService.SendAsync(new Email
        {
            To = notification.CustomerEmail,
            Subject = $"Order {notification.OrderId} Confirmed",
            Body = $"Your order of ${notification.TotalAmount} has been confirmed."
        });
    }
}

// Handler 2: Update cache
public class InvalidateOrderCacheHandler 
    : IMediatorNotificationHandler<OrderCreatedNotification>
{
    private readonly IDistributedCache _cache;

    public InvalidateOrderCacheHandler(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task Handle(
        OrderCreatedNotification notification, 
        CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync($"orders:list", cancellationToken);
    }
}

// Handler 3: Audit logging
public class AuditOrderCreatedHandler 
    : IMediatorNotificationHandler<OrderCreatedNotification>
{
    private readonly IAuditService _auditService;

    public AuditOrderCreatedHandler(IAuditService auditService)
    {
        _auditService = auditService;
    }

    public async Task Handle(
        OrderCreatedNotification notification, 
        CancellationToken cancellationToken)
    {
        await _auditService.LogAsync(new AuditEntry
        {
            EventType = "OrderCreated",
            EntityId = notification.OrderId.ToString(),
            Timestamp = notification.CreatedAt
        });
    }
}
```

## Publishing Notifications

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IPublisher _publisher;

    public CreateOrderCommandHandler(
        IOrderRepository repository,
        IUnitOfWorkAsync unitOfWork,
        IPublisher publisher)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _publisher = publisher;
    }

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        // Create order
        var order = new Order { /* ... */ };
        await _repository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publish notification
        await _publisher.PublishAsync(new OrderCreatedNotification
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount,
            CreatedAt = order.CreatedAt
        }, cancellationToken);

        return new OrderDto { Id = order.Id /* ... */ };
    }
}
```

## Publishing Strategies

### Sequential (Default)

Executes handlers one after another:

```csharp
options.DefaultNotificationStrategy = NotificationPublishingStrategy.Sequential;
```

### ParallelWhenAll

Executes all in parallel and waits for all:

```csharp
options.DefaultNotificationStrategy = NotificationPublishingStrategy.ParallelWhenAll;
```

### ParallelNoWait

Fires all in parallel without waiting:

```csharp
options.DefaultNotificationStrategy = NotificationPublishingStrategy.ParallelNoWait;
```

## Notifications vs Domain Events

| Aspect | Notification | Domain Event |
|--------|-------------|--------------|
| Interface | `IMediatorNotification` | `IDomainEvent` |
| Origin | Explicitly published | Raised by entity |
| Timing | After operation completes | Before SaveChanges |
| Transaction | Outside transaction | Inside transaction |

### Domain Event Example

```csharp
// Domain Event in entity
public class Order : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public void Cancel(string reason)
    {
        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        
        // Raise domain event
        AddDomainEvent(new OrderCancelledDomainEvent(Id, reason));
    }

    public void AddDomainEvent(IDomainEvent eventItem)
    {
        _domainEvents.Add(eventItem);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

## Best Practices

1. **Fire-and-Forget**: Use for operations that don't need to block
2. **Idempotency**: Handlers should be idempotent
3. **Error Handling**: Each handler should handle its own errors
4. **Logging**: Log success and failure of each handler
5. **Decoupling**: Handlers should not depend on each other

