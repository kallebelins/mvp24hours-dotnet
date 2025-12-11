# Integration Events via RabbitMQ

## Overview

The Mediator integrates with the existing `Mvp24Hours.Infrastructure.RabbitMQ` to publish and consume Integration Events between bounded contexts.

## Configuration

### Installation

```bash
dotnet add package Mvp24Hours.Infrastructure.RabbitMQ
dotnet add package Mvp24Hours.Infrastructure.Cqrs
```

### Service Registration

```csharp
// Configure RabbitMQ
services.AddMvpRabbitMQ(options =>
{
    options.ConnectionString = "amqp://guest:guest@localhost:5672";
    options.Exchange = "myapp.events";
    options.ExchangeType = ExchangeType.Topic;
    options.QueueName = "myapp.orders";
});

// Register publisher
services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();

// Register outbox (optional but recommended)
services.AddScoped<IIntegrationEventOutbox, InMemoryIntegrationEventOutbox>();

// Register Mediator
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});
```

## Publishing Integration Events

### Event Definition

```csharp
public record OrderCreatedIntegrationEvent : IntegrationEventBase
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
}
```

### Direct Publishing

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IIntegrationEventPublisher _publisher;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerEmail, request.Items);
        await _repository.AddAsync(order);

        // Publish event directly
        await _publisher.PublishAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new OrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        }, cancellationToken);

        return OrderDto.FromEntity(order);
    }
}
```

### Publishing via Outbox (Recommended)

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

        return OrderDto.FromEntity(order);
    }
}
```

## Outbox Processor

```csharp
public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages");
            }

            await Task.Delay(_pollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outbox = scope.ServiceProvider.GetRequiredService<IIntegrationEventOutbox>();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();

        var messages = await outbox.GetPendingAsync(100, cancellationToken);
        
        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishFromOutboxAsync(message, cancellationToken);
                await outbox.MarkAsPublishedAsync(message.Id, cancellationToken);
                
                _logger.LogInformation("Published message {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish message {MessageId}", message.Id);
                await outbox.MarkAsFailedAsync(message.Id, ex.Message, cancellationToken);
            }
        }
    }
}

// Registration
services.AddHostedService<OutboxProcessor>();
```

## Consuming Integration Events

### Consumer Handler

```csharp
public class OrderCreatedIntegrationEventHandler 
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IInventoryService _inventoryService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderCreatedIntegrationEventHandler> _logger;

    public async Task HandleAsync(
        OrderCreatedIntegrationEvent @event, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing OrderCreated event for order {OrderId}",
            @event.OrderId);

        // Reserve stock
        foreach (var item in @event.Items)
        {
            await _inventoryService.ReserveAsync(
                item.ProductId, 
                item.Quantity, 
                cancellationToken);
        }

        // Send notification
        await _notificationService.SendAsync(
            @event.CustomerEmail,
            "Order Confirmed",
            $"Your order {@event.OrderId} has been confirmed.",
            cancellationToken);
    }
}
```

### Consumer Configuration

```csharp
// In startup
services.AddMvpRabbitMQConsumer<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>(
    options =>
    {
        options.QueueName = "inventory.order-created";
        options.RoutingKey = "orders.created";
        options.AutoAck = false;
        options.PrefetchCount = 10;
    });
```

## Routing Keys

### Naming Convention

```
{bounded-context}.{aggregate}.{event}

Examples:
- orders.order.created
- orders.order.cancelled
- inventory.stock.reserved
- payments.payment.processed
```

### Routing Configuration

```csharp
services.AddMvpRabbitMQ(options =>
{
    options.Exchange = "myapp.events";
    options.ExchangeType = ExchangeType.Topic;
});

// Publisher with routing key
await _publisher.PublishAsync(
    @event, 
    routingKey: "orders.order.created",
    cancellationToken);
```

## Error Handling

### Retry Policy

```csharp
services.AddMvpRabbitMQConsumer<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>(
    options =>
    {
        options.MaxRetryAttempts = 3;
        options.RetryDelayMilliseconds = 1000;
        options.ExponentialBackoff = true;
        options.DeadLetterExchange = "myapp.dlx";
    });
```

### Dead Letter Queue

```csharp
// Messages that failed after all retries
// are sent to DLQ for analysis
services.AddMvpRabbitMQDeadLetterConsumer(options =>
{
    options.QueueName = "myapp.dlq";
    options.OnDeadLetter = async (message, exception) =>
    {
        _logger.LogError(exception, 
            "Message {MessageId} moved to DLQ", 
            message.Id);
    };
});
```

## Best Practices

1. **Outbox Pattern**: Always use to ensure consistency
2. **Idempotency**: Handlers must be idempotent
3. **Correlation ID**: Propagate for distributed tracing
4. **Routing Keys**: Use consistent convention
5. **Dead Letter**: Configure for problematic messages
6. **Monitoring**: Monitor queues and latency
7. **Serialization**: Use JSON with schema versioning

