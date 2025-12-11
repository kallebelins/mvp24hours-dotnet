# Concepts Comparison

## ICommand (Repository) vs IMediatorCommand (CQRS)

Mvp24Hours has two `ICommand` interfaces with different purposes. This guide clarifies the differences.

## Repository Pattern - ICommand\<TEntity\>

### Location

```csharp
namespace Mvp24Hours.Core.Contract.Data
{
    public interface ICommand<TEntity> where TEntity : class
    {
        void Add(TEntity entity);
        void Modify(TEntity entity);
        void Remove(TEntity entity);
    }
}
```

### Purpose

- **CRUD operations** on the database
- **Generic data access**
- Part of the **Repository Pattern**
- Works directly with **entities**

### Usage

```csharp
public class OrderRepository : IRepository<Order>
{
    private readonly ICommand<Order> _command;
    
    public async Task AddAsync(Order order)
    {
        _command.Add(order);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

## Mediator Pattern - IMediatorCommand\<TResponse\>

### Location

```csharp
namespace Mvp24Hours.Infrastructure.Cqrs.Abstractions
{
    public interface IMediatorCommand<TResponse> : IMediatorRequest<TResponse>
    {
    }
    
    public interface IMediatorCommand : IMediatorCommand<Unit>
    {
    }
}
```

### Purpose

- **Application use cases**
- **Business logic orchestration**
- Part of the **CQRS/Mediator Pattern**
- Works with **DTOs and abstractions**

### Usage

```csharp
// Command
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public string CustomerEmail { get; init; }
    public List<OrderItemDto> Items { get; init; }
}

// Handler
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(CreateOrderCommand request, ...)
    {
        // Business logic + persistence via repository
    }
}
```

## Comparison Table

| Aspect | ICommand (Repository) | IMediatorCommand (CQRS) |
|--------|----------------------|-------------------------|
| **Namespace** | `Mvp24Hours.Core.Contract.Data` | `Mvp24Hours.Infrastructure.Cqrs.Abstractions` |
| **Layer** | Infrastructure/Data | Application |
| **Responsibility** | Database CRUD | Use case |
| **Operations** | Add, Modify, Remove | Any business operation |
| **Parameter** | `TEntity` (entity) | `TResponse` (DTO/result) |
| **Return** | void | TResponse or Unit |
| **Pipeline** | No | Yes (Behaviors) |
| **Validation** | Manual | Via ValidationBehavior |
| **Transaction** | Via UnitOfWork | Via TransactionBehavior |
| **Logging** | Manual | Via LoggingBehavior |

## Integration Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                     Controller/API                               │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      IMediator                                   │
│                   (Mediator Pattern)                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Pipeline Behaviors                              │
│    [Logging] → [Validation] → [Transaction] → [Caching]        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│            IMediatorCommandHandler / QueryHandler                │
│                    (Application Layer)                           │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   IRepository<TEntity>                           │
│               (uses ICommand<TEntity> internally)                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Database                                    │
└─────────────────────────────────────────────────────────────────┘
```

## Practical Example

### Scenario: Create Order

**Without Mediator (using Repository directly)**:
```csharp
[HttpPost]
public async Task<ActionResult> CreateOrder(CreateOrderRequest request)
{
    // Manual validation
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    // Direct logic
    var order = new Order { ... };
    
    // Manual transaction
    await _repository.AddAsync(order);
    await _unitOfWork.SaveChangesAsync();
    
    return Ok(new OrderDto { Id = order.Id });
}
```

**With Mediator**:
```csharp
[HttpPost]
public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderRequest request)
{
    var command = new CreateOrderCommand
    {
        CustomerEmail = request.CustomerEmail,
        Items = request.Items
    };
    
    // Validation, transaction, logging = automatic via behaviors
    var result = await _mediator.SendAsync(command);
    return Ok(result);
}
```

## When to Use Each

### Use ICommand (Repository) when:

- ✅ Simple CRUD operations
- ✅ Direct data access without business logic
- ✅ Inside Mediator handlers
- ✅ Database migrations and seeding

### Use IMediatorCommand when:

- ✅ Application use cases
- ✅ Need automatic validation
- ✅ Need managed transactions
- ✅ Need logging/auditing
- ✅ Need automatic caching
- ✅ Complex business logic

## Conclusion

Both coexist harmoniously:
- **IMediatorCommand** orchestrates the use case
- **ICommand/Repository** executes persistence

The Mediator doesn't replace the Repository - it complements it by adding an orchestration layer with cross-cutting concerns.

