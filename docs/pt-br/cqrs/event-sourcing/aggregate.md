# Agregados com Event Sourcing

## Visão Geral

Agregados em Event Sourcing mantêm seu estado através da aplicação de eventos, ao invés de armazenar estado diretamente.

## Interface Base

### IAggregate

```csharp
public interface IAggregate
{
    Guid Id { get; }
    long Version { get; }
    IReadOnlyCollection<IDomainEvent> UncommittedEvents { get; }
    void ClearUncommittedEvents();
}
```

## Classe Base

### EventSourcedAggregate

```csharp
public abstract class EventSourcedAggregate : IAggregate
{
    private readonly List<IDomainEvent> _uncommittedEvents = new();
    
    public Guid Id { get; protected set; }
    public long Version { get; private set; }
    
    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => 
        _uncommittedEvents.AsReadOnly();

    // Aplica evento para reconstruir estado
    protected abstract void Apply(IDomainEvent @event);

    // Emite novo evento
    protected void Raise(IDomainEvent @event)
    {
        Apply(@event);
        _uncommittedEvents.Add(@event);
        Version++;
    }

    // Reconstrói a partir de eventos históricos
    public void LoadFromHistory(IEnumerable<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            Apply(@event);
            Version++;
        }
    }

    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }
}
```

## Exemplo Completo: Order

### Eventos

```csharp
public record OrderCreatedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string CustomerEmail { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderItemAddedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderPaidEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderShippedEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string TrackingNumber { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record OrderCancelledEvent : IDomainEvent
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

### Agregado Order

```csharp
public class Order : EventSourcedAggregate
{
    private readonly List<OrderItem> _items = new();
    
    public string CustomerEmail { get; private set; } = string.Empty;
    public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Construtor privado para reconstrução
    private Order() { }

    // Factory method para criação
    public static Order Create(string customerEmail)
    {
        var order = new Order();
        order.Raise(new OrderCreatedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = customerEmail,
            TotalAmount = 0
        });
        return order;
    }

    // Métodos de negócio
    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatus.Created)
            throw new DomainException("Cannot add items to a non-pending order");

        Raise(new OrderItemAddedEvent
        {
            OrderId = Id,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice
        });
    }

    public void Pay(Guid paymentId)
    {
        if (Status != OrderStatus.Created)
            throw new DomainException("Order is not in a payable state");

        if (!_items.Any())
            throw new DomainException("Cannot pay for an empty order");

        Raise(new OrderPaidEvent
        {
            OrderId = Id,
            PaymentId = paymentId,
            Amount = TotalAmount
        });
    }

    public void Ship(string trackingNumber)
    {
        if (Status != OrderStatus.Paid)
            throw new DomainException("Order must be paid before shipping");

        Raise(new OrderShippedEvent
        {
            OrderId = Id,
            TrackingNumber = trackingNumber
        });
    }

    public void Cancel(string reason)
    {
        if (Status == OrderStatus.Shipped)
            throw new DomainException("Cannot cancel a shipped order");

        Raise(new OrderCancelledEvent
        {
            OrderId = Id,
            Reason = reason
        });
    }

    // Aplicar eventos para reconstruir estado
    protected override void Apply(IDomainEvent @event)
    {
        switch (@event)
        {
            case OrderCreatedEvent e:
                Id = e.OrderId;
                CustomerEmail = e.CustomerEmail;
                TotalAmount = e.TotalAmount;
                Status = OrderStatus.Created;
                break;

            case OrderItemAddedEvent e:
                _items.Add(new OrderItem(e.ProductId, e.Quantity, e.UnitPrice));
                TotalAmount += e.Quantity * e.UnitPrice;
                break;

            case OrderPaidEvent:
                Status = OrderStatus.Paid;
                break;

            case OrderShippedEvent:
                Status = OrderStatus.Shipped;
                break;

            case OrderCancelledEvent:
                Status = OrderStatus.Cancelled;
                break;
        }
    }
}
```

## Repositório Event Sourced

```csharp
public class EventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate>
    where TAggregate : EventSourcedAggregate, new()
{
    private readonly IEventStore _eventStore;

    public async Task<TAggregate?> GetByIdAsync(Guid id)
    {
        var events = await _eventStore.GetEventsAsync(id);
        
        if (!events.Any())
            return null;

        var aggregate = new TAggregate();
        aggregate.LoadFromHistory(events);
        return aggregate;
    }

    public async Task SaveAsync(TAggregate aggregate)
    {
        var uncommittedEvents = aggregate.UncommittedEvents;
        
        if (!uncommittedEvents.Any())
            return;

        var expectedVersion = aggregate.Version - uncommittedEvents.Count;
        
        await _eventStore.AppendEventsAsync(
            aggregate.Id, 
            uncommittedEvents, 
            expectedVersion);
        
        aggregate.ClearUncommittedEvents();
    }
}
```

## Command Handler

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IEventSourcedRepository<Order> _repository;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerEmail);
        
        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
        }

        await _repository.SaveAsync(order);

        return new OrderDto
        {
            Id = order.Id,
            Status = order.Status.ToString(),
            TotalAmount = order.TotalAmount
        };
    }
}
```

## Boas Práticas

1. **Eventos Imutáveis**: Use records para eventos
2. **Nomenclatura no Passado**: OrderCreated, não OrderCreate
3. **Eventos Completos**: Inclua todos os dados necessários
4. **Validação no Agregado**: Valide antes de emitir eventos
5. **Um Agregado por Transação**: Evite modificar múltiplos
6. **Versão para Concorrência**: Use versão otimista

