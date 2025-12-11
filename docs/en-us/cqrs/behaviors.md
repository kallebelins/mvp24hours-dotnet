# Pipeline Behaviors

## Overview

Pipeline Behaviors are interceptors that process requests before and after the handler. They enable implementing cross-cutting concerns in a modular way.

## Execution Flow

```
Request → [Behavior1 → [Behavior2 → [Handler] ← Behavior2] ← Behavior1] → Response
```

## Available Behaviors

### LoggingBehavior
Logs start, end, and duration of each request.

```csharp
options.RegisterLoggingBehavior = true;
```

**Example log:**
```
[Information] Handling CreateOrderCommand
[Information] Handled CreateOrderCommand in 45ms
```

### PerformanceBehavior
Alerts about slow requests.

```csharp
options.RegisterPerformanceBehavior = true;
options.PerformanceThresholdMilliseconds = 500; // Default: 500ms
```

**Example log:**
```
[Warning] CreateOrderCommand took 1250ms - Performance threshold exceeded!
```

### ValidationBehavior
Validates requests using FluentValidation.

```csharp
options.RegisterValidationBehavior = true;

// Create validator
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName)
            .NotEmpty()
            .MaximumLength(100);
            
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("Order must have at least one item");
    }
}

// Register validators
services.AddValidatorsFromAssemblyContaining<Program>();
```

### CachingBehavior
Stores query responses in cache.

```csharp
options.RegisterCachingBehavior = true;

// Implement ICacheableRequest interface
public record GetOrderByIdQuery : IMediatorQuery<OrderDto>, ICacheableRequest
{
    public Guid OrderId { get; init; }
    
    public string CacheKey => $"order:{OrderId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
```

### TransactionBehavior
Wraps commands in transactions.

```csharp
options.RegisterTransactionBehavior = true;

// Mark command as transactional
public record CreateOrderCommand : IMediatorCommand<OrderDto>, ITransactionalCommand
{
    // ...
}
```

### AuthorizationBehavior
Checks authorization before executing requests.

```csharp
options.RegisterAuthorizationBehavior = true;

// Implement IUserContext
public interface IUserContext
{
    string? UserId { get; }
    IEnumerable<string> Roles { get; }
    bool HasPermission(string permission);
}

// Mark request as authorized
public record DeleteOrderCommand : IMediatorCommand, IAuthorizedRequest
{
    public Guid OrderId { get; init; }
    
    public IEnumerable<string> RequiredRoles => new[] { "Admin", "Manager" };
    public IEnumerable<string> RequiredPermissions => new[] { "orders:delete" };
}
```

### RetryBehavior
Implements retry with exponential backoff.

```csharp
options.RegisterRetryBehavior = true;
options.MaxRetryAttempts = 3;
options.RetryBaseDelayMilliseconds = 100;

// Mark request as retryable
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IRetryableRequest
{
    public int MaxRetries => 3;
    public TimeSpan BaseDelay => TimeSpan.FromMilliseconds(100);
    
    public bool ShouldRetry(Exception ex) => ex is HttpRequestException;
}
```

### IdempotencyBehavior
Prevents duplicate command processing.

```csharp
options.RegisterIdempotencyBehavior = true;
options.IdempotencyDurationHours = 24;

// Mark command as idempotent
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IIdempotentCommand
{
    public Guid PaymentId { get; init; }
    
    // Custom key based on payment ID
    public string? IdempotencyKey => $"payment:{PaymentId}";
    public TimeSpan? IdempotencyDuration => TimeSpan.FromHours(24);
}
```

## Creating Custom Behaviors

```csharp
public class AuditBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    private readonly IAuditService _auditService;
    private readonly IUserContext _userContext;

    public AuditBehavior(IAuditService auditService, IUserContext userContext)
    {
        _auditService = auditService;
        _userContext = userContext;
    }

    public async Task<TResponse> Handle(
        TRequest request, 
        RequestHandlerDelegate<TResponse> next, 
        CancellationToken cancellationToken)
    {
        // Before handler
        var startTime = DateTime.UtcNow;
        
        // Execute next behavior or handler
        var response = await next();
        
        // After handler
        await _auditService.LogAsync(new AuditEntry
        {
            UserId = _userContext.UserId,
            RequestType = typeof(TRequest).Name,
            ExecutedAt = startTime,
            Duration = DateTime.UtcNow - startTime
        });
        
        return response;
    }
}

// Register custom behavior
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
```

## Execution Order

Behaviors execute in the order they are registered:

1. UnhandledExceptionBehavior (first - catches all exceptions)
2. LoggingBehavior
3. PerformanceBehavior
4. AuthorizationBehavior
5. ValidationBehavior
6. IdempotencyBehavior
7. CachingBehavior
8. RetryBehavior
9. TransactionBehavior (last before handler)

**Handler executes here**

Behaviors then return in reverse order.

