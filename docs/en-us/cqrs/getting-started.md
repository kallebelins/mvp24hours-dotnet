# Getting Started with CQRS

## Installation

Add the package to your project:

```bash
dotnet add package Mvp24Hours.Infrastructure.Cqrs
```

## Basic Configuration

### 1. Register the Mediator

In `Program.cs` or `Startup.cs`:

```csharp
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

// Basic configuration with assembly scanning
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});

// Or with default behaviors
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.WithDefaultBehaviors(); // Logging, Performance, UnhandledException
});
```

### 2. Create a Command

```csharp
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

// Command with return value
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public string CustomerName { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}

// Command without return (void)
public record CancelOrderCommand : IMediatorCommand
{
    public Guid OrderId { get; init; }
}
```

### 3. Create the Handler

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderRepository repository, 
        IUnitOfWorkAsync unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = new Order
        {
            CustomerName = request.CustomerName,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList()
        };

        await _repository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OrderDto { Id = order.Id, /* ... */ };
    }
}
```

### 4. Use the Mediator

```csharp
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        CreateOrderCommand command)
    {
        var result = await _mediator.SendAsync(command);
        return Ok(result);
    }
}
```

## Complete Configuration

### With all behaviors

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    
    // Basic behaviors
    options.RegisterLoggingBehavior = true;
    options.RegisterPerformanceBehavior = true;
    options.RegisterUnhandledExceptionBehavior = true;
    
    // Advanced behaviors
    options.RegisterValidationBehavior = true;      // Requires FluentValidation
    options.RegisterCachingBehavior = true;         // Requires IDistributedCache
    options.RegisterTransactionBehavior = true;     // Requires IUnitOfWorkAsync
    options.RegisterAuthorizationBehavior = true;   // Requires IUserContext
    options.RegisterRetryBehavior = true;
    options.RegisterIdempotencyBehavior = true;     // Requires IDistributedCache
    
    // Performance settings
    options.PerformanceThresholdMilliseconds = 1000;
    options.MaxRetryAttempts = 3;
    options.IdempotencyDurationHours = 24;
});
```

### With Redis cache

```csharp
// Add Redis cache
services.AddMediatorRedisCache("localhost:6379", "myapp");

// Configure Mediator
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
    options.RegisterIdempotencyBehavior = true;
});
```

## Next Steps

- [Commands](commands.md) - Command details
- [Queries](queries.md) - Implementing queries
- [Behaviors](behaviors.md) - Pipeline behaviors in detail

