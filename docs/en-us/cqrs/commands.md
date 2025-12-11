# Commands

## Overview

Commands represent intentions to modify state in the system. Following the CQRS pattern, commands are used for write operations.

## Interfaces

### IMediatorCommand<TResponse>

Command that returns a value:

```csharp
public interface IMediatorCommand<out TResponse> : IMediatorRequest<TResponse>
{
}
```

### IMediatorCommand

Command without return (void):

```csharp
public interface IMediatorCommand : IMediatorCommand<Unit>
{
}
```

> **Note**: `Unit` is a struct that represents "void" for generic types.

## Creating Commands

### Command with Return

```csharp
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}
```

### Command without Return

```csharp
public record CancelOrderCommand : IMediatorCommand
{
    public Guid OrderId { get; init; }
    public string Reason { get; init; } = string.Empty;
}
```

### Command with Validation

```csharp
public record UpdateOrderCommand : IMediatorCommand<OrderDto>
{
    public Guid OrderId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
}

public class UpdateOrderCommandValidator : AbstractValidator<UpdateOrderCommand>
{
    public UpdateOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");
            
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(100);
    }
}
```

## Creating Handlers

### Handler with Return

```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;
    private readonly IMapper _mapper;

    public CreateOrderCommandHandler(
        IOrderRepository repository,
        IUnitOfWorkAsync unitOfWork,
        IMapper mapper)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        // Create entity
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Pending
        };

        // Add items
        foreach (var item in request.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, item.UnitPrice);
        }

        // Persist
        await _repository.AddAsync(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Return DTO
        return _mapper.Map<OrderDto>(order);
    }
}
```

### Handler without Return

```csharp
public class CancelOrderCommandHandler 
    : IMediatorCommandHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _repository;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public CancelOrderCommandHandler(
        IOrderRepository repository,
        IUnitOfWorkAsync unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(
        CancelOrderCommand request, 
        CancellationToken cancellationToken)
    {
        var order = await _repository.GetByIdAsync(request.OrderId);
        
        if (order is null)
            throw new NotFoundException("Order", request.OrderId);

        order.Cancel(request.Reason);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
```

## Special Commands

### Transactional Command

```csharp
public record TransferFundsCommand : IMediatorCommand, ITransactionalCommand
{
    public Guid FromAccountId { get; init; }
    public Guid ToAccountId { get; init; }
    public decimal Amount { get; init; }
}
```

### Idempotent Command

```csharp
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IIdempotentCommand
{
    public Guid PaymentId { get; init; }
    public decimal Amount { get; init; }
    
    // Key based on payment ID
    public string? IdempotencyKey => $"payment:{PaymentId}";
    public TimeSpan? IdempotencyDuration => TimeSpan.FromHours(24);
}
```

### Authorized Command

```csharp
public record DeleteOrderCommand : IMediatorCommand, IAuthorizedRequest
{
    public Guid OrderId { get; init; }
    
    public IEnumerable<string> RequiredRoles => new[] { "Admin" };
    public IEnumerable<string> RequiredPermissions => new[] { "orders:delete" };
}
```

### Retryable Command

```csharp
public record SendEmailCommand : IMediatorCommand, IRetryableRequest
{
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    
    public int MaxRetries => 3;
    public TimeSpan BaseDelay => TimeSpan.FromSeconds(1);
    
    public bool ShouldRetry(Exception ex) => 
        ex is HttpRequestException or TimeoutException;
}
```

## Sending Commands

```csharp
public class OrderController : ControllerBase
{
    private readonly ISender _sender;

    public OrderController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.SendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Cancel(
        Guid id, 
        [FromBody] string reason,
        CancellationToken cancellationToken)
    {
        await _sender.SendAsync(new CancelOrderCommand 
        { 
            OrderId = id, 
            Reason = reason 
        }, cancellationToken);
        
        return NoContent();
    }
}
```

## Best Practices

1. **Immutability**: Use `record` or `init` properties
2. **Naming**: Use imperative verbs (Create, Update, Delete)
3. **Validation**: Use FluentValidation to validate inputs
4. **Single Responsibility**: One command = one operation
5. **Transactions**: Use `ITransactionalCommand` for critical operations
6. **Idempotency**: Use `IIdempotentCommand` for duplicable operations

