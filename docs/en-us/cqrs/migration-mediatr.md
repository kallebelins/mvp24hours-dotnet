# MediatR Migration Guide

## Overview

This guide helps migrate projects using MediatR to the Mvp24Hours Mediator.

## Interface Comparison

### Request/Response

| MediatR | Mvp24Hours |
|---------|-----------|
| `IRequest<TResponse>` | `IMediatorRequest<TResponse>` |
| `IRequest` | `IMediatorRequest` (returns `Unit`) |
| `IRequestHandler<TRequest, TResponse>` | `IMediatorRequestHandler<TRequest, TResponse>` |

### Commands and Queries

| MediatR | Mvp24Hours |
|---------|-----------|
| N/A (uses `IRequest`) | `IMediatorCommand<TResponse>` |
| N/A (uses `IRequest`) | `IMediatorQuery<TResponse>` |
| N/A | `IMediatorCommandHandler<TCommand, TResponse>` |
| N/A | `IMediatorQueryHandler<TQuery, TResponse>` |

### Notifications

| MediatR | Mvp24Hours |
|---------|-----------|
| `INotification` | `IMediatorNotification` |
| `INotificationHandler<TNotification>` | `IMediatorNotificationHandler<TNotification>` |

### Pipeline Behaviors

| MediatR | Mvp24Hours |
|---------|-----------|
| `IPipelineBehavior<TRequest, TResponse>` | `IPipelineBehavior<TRequest, TResponse>` |
| `RequestHandlerDelegate<TResponse>` | `RequestHandlerDelegate<TResponse>` |

## Step-by-Step Migration

### 1. Replace Packages

```xml
<!-- Remove -->
<PackageReference Include="MediatR" Version="12.x.x" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.x.x" />

<!-- Add -->
<PackageReference Include="Mvp24Hours.Infrastructure.Cqrs" Version="x.x.x" />
```

### 2. Update Usings

```csharp
// Remove
using MediatR;

// Add
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;
```

### 3. Migrate Requests

**MediatR:**
```csharp
public class CreateOrderCommand : IRequest<OrderDto>
{
    public string CustomerEmail { get; set; }
}
```

**Mvp24Hours:**
```csharp
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public required string CustomerEmail { get; init; }
}
```

### 4. Migrate Handlers

**MediatR:**
```csharp
public class CreateOrderCommandHandler 
    : IRequestHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        // ...
    }
}
```

**Mvp24Hours:**
```csharp
public class CreateOrderCommandHandler 
    : IMediatorCommandHandler<CreateOrderCommand, OrderDto>
{
    public async Task<OrderDto> Handle(
        CreateOrderCommand request, 
        CancellationToken cancellationToken)
    {
        // ... (same logic)
    }
}
```

### 5. Migrate Notifications

**MediatR:**
```csharp
public class OrderCreatedNotification : INotification
{
    public Guid OrderId { get; set; }
}

public class OrderCreatedHandler 
    : INotificationHandler<OrderCreatedNotification>
{
    public Task Handle(
        OrderCreatedNotification notification, 
        CancellationToken cancellationToken)
    {
        // ...
    }
}
```

**Mvp24Hours:**
```csharp
public record OrderCreatedNotification : IMediatorNotification
{
    public Guid OrderId { get; init; }
}

public class OrderCreatedHandler 
    : IMediatorNotificationHandler<OrderCreatedNotification>
{
    public Task Handle(
        OrderCreatedNotification notification, 
        CancellationToken cancellationToken)
    {
        // ... (same logic)
    }
}
```

### 6. Migrate Pipeline Behaviors

**MediatR:**
```csharp
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling {RequestName}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestName}", typeof(TRequest).Name);
        return response;
    }
}
```

**Mvp24Hours:**
```csharp
public class LoggingBehavior<TRequest, TResponse> 
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IMediatorRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // ... (same logic)
    }
}
```

### 7. Migrate DI Configuration

**MediatR:**
```csharp
services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssemblyContaining<Program>();
    cfg.AddBehavior<LoggingBehavior<,>>();
    cfg.AddBehavior<ValidationBehavior<,>>();
});
```

**Mvp24Hours:**
```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterLoggingBehavior = true;
    options.RegisterValidationBehavior = true;
});
```

### 8. Migrate Mediator Injection

**MediatR:**
```csharp
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
```

**Mvp24Hours:**
```csharp
public class OrderController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public OrderController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderCommand command)
    {
        var result = await _mediator.SendAsync(command);
        return Ok(result);
    }
}
```

## Important Differences

### Send vs SendAsync Method

| MediatR | Mvp24Hours |
|---------|-----------|
| `_mediator.Send(request)` | `_mediator.SendAsync(request)` |
| `_mediator.Publish(notification)` | `_mediator.PublishAsync(notification)` |

### Included Behaviors

Mvp24Hours includes ready-to-use behaviors:
- ✅ LoggingBehavior
- ✅ PerformanceBehavior
- ✅ ValidationBehavior
- ✅ CachingBehavior
- ✅ TransactionBehavior
- ✅ AuthorizationBehavior
- ✅ RetryBehavior
- ✅ IdempotencyBehavior

### Streaming

**MediatR:**
```csharp
public class GetItemsQuery : IStreamRequest<Item> { }
```

**Mvp24Hours:**
```csharp
public record GetItemsQuery : IStreamRequest<Item> { }

// In controller
await foreach (var item in _mediator.CreateStreamAsync(query))
{
    yield return item;
}
```

## Migration Checklist

- [ ] Replace NuGet packages
- [ ] Update usings
- [ ] Migrate `IRequest<T>` to `IMediatorCommand<T>` or `IMediatorQuery<T>`
- [ ] Migrate `IRequestHandler` to specific handlers
- [ ] Migrate `INotification` to `IMediatorNotification`
- [ ] Migrate `INotificationHandler` to `IMediatorNotificationHandler`
- [ ] Update constraint `where TRequest : IRequest<TResponse>` to `IMediatorRequest<TResponse>`
- [ ] Replace `Send()` with `SendAsync()`
- [ ] Replace `Publish()` with `PublishAsync()`
- [ ] Update DI configuration
- [ ] Test all handlers

