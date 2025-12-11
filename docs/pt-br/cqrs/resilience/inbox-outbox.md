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

## Registro no DI

O Mvp24Hours fornece extensões para registro simplificado:

```csharp
// Registra apenas Inbox
services.AddMvpInbox(options =>
{
    options.InboxRetentionDays = 7;
    options.EnableAutomaticCleanup = true;
});

// Registra apenas Outbox
services.AddMvpOutbox(options =>
{
    options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
    options.MaxRetries = 5;
    options.BatchSize = 100;
    options.EnableDeadLetterQueue = true;
});

// Registra ambos
services.AddMvpInboxOutbox(options =>
{
    options.OutboxPollingInterval = TimeSpan.FromSeconds(5);
    options.MaxRetries = 5;
    options.InboxRetentionDays = 7;
    options.EnableDeadLetterQueue = true;
});
```

## Processador de Inbox

O `IInboxProcessor` fornece deduplicação automática:

```csharp
public class OrderCreatedEventConsumer
{
    private readonly IInboxProcessor _processor;

    public async Task HandleAsync(OrderCreatedIntegrationEvent @event)
    {
        // Processa com deduplicação automática
        var processed = await _processor.ProcessAsync(@event, async (e, ct) =>
        {
            // Lógica de processamento
            await _inventoryService.ReserveItemsAsync(e.Items);
        });

        if (!processed)
        {
            // Mensagem duplicada, ignorada
        }
    }
}
```

## Dead Letter Queue (DLQ)

Mensagens que falham após o limite máximo de tentativas são movidas para a DLQ:

```csharp
// Interface IDeadLetterStore
public interface IDeadLetterStore
{
    Task AddAsync(DeadLetterMessage message, CancellationToken ct);
    Task<IReadOnlyList<DeadLetterMessage>> GetAllAsync(int limit = 100, CancellationToken ct);
    Task<bool> RequeueAsync(Guid id, CancellationToken ct);
    Task MarkAsResolvedAsync(Guid id, string resolution, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct);
}

// Gerenciando mensagens na DLQ
var dlqMessages = await _deadLetterStore.GetAllAsync();
foreach (var msg in dlqMessages)
{
    Console.WriteLine($"Falhou: {msg.EventType} - {msg.Error}");
    
    // Reprocessar após correção
    await _deadLetterStore.RequeueAsync(msg.Id);
    
    // Ou marcar como resolvido manualmente
    await _deadLetterStore.MarkAsResolvedAsync(msg.Id, "Processado manualmente");
}
```

## Exponential Backoff

O processador de outbox implementa retry com backoff exponencial:

```
Tentativa 1: delay = 1s
Tentativa 2: delay = 2s
Tentativa 3: delay = 4s
Tentativa 4: delay = 8s
Tentativa 5: delay = 16s (máximo configurável)
```

Configuração:

```csharp
services.AddMvpOutbox(options =>
{
    options.RetryBaseDelayMilliseconds = 1000;  // 1 segundo
    options.RetryMaxDelayMilliseconds = 60000;  // 1 minuto máximo
    options.MaxRetries = 5;
});
```

## Opções de Configuração

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

## Implementações Customizadas

Para produção, implemente stores persistentes:

```csharp
// Usar implementação customizada
services.AddMvpInboxOutbox()
        .UseInboxStore<EfCoreInboxStore>()
        .UseOutboxStore<EfCoreOutboxStore>()
        .UseDeadLetterStore<EfCoreDeadLetterStore>()
        .UseIntegrationEventPublisher<RabbitMqIntegrationEventPublisher>();
```

## Boas Práticas

1. **Transação Atômica**: Outbox na mesma transação dos dados
2. **Polling Interval**: Configure intervalo adequado (5-30s)
3. **Batch Size**: Processe em lotes (100-500)
4. **Retry Limit**: Limite de tentativas antes de DLQ
5. **Cleanup**: Limpe mensagens antigas periodicamente
6. **Monitoring**: Monitore filas de outbox/inbox
7. **DLQ**: Sempre habilite Dead Letter Queue em produção
8. **Backoff**: Use exponential backoff para evitar sobrecarga

