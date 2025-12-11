# Integration Events

## Overview

Integration Events are events used for communication between Bounded Contexts via message brokers (RabbitMQ, Kafka, etc.). Unlike Domain Events (in-process), Integration Events cross application boundaries.

## Interfaces

### IIntegrationEvent

```csharp
public interface IIntegrationEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string? CorrelationId { get; }
}
```

### IIntegrationEventHandler

```csharp
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
```

## Creating Integration Events

### Using Base Record

```csharp
public abstract record IntegrationEventBase : IIntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
    public string? CausationId { get; init; }
    public string EventType => GetType().Name;
}
```

### Defining Events

```csharp
public record OrderCreatedIntegrationEvent : IntegrationEventBase
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public List<OrderItemDto> Items { get; init; } = new();
}

public record PaymentProcessedIntegrationEvent : IntegrationEventBase
{
    public Guid PaymentId { get; init; }
    public Guid OrderId { get; init; }
    public decimal Amount { get; init; }
    public PaymentStatus Status { get; init; }
}
```

## Outbox Pattern

The Outbox Pattern ensures reliable event delivery.

### IIntegrationEventOutbox Interface

```csharp
public interface IIntegrationEventOutbox
{
    Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
    
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int batchSize = 100, 
        CancellationToken cancellationToken = default);
    
    Task MarkAsPublishedAsync(Guid messageId, CancellationToken cancellationToken = default);
    
    Task MarkAsFailedAsync(Guid messageId, string error, CancellationToken cancellationToken = default);
}
```

### Using the Outbox

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IIntegrationEventOutbox _outbox;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerEmail, request.Items);
        
        await _repository.AddAsync(order);

        // Add to outbox (same transaction)
        await _outbox.AddAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount
        }, cancellationToken);

        // Save everything atomically
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto { Id = order.Id };
    }
}
```

## Publishing with RabbitMQ

### Configuration

```csharp
// Configure RabbitMQ
services.AddMvpRabbitMQ(options =>
{
    options.ConnectionString = "amqp://guest:guest@localhost:5672";
    options.Exchange = "orders";
});

// Register publisher
services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
```

### Outbox Processor

```csharp
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
            var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

            var messages = await outbox.GetPendingAsync(100, stoppingToken);

            foreach (var message in messages)
            {
                try
                {
                    await publisher.PublishFromOutboxAsync(message, stoppingToken);
                    await outbox.MarkAsPublishedAsync(message.Id, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish message {Id}", message.Id);
                    await outbox.MarkAsFailedAsync(message.Id, ex.Message, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

## Domain → Integration Conversion

### Manual Converter

```csharp
public class OrderCreatedDomainToIntegrationConverter 
    : IDomainToIntegrationEventConverter<OrderCreatedDomainEvent, OrderCreatedIntegrationEvent>
{
    public OrderCreatedIntegrationEvent? Convert(OrderCreatedDomainEvent domainEvent)
    {
        return new OrderCreatedIntegrationEvent
        {
            OrderId = domainEvent.OrderId,
            CustomerEmail = domainEvent.CustomerEmail,
            CorrelationId = domainEvent.Id.ToString()
        };
    }
}
```

### Automatic Conversion

```csharp
// Register automatic handler
services.AddTransient<
    IDomainEventHandler<OrderCreatedDomainEvent>,
    AutoIntegrationEventHandler<OrderCreatedDomainEvent, OrderCreatedIntegrationEvent>>();

// Register converter
services.AddTransient<
    IDomainToIntegrationEventConverter<OrderCreatedDomainEvent, OrderCreatedIntegrationEvent>,
    OrderCreatedDomainToIntegrationConverter>();
```

## Consuming Integration Events

```csharp
public class OrderCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<OrderCreatedIntegrationEventHandler> _logger;

    public async Task HandleAsync(
        OrderCreatedIntegrationEvent @event, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing OrderCreated integration event for order {OrderId}",
            @event.OrderId);

        // Reserve stock in another bounded context
        foreach (var item in @event.Items)
        {
            await _inventoryService.ReserveAsync(
                item.ProductId, 
                item.Quantity, 
                cancellationToken);
        }
    }
}
```

## Event Comparison

| Aspect | Domain Event | Integration Event |
|--------|-------------|-------------------|
| **Scope** | Bounded Context | Between Bounded Contexts |
| **Transport** | In-memory | Message Broker |
| **Consistency** | Transactional | Eventual |
| **Serialization** | Not needed | JSON/Protobuf |
| **Versioning** | Not needed | Important |
| **Retry** | Rollback | Dead Letter Queue |

## Best Practices

1. **Outbox Pattern**: Always use for guaranteed delivery
2. **Idempotency**: Consumers must be idempotent
3. **Versioning**: Version events for compatibility
4. **Correlation ID**: Propagate for distributed tracing
5. **Dead Letter**: Configure for problematic messages
6. **Monitoring**: Monitor queues and latency

