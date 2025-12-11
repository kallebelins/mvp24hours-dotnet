# Inbox/Outbox Patterns

## Overview

The Inbox and Outbox patterns ensure reliable message delivery in distributed systems, preventing data loss and duplication.

## Outbox Pattern

### Problem

```
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   Database    │    │   Handler     │    │   RabbitMQ    │
│   (Commit)    │ ✓  │   Success     │ ✗  │   (Failure)   │
└───────────────┘    └───────────────┘    └───────────────┘
        │                    │                    │
        └────────────────────┴────────────────────┘
                    Inconsistency!
        Data saved, but event not published
```

### Solution: Outbox

```
┌─────────────────────────────────────────────────────────────┐
│                    Outbox Pattern                            │
├─────────────────────────────────────────────────────────────┤
│  1. Handler saves data + OutboxMessage in same transaction  │
│  2. Processor reads pending OutboxMessages                  │
│  3. Processor publishes to broker                           │
│  4. Processor marks as published                            │
└─────────────────────────────────────────────────────────────┘
```

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

### OutboxMessage

```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public OutboxMessageStatus Status { get; set; }
}

public enum OutboxMessageStatus
{
    Pending,
    Published,
    Failed
}
```

### Usage in Handler

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

        // Atomic commit
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return OrderDto.FromEntity(order);
    }
}
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
                    _logger.LogInformation("Published {MessageId}", message.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish {MessageId}", message.Id);
                    await outbox.MarkAsFailedAsync(message.Id, ex.Message, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

## Inbox Pattern

### Problem

```
┌───────────────┐    ┌───────────────┐
│   RabbitMQ    │    │   Consumer    │
│   Redelivery  │───▶│   Processes   │  Duplication!
│   (retry)     │───▶│   again       │
└───────────────┘    └───────────────┘
```

### Solution: Inbox

```
┌─────────────────────────────────────────────────────────────┐
│                    Inbox Pattern                             │
├─────────────────────────────────────────────────────────────┤
│  1. Consumer receives message                               │
│  2. Check if MessageId was already processed (Inbox)        │
│  3. If yes → Ignore (deduplicate)                           │
│  4. If no → Process and save MessageId in Inbox             │
└─────────────────────────────────────────────────────────────┘
```

### IInboxStore Interface

```csharp
public interface IInboxStore
{
    Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}
```

### Consumer with Inbox

```csharp
public class OrderCreatedEventConsumer 
    : IIntegrationEventHandler<OrderCreatedIntegrationEvent>
{
    private readonly IInboxStore _inbox;
    private readonly IInventoryService _inventoryService;

    public async Task HandleAsync(
        OrderCreatedIntegrationEvent @event, 
        CancellationToken cancellationToken)
    {
        // Check for duplication
        if (await _inbox.ExistsAsync(@event.Id, cancellationToken))
        {
            return; // Already processed
        }

        // Process
        foreach (var item in @event.Items)
        {
            await _inventoryService.ReserveAsync(item.ProductId, item.Quantity);
        }

        // Mark as processed
        await _inbox.MarkAsProcessedAsync(@event.Id, cancellationToken);
    }
}
```

## Combining Inbox + Outbox

```
┌─────────────────────────────────────────────────────────────────┐
│                    Service A (Producer)                          │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐                     │
│  │ Handler │───▶│ Outbox  │───▶│Processor│───▶ RabbitMQ        │
│  └─────────┘    └─────────┘    └─────────┘                     │
└─────────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Service B (Consumer)                          │
│  RabbitMQ ───▶ ┌─────────┐    ┌─────────┐    ┌─────────┐       │
│                │Consumer │───▶│  Inbox  │───▶│ Handler │       │
│                └─────────┘    └─────────┘    └─────────┘       │
└─────────────────────────────────────────────────────────────────┘
```

## EF Core Implementation

### OutboxMessage Entity

```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}

// DbContext
public DbSet<OutboxMessage> OutboxMessages { get; set; }
```

### EfCoreIntegrationEventOutbox

```csharp
public class EfCoreIntegrationEventOutbox : IIntegrationEventOutbox
{
    private readonly AppDbContext _context;

    public async Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        var message = new OutboxMessage
        {
            Id = @event.Id,
            EventType = typeof(TEvent).AssemblyQualifiedName!,
            Payload = JsonSerializer.Serialize(@event),
            CreatedAt = DateTime.UtcNow
        };
        
        await _context.OutboxMessages.AddAsync(message, cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        int batchSize, 
        CancellationToken cancellationToken)
    {
        return await _context.OutboxMessages
            .Where(m => m.PublishedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
```

## Best Practices

1. **Atomic Transaction**: Outbox in same transaction as data
2. **Polling Interval**: Configure appropriate interval (5-30s)
3. **Batch Size**: Process in batches (100-500)
4. **Retry Limit**: Limit attempts before DLQ
5. **Cleanup**: Clean old messages periodically
6. **Monitoring**: Monitor outbox/inbox queues

