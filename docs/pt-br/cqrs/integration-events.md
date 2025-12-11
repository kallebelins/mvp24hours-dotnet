# Integration Events

## Visão Geral

Integration Events são eventos usados para comunicação entre Bounded Contexts via message brokers (RabbitMQ, Kafka, etc.). Diferente de Domain Events (in-process), Integration Events atravessam fronteiras de aplicação.

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

## Criando Integration Events

### Usando Base Record

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

### Definindo Events

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

O Outbox Pattern garante entrega confiável de eventos.

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

### Usando o Outbox

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

        // Adiciona ao outbox (na mesma transação)
        await _outbox.AddAsync(new OrderCreatedIntegrationEvent
        {
            OrderId = order.Id,
            CustomerEmail = order.CustomerEmail,
            TotalAmount = order.TotalAmount
        }, cancellationToken);

        // Salva tudo atomicamente
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto { Id = order.Id };
    }
}
```

## Publicação com RabbitMQ

### Configuração

```csharp
// Configurar RabbitMQ
services.AddMvpRabbitMQ(options =>
{
    options.ConnectionString = "amqp://guest:guest@localhost:5672";
    options.Exchange = "orders";
});

// Registrar publisher
services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
```

### Processador de Outbox

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

## Conversão Domain → Integration

### Conversor Manual

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

### Conversão Automática

```csharp
// Registrar handler automático
services.AddTransient<
    IDomainEventHandler<OrderCreatedDomainEvent>,
    AutoIntegrationEventHandler<OrderCreatedDomainEvent, OrderCreatedIntegrationEvent>>();

// Registrar conversor
services.AddTransient<
    IDomainToIntegrationEventConverter<OrderCreatedDomainEvent, OrderCreatedIntegrationEvent>,
    OrderCreatedDomainToIntegrationConverter>();
```

## Consumindo Integration Events

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

        // Reservar estoque em outro bounded context
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

## Comparação de Eventos

| Aspecto | Domain Event | Integration Event |
|---------|-------------|-------------------|
| **Escopo** | Bounded Context | Entre Bounded Contexts |
| **Transporte** | In-memory | Message Broker |
| **Consistência** | Transacional | Eventual |
| **Serialização** | Não necessária | JSON/Protobuf |
| **Versionamento** | Não necessário | Importante |
| **Retry** | Rollback | Dead Letter Queue |

## Boas Práticas

1. **Outbox Pattern**: Sempre use para garantir entrega
2. **Idempotência**: Consumidores devem ser idempotentes
3. **Versionamento**: Versione eventos para compatibilidade
4. **Correlation ID**: Propague para tracing distribuído
5. **Dead Letter**: Configure para mensagens problemáticas
6. **Monitoring**: Monitore filas e latência

