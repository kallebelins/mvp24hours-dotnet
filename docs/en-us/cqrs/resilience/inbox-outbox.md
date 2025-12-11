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

## DI Registration

Mvp24Hours provides simplified registration extensions:

```csharp
// Register Inbox only
services.AddMvpInbox(options =>
{
    options.InboxRetentionDays = 7;
    options.EnableAutomaticCleanup = true;
});

// Register Outbox only
services.AddMvpOutbox(options =>
{
    options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
    options.MaxRetries = 5;
    options.BatchSize = 100;
    options.EnableDeadLetterQueue = true;
});

// Register both
services.AddMvpInboxOutbox(options =>
{
    options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
    options.MaxRetries = 5;
    options.InboxRetentionDays = 7;
    options.EnableDeadLetterQueue = true;
});
```

## Inbox Processor

The `IInboxProcessor` provides automatic deduplication:

```csharp
public class OrderCreatedEventConsumer
{
    private readonly IInboxProcessor _processor;

    public async Task HandleAsync(OrderCreatedIntegrationEvent @event)
    {
        // Process with automatic deduplication
        var processed = await _processor.ProcessAsync(@event, async (e, ct) =>
        {
            // Processing logic
            await _inventoryService.ReserveItemsAsync(e.Items);
        });

        if (!processed)
        {
            // Duplicate message, ignored
        }
    }
}
```

## Dead Letter Queue (DLQ)

Messages that fail after the maximum retry limit are moved to the DLQ:

```csharp
// IDeadLetterStore Interface
public interface IDeadLetterStore
{
    Task AddAsync(DeadLetterMessage message, CancellationToken ct);
    Task<IReadOnlyList<DeadLetterMessage>> GetAllAsync(int limit = 100, CancellationToken ct);
    Task<bool> RequeueAsync(Guid id, CancellationToken ct);
    Task MarkAsResolvedAsync(Guid id, string resolution, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

// Managing DLQ messages
var dlqMessages = await _deadLetterStore.GetAllAsync();
foreach (var msg in dlqMessages)
{
    Console.WriteLine($"Failed: {msg.EventType} - {msg.Error}");
    
    // Reprocess after fixing issue
    await _deadLetterStore.RequeueAsync(msg.Id);
    
    // Or mark as manually resolved
    await _deadLetterStore.MarkAsResolvedAsync(msg.Id, "Manually processed");
}
```

## Exponential Backoff

The outbox processor implements retry with exponential backoff:

```
Attempt 1: delay = 1s
Attempt 2: delay = 2s
Attempt 3: delay = 4s
Attempt 4: delay = 8s
Attempt 5: delay = 16s (max configurable)
```

Configuration:

```csharp
services.AddMvpOutbox(options =>
{
    options.RetryBaseDelayMilliseconds = 1000;  // 1 second
    options.RetryMaxDelayMilliseconds = 60000;  // 1 minute max
    options.MaxRetries = 5;
});
```

## Configuration Options

```csharp
public class InboxOutboxOptions
{
    // Outbox
    public TimeSpan OutboxPollingInterval { get; set; } = TimeSpan.FromSeconds(5);
    public int BatchSize { get; set; } = 100;
    public int MaxRetries { get; set; } = 5;
    public int RetryBaseDelayMilliseconds { get; set; } = 1000;
    public int RetryMaxDelayMilliseconds { get; set; } = 60000;
    public int OutboxRetentionDays { get; set; } = 7;

    // Inbox
    public int InboxRetentionDays { get; set; } = 7;

    // Cleanup
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(1);
    public bool EnableAutomaticCleanup { get; set; } = true;

    // Dead Letter Queue
    public bool EnableDeadLetterQueue { get; set; } = true;
    public int DeadLetterRetentionDays { get; set; } = 30;

    // Performance
    public bool EnableParallelProcessing { get; set; } = false;
    public int MaxDegreeOfParallelism { get; set; } = 4;
}
```

## Custom Implementations

For production, implement persistent stores:

```csharp
// Use custom implementation
services.AddMvpInboxOutbox()
        .UseInboxStore<EfCoreInboxStore>()
        .UseOutboxStore<EfCoreOutboxStore>()
        .UseDeadLetterStore<EfCoreDeadLetterStore>()
        .UseIntegrationEventPublisher<RabbitMqIntegrationEventPublisher>();
```

## Best Practices

1. **Atomic Transaction**: Outbox in same transaction as data
2. **Polling Interval**: Configure appropriate interval (5-30s)
3. **Batch Size**: Process in batches (100-500)
4. **Retry Limit**: Limit attempts before DLQ
5. **Cleanup**: Clean old messages periodically
6. **Monitoring**: Monitor outbox/inbox queues
7. **DLQ**: Always enable Dead Letter Queue in production
8. **Backoff**: Use exponential backoff to prevent overload

