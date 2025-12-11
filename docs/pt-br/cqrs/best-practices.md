# Boas Práticas CQRS

## Organização de Código

### Estrutura de Pastas

```
src/
├── Application/
│   ├── Commands/
│   │   ├── CreateOrder/
│   │   │   ├── CreateOrderCommand.cs
│   │   │   ├── CreateOrderCommandHandler.cs
│   │   │   └── CreateOrderCommandValidator.cs
│   │   └── UpdateOrder/
│   │       └── ...
│   ├── Queries/
│   │   ├── GetOrderById/
│   │   │   ├── GetOrderByIdQuery.cs
│   │   │   └── GetOrderByIdQueryHandler.cs
│   │   └── GetOrders/
│   │       └── ...
│   ├── Notifications/
│   │   ├── OrderCreated/
│   │   │   ├── OrderCreatedNotification.cs
│   │   │   └── OrderCreatedNotificationHandler.cs
│   │   └── ...
│   └── Behaviors/
│       └── CustomBehavior.cs
├── Domain/
│   ├── Entities/
│   ├── Events/
│   └── Exceptions/
└── Infrastructure/
    └── ...
```

### Nomenclatura

| Tipo | Convenção | Exemplo |
|------|-----------|---------|
| Command | `{Ação}{Entidade}Command` | `CreateOrderCommand` |
| Query | `Get{Entidade}[By{Critério}]Query` | `GetOrderByIdQuery` |
| Handler | `{Request}Handler` | `CreateOrderCommandHandler` |
| Validator | `{Request}Validator` | `CreateOrderCommandValidator` |
| Notification | `{Entidade}{Evento}Notification` | `OrderCreatedNotification` |

## Design de Commands

### Command Imutável

```csharp
// ✅ Bom - Record imutável
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public required string CustomerEmail { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
}

// ❌ Evite - Class mutável
public class CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public string CustomerEmail { get; set; }
    public List<OrderItemDto> Items { get; set; }
}
```

### Command Focado

```csharp
// ✅ Bom - Um propósito específico
public record UpdateOrderStatusCommand : IMediatorCommand
{
    public required Guid OrderId { get; init; }
    public required OrderStatus NewStatus { get; init; }
}

// ❌ Evite - Faz muitas coisas
public record UpdateOrderCommand : IMediatorCommand
{
    public Guid OrderId { get; init; }
    public string? CustomerEmail { get; init; }
    public OrderStatus? Status { get; init; }
    public Address? ShippingAddress { get; init; }
    public decimal? Discount { get; init; }
}
```

## Design de Queries

### Query Específica

```csharp
// ✅ Bom - Retorna exatamente o necessário
public record GetOrderSummaryQuery : IMediatorQuery<OrderSummaryDto>
{
    public required Guid OrderId { get; init; }
}

public class OrderSummaryDto
{
    public Guid Id { get; init; }
    public string Status { get; init; }
    public decimal Total { get; init; }
    // Apenas campos necessários
}
```

### Query Paginada

```csharp
public record GetOrdersQuery : IMediatorQuery<PagedResult<OrderListItemDto>>
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public OrderStatus? Status { get; init; }
    public DateTime? FromDate { get; init; }
}
```

## Handlers

### Handler Simples

```csharp
// ✅ Bom - Responsabilidade única
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = Order.Create(request.CustomerEmail, request.Items);
        await _repository.AddAsync(order, cancellationToken);
        return OrderDto.FromEntity(order);
    }
}
```

### Evite Lógica no Handler

```csharp
// ❌ Evite - Muita lógica no handler
public async Task<OrderDto> Handle(...)
{
    // Validação complexa aqui ❌
    if (string.IsNullOrEmpty(request.CustomerEmail))
        throw new ValidationException(...);
    
    // Use ValidationBehavior + FluentValidation
}
```

## Validação

### FluentValidation

```csharp
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator(ICustomerRepository customerRepo)
    {
        RuleFor(x => x.CustomerEmail)
            .NotEmpty()
            .EmailAddress()
            .MustAsync(async (email, ct) => 
                await customerRepo.ExistsAsync(email, ct))
            .WithMessage("Customer not found");

        RuleFor(x => x.Items)
            .NotEmpty()
            .Must(items => items.All(i => i.Quantity > 0))
            .WithMessage("All items must have positive quantity");
    }
}
```

## Transações

### Use ITransactional

```csharp
// ✅ Comando transacional
public record TransferFundsCommand 
    : IMediatorCommand, ITransactional
{
    public required Guid FromAccountId { get; init; }
    public required Guid ToAccountId { get; init; }
    public required decimal Amount { get; init; }
}
```

## Cache

### Cache Inteligente

```csharp
// ✅ Cache com chave baseada em parâmetros
public record GetProductQuery 
    : IMediatorQuery<ProductDto>, ICacheableRequest
{
    public required Guid ProductId { get; init; }
    
    public string CacheKey => $"product:{ProductId}";
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(15);
    public TimeSpan? SlidingExpiration => TimeSpan.FromMinutes(5);
}
```

## Domain Events

### Evento Rico

```csharp
// ✅ Evento com informações úteis
public record OrderCreatedDomainEvent : IDomainEvent
{
    public required Guid OrderId { get; init; }
    public required string CustomerEmail { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<Guid> ProductIds { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

## Tratamento de Erros

### Exceções Específicas

```csharp
// ✅ Use exceções do domínio
throw new NotFoundException("Order", orderId);
throw new DomainException("Cannot cancel a delivered order");
throw new ConflictException("Order already processed");
```

### Result Pattern

```csharp
// ✅ Retorne resultado com erros
public async Task<IBusinessResult<OrderDto>> Handle(...)
{
    var order = await _repository.GetByIdAsync(request.OrderId);
    
    if (order is null)
        return BusinessResultFactory.Failure<OrderDto>(
            StructuredMessageResult.NotFound("Order", request.OrderId));
    
    return BusinessResultFactory.Success(OrderDto.FromEntity(order));
}
```

## Performance

### Evite N+1

```csharp
// ✅ Carregue relacionamentos necessários
public async Task<OrderDto> Handle(GetOrderByIdQuery request, ...)
{
    var order = await _context.Orders
        .Include(o => o.Items)
        .Include(o => o.Customer)
        .FirstOrDefaultAsync(o => o.Id == request.OrderId);
}
```

### Use Projeção

```csharp
// ✅ Projete apenas campos necessários
public async Task<IReadOnlyList<OrderListItemDto>> Handle(...)
{
    return await _context.Orders
        .Where(o => o.Status == request.Status)
        .Select(o => new OrderListItemDto
        {
            Id = o.Id,
            CustomerName = o.Customer.Name,
            Total = o.Total
        })
        .ToListAsync();
}
```

## Checklist

- [ ] Commands são imutáveis (records)
- [ ] Cada command/query tem um único propósito
- [ ] Validação via FluentValidation
- [ ] Handlers são simples e focados
- [ ] Transações marcadas com ITransactional
- [ ] Queries cacheaveis marcadas com ICacheableRequest
- [ ] Exceções específicas do domínio
- [ ] Evitar N+1 queries
- [ ] Usar projeção em queries

