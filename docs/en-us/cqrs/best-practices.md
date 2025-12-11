# CQRS Best Practices

## Code Organization

### Folder Structure

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

### Naming Conventions

| Type | Convention | Example |
|------|-----------|---------|
| Command | `{Action}{Entity}Command` | `CreateOrderCommand` |
| Query | `Get{Entity}[By{Criteria}]Query` | `GetOrderByIdQuery` |
| Handler | `{Request}Handler` | `CreateOrderCommandHandler` |
| Validator | `{Request}Validator` | `CreateOrderCommandValidator` |
| Notification | `{Entity}{Event}Notification` | `OrderCreatedNotification` |

## Command Design

### Immutable Command

```csharp
// ✅ Good - Immutable record
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public required string CustomerEmail { get; init; }
    public required IReadOnlyList<OrderItemDto> Items { get; init; }
}

// ❌ Avoid - Mutable class
public class CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public string CustomerEmail { get; set; }
    public List<OrderItemDto> Items { get; set; }
}
```

### Focused Command

```csharp
// ✅ Good - Single specific purpose
public record UpdateOrderStatusCommand : IMediatorCommand
{
    public required Guid OrderId { get; init; }
    public required OrderStatus NewStatus { get; init; }
}

// ❌ Avoid - Does too many things
public record UpdateOrderCommand : IMediatorCommand
{
    public Guid OrderId { get; init; }
    public string? CustomerEmail { get; init; }
    public OrderStatus? Status { get; init; }
    public Address? ShippingAddress { get; init; }
    public decimal? Discount { get; init; }
}
```

## Query Design

### Specific Query

```csharp
// ✅ Good - Returns exactly what's needed
public record GetOrderSummaryQuery : IMediatorQuery<OrderSummaryDto>
{
    public required Guid OrderId { get; init; }
}

public class OrderSummaryDto
{
    public Guid Id { get; init; }
    public string Status { get; init; }
    public decimal Total { get; init; }
    // Only necessary fields
}
```

### Paginated Query

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

### Simple Handler

```csharp
// ✅ Good - Single responsibility
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

### Avoid Logic in Handler

```csharp
// ❌ Avoid - Too much logic in handler
public async Task<OrderDto> Handle(...)
{
    // Complex validation here ❌
    if (string.IsNullOrEmpty(request.CustomerEmail))
        throw new ValidationException(...);
    
    // Use ValidationBehavior + FluentValidation instead
}
```

## Validation

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

## Transactions

### Use ITransactional

```csharp
// ✅ Transactional command
public record TransferFundsCommand 
    : IMediatorCommand, ITransactional
{
    public required Guid FromAccountId { get; init; }
    public required Guid ToAccountId { get; init; }
    public required decimal Amount { get; init; }
}
```

## Caching

### Smart Caching

```csharp
// ✅ Cache with parameter-based key
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

### Rich Event

```csharp
// ✅ Event with useful information
public record OrderCreatedDomainEvent : IDomainEvent
{
    public required Guid OrderId { get; init; }
    public required string CustomerEmail { get; init; }
    public required decimal TotalAmount { get; init; }
    public required IReadOnlyList<Guid> ProductIds { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
```

## Error Handling

### Specific Exceptions

```csharp
// ✅ Use domain exceptions
throw new NotFoundException("Order", orderId);
throw new DomainException("Cannot cancel a delivered order");
throw new ConflictException("Order already processed");
```

### Result Pattern

```csharp
// ✅ Return result with errors
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

### Avoid N+1

```csharp
// ✅ Load necessary relationships
public async Task<OrderDto> Handle(GetOrderByIdQuery request, ...)
{
    var order = await _context.Orders
        .Include(o => o.Items)
        .Include(o => o.Customer)
        .FirstOrDefaultAsync(o => o.Id == request.OrderId);
}
```

### Use Projection

```csharp
// ✅ Project only needed fields
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

- [ ] Commands are immutable (records)
- [ ] Each command/query has a single purpose
- [ ] Validation via FluentValidation
- [ ] Handlers are simple and focused
- [ ] Transactions marked with ITransactional
- [ ] Cacheable queries marked with ICacheableRequest
- [ ] Domain-specific exceptions
- [ ] Avoid N+1 queries
- [ ] Use projection in queries

