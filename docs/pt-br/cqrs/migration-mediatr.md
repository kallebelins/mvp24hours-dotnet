# Guia de Migração do MediatR

## Visão Geral

Este guia ajuda a migrar projetos que usam MediatR para o Mediator do Mvp24Hours.

## Comparação de Interfaces

### Request/Response

| MediatR | Mvp24Hours |
|---------|-----------|
| `IRequest<TResponse>` | `IMediatorRequest<TResponse>` |
| `IRequest` | `IMediatorRequest` (retorna `Unit`) |
| `IRequestHandler<TRequest, TResponse>` | `IMediatorRequestHandler<TRequest, TResponse>` |

### Commands e Queries

| MediatR | Mvp24Hours |
|---------|-----------|
| Não tem (usa `IRequest`) | `IMediatorCommand<TResponse>` |
| Não tem (usa `IRequest`) | `IMediatorQuery<TResponse>` |
| Não tem | `IMediatorCommandHandler<TCommand, TResponse>` |
| Não tem | `IMediatorQueryHandler<TQuery, TResponse>` |

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

## Migração Passo a Passo

### 1. Substituir Pacotes

```xml
<!-- Remover -->
<PackageReference Include="MediatR" Version="12.x.x" />
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.x.x" />

<!-- Adicionar -->
<PackageReference Include="Mvp24Hours.Infrastructure.Cqrs" Version="x.x.x" />
```

### 2. Atualizar Usings

```csharp
// Remover
using MediatR;

// Adicionar
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using Mvp24Hours.Infrastructure.Cqrs.Extensions;
```

### 3. Migrar Requests

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

### 4. Migrar Handlers

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
        // ... (mesma lógica)
    }
}
```

### 5. Migrar Notifications

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
        // ... (mesma lógica)
    }
}
```

### 6. Migrar Pipeline Behaviors

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
        // ... (mesma lógica)
    }
}
```

### 7. Migrar Configuração DI

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

### 8. Migrar Injeção do Mediator

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

## Diferenças Importantes

### Método Send vs SendAsync

| MediatR | Mvp24Hours |
|---------|-----------|
| `_mediator.Send(request)` | `_mediator.SendAsync(request)` |
| `_mediator.Publish(notification)` | `_mediator.PublishAsync(notification)` |

### Behaviors Incluídos

Mvp24Hours inclui behaviors prontos:
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

// No controller
await foreach (var item in _mediator.CreateStreamAsync(query))
{
    yield return item;
}
```

## Checklist de Migração

- [ ] Substituir pacotes NuGet
- [ ] Atualizar usings
- [ ] Migrar `IRequest<T>` para `IMediatorCommand<T>` ou `IMediatorQuery<T>`
- [ ] Migrar `IRequestHandler` para handlers específicos
- [ ] Migrar `INotification` para `IMediatorNotification`
- [ ] Migrar `INotificationHandler` para `IMediatorNotificationHandler`
- [ ] Atualizar constraint `where TRequest : IRequest<TResponse>` para `IMediatorRequest<TResponse>`
- [ ] Substituir `Send()` por `SendAsync()`
- [ ] Substituir `Publish()` por `PublishAsync()`
- [ ] Atualizar configuração DI
- [ ] Testar todos os handlers

