# Notificações

## Visão Geral

Notificações são eventos in-process que podem ter múltiplos handlers. Diferente de Commands e Queries (que têm um único handler), notificações são publicadas para todos os handlers registrados.

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

## Criando Notificações

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

## Criando Handlers

### Múltiplos Handlers para uma Notificação

```csharp
// Handler 1: Enviar email
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

// Handler 2: Atualizar cache
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

// Handler 3: Registrar auditoria
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

## Publicando Notificações

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
        // Criar pedido
        var order = new Order { /* ... */ };
        await _repository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Publicar notificação
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

## Estratégias de Publicação

### Sequential (Padrão)

Executa handlers um após o outro:

```csharp
options.DefaultNotificationStrategy = NotificationPublishingStrategy.Sequential;
```

### ParallelWhenAll

Executa todos em paralelo e aguarda todos:

```csharp
options.DefaultNotificationStrategy = NotificationPublishingStrategy.ParallelWhenAll;
```

### ParallelNoWait

Dispara todos em paralelo sem aguardar:

```csharp
options.DefaultNotificationStrategy = NotificationPublishingStrategy.ParallelNoWait;
```

## Notificações vs Domain Events

| Aspecto | Notificação | Domain Event |
|---------|-------------|--------------|
| Interface | `IMediatorNotification` | `IDomainEvent` |
| Origem | Publicada explicitamente | Disparada pela entidade |
| Momento | Após operação concluída | Antes do SaveChanges |
| Transação | Fora da transação | Dentro da transação |

### Exemplo Domain Event

```csharp
// Domain Event na entidade
public class Order : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    public void Cancel(string reason)
    {
        Status = OrderStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        
        // Dispara evento de domínio
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

## Boas Práticas

1. **Fire-and-Forget**: Use para operações que não precisam bloquear
2. **Idempotência**: Handlers devem ser idempotentes
3. **Tratamento de Erros**: Cada handler deve tratar seus próprios erros
4. **Logging**: Registre sucesso e falha de cada handler
5. **Desacoplamento**: Handlers não devem depender uns dos outros

