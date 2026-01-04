# Começando com CQRS

## Instalação

Adicione o pacote ao seu projeto:

```bash
dotnet add package Mvp24Hours.Infrastructure.Cqrs
```

## Configuração Básica

### 1. Registrar o Mediator

No `Program.cs` ou `Startup.cs`:

```csharp
using Mvp24Hours.Infrastructure.Cqrs.Extensions;

// Configuração básica com assembly scanning
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});

// Ou com behaviors padrão
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.WithDefaultBehaviors(); // Logging, Performance, UnhandledException
});
```

### 2. Criar um Command

```csharp
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

// Command com retorno
public record CreateOrderCommand : IMediatorCommand<OrderDto>
{
    public string CustomerName { get; init; } = string.Empty;
    public List<OrderItemDto> Items { get; init; } = new();
}

// Command sem retorno
public record CancelOrderCommand : IMediatorCommand
{
    public Guid OrderId { get; init; }
}
```

### 3. Criar o Handler

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

### 4. Usar o Mediator

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

## Configuração Completa

### Com todos os behaviors

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    
    // Behaviors básicos
    options.RegisterLoggingBehavior = true;
    options.RegisterPerformanceBehavior = true;
    options.RegisterUnhandledExceptionBehavior = true;
    
    // Behaviors avançados
    options.RegisterValidationBehavior = true;      // Requer FluentValidation
    options.RegisterCachingBehavior = true;         // Requer IDistributedCache
    options.RegisterTransactionBehavior = true;     // Requer IUnitOfWorkAsync
    options.RegisterAuthorizationBehavior = true;   // Requer IUserContext
    options.RegisterRetryBehavior = true;
    options.RegisterIdempotencyBehavior = true;     // Requer IDistributedCache
    
    // Configurações de performance
    options.PerformanceThresholdMilliseconds = 1000;
    options.MaxRetryAttempts = 3;
    options.IdempotencyDurationHours = 24;
});
```

### Com cache Redis

```csharp
// Adicionar cache Redis
services.AddMediatorRedisCache("localhost:6379", "myapp");

// Configurar Mediator
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterCachingBehavior = true;
    options.RegisterIdempotencyBehavior = true;
});
```

## Próximos Passos

- [Commands](commands.md) - Detalhes sobre commands
- [Queries](queries.md) - Implementando queries
- [Behaviors](behaviors.md) - Pipeline behaviors em detalhes

