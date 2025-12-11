# Inbox/Outbox Patterns

## Visão Geral

Os padrões Inbox e Outbox garantem entrega confiável de mensagens em sistemas distribuídos, evitando perda de dados e duplicação.

## Outbox Pattern

### Problema

```
┌───────────────┐    ┌───────────────┐    ┌───────────────┐
│   Database    │    │   Handler     │    │   RabbitMQ    │
│   (Commit)    │ ✓  │   Success     │ ✗  │   (Falha)     │
└───────────────┘    └───────────────┘    └───────────────┘
        │                    │                    │
        └────────────────────┴────────────────────┘
                    Inconsistência!
        Dados salvos, mas evento não publicado
```

### Solução: Outbox

```
┌─────────────────────────────────────────────────────────────┐
│                    Outbox Pattern                            │
├─────────────────────────────────────────────────────────────┤
│  1. Handler salva dados + OutboxMessage na mesma transação  │
│  2. Processor lê OutboxMessages pendentes                    │
│  3. Processor publica no broker                             │
│  4. Processor marca como publicado                          │
└─────────────────────────────────────────────────────────────┘
```

### Interface IIntegrationEventOutbox

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

### Uso no Handler

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

        // Adiciona ao outbox (mesma transação)
        await _outbox.AddAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount
        }, cancellationToken);

        // Commit atômico
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

### Problema

```
┌───────────────┐    ┌───────────────┐
│   RabbitMQ    │    │   Consumer    │
│   Redelivery  │───▶│   Processa    │  Duplicação!
│   (retry)     │───▶│   de novo     │
└───────────────┘    └───────────────┘
```

### Solução: Inbox

```
┌─────────────────────────────────────────────────────────────┐
│                    Inbox Pattern                             │
├─────────────────────────────────────────────────────────────┤
│  1. Consumer recebe mensagem                                │
│  2. Verifica se MessageId já foi processado (Inbox)         │
│  3. Se sim → Ignora (deduplica)                             │
│  4. Se não → Processa e salva MessageId no Inbox            │
└─────────────────────────────────────────────────────────────┘
```

### Interface IInboxStore

```csharp
public interface IInboxStore
{
    Task<bool> ExistsAsync(Guid messageId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}
```

### Consumer com Inbox

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
        // Verificar duplicação
        if (await _inbox.ExistsAsync(@event.Id, cancellationToken))
        {
            return; // Já processado
        }

        // Processar
        foreach (var item in @event.Items)
        {
            await _inventoryService.ReserveAsync(item.ProductId, item.Quantity);
        }

        // Marcar como processado
        await _inbox.MarkAsProcessedAsync(@event.Id, cancellationToken);
    }
}
```

## Combinando Inbox + Outbox

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

## Implementação EF Core

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

## Boas Práticas

1. **Transação Atômica**: Outbox na mesma transação dos dados
2. **Polling Interval**: Configure intervalo adequado (5-30s)
3. **Batch Size**: Processe em lotes (100-500)
4. **Retry Limit**: Limite de tentativas antes de DLQ
5. **Cleanup**: Limpe mensagens antigas periodicamente
6. **Monitoring**: Monitore filas de outbox/inbox

