# Domain Events

## Visão Geral

Domain Events são eventos que representam algo significativo que aconteceu no domínio. Eles são disparados por agregados/entidades e processados de forma síncrona dentro da mesma transação.

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

Interface para entidades que disparam eventos:

```csharp
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent eventItem);
    void RemoveDomainEvent(IDomainEvent eventItem);
    void ClearDomainEvents();
}
```

## Criando Domain Events

### Definindo o Evento

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

### Usando Base Record

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

## Implementando em Entidades

### Entidade com Domain Events

```csharp
public class Order : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public Guid Id { get; private set; }
    public string CustomerEmail { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    // Construtor privado para EF Core
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

        // Dispara evento de criação
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

        // Dispara evento de cancelamento
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

## Criando Handlers

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

## Despachando Domain Events

### Usando DomainEventDispatcher

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
        // Criar pedido (dispara eventos internamente)
        var order = Order.Create(request.CustomerEmail, request.Items);
        
        await _repository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Despachar eventos após salvar
        await _dispatcher.DispatchAsync(order.DomainEvents, cancellationToken);
        order.ClearDomainEvents();

        return new OrderDto { Id = order.Id };
    }
}
```

### Usando Extension Method

```csharp
// Extensão para IUnitOfWork
await _unitOfWork.SaveChangesWithEventsAsync(_dispatcher, cancellationToken);
```

## Domain Events vs Notifications

| Aspecto | Domain Event | Notification |
|---------|-------------|--------------|
| Origem | Entidade de domínio | Qualquer lugar |
| Momento | Durante operação de domínio | Após operação |
| Transação | Dentro da transação | Pode ser fora |
| Semântica | "Algo aconteceu no domínio" | "Notifique interessados" |

## Domain Events vs Integration Events

| Aspecto | Domain Event | Integration Event |
|---------|-------------|-------------------|
| Escopo | Bounded Context | Entre Bounded Contexts |
| Transporte | In-process (memória) | Message Broker |
| Garantia | Síncrono | Assíncrono (eventual) |
| Falha | Rollback da transação | Retry/Dead Letter |

## Boas Práticas

1. **Imutabilidade**: Domain Events devem ser imutáveis
2. **Nomenclatura**: Use passado (OrderCreated, PaymentProcessed)
3. **Consistência**: Eventos devem estar na mesma transação
4. **Idempotência**: Handlers devem ser idempotentes
5. **Auditoria**: Events são ótimos para audit trail

