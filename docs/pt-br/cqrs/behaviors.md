# Pipeline Behaviors

## Visão Geral

Pipeline Behaviors são interceptadores que processam requests antes e depois do handler. Eles permitem implementar cross-cutting concerns de forma modular.

## Fluxo de Execução

```
Request → [Behavior1 → [Behavior2 → [Handler] ← Behavior2] ← Behavior1] → Response
```

## Behaviors Disponíveis

### LoggingBehavior
Registra início, fim e duração de cada request.

```csharp
options.RegisterLoggingBehavior = true;
```

**Log de exemplo:**
```
[Information] Handling CreateOrderCommand
[Information] Handled CreateOrderCommand in 45ms
```

### PerformanceBehavior
Alerta sobre requests lentos.

```csharp
options.RegisterPerformanceBehavior = true;
options.PerformanceThresholdMilliseconds = 500; // Default: 500ms
```

**Log de exemplo:**
```
[Warning] CreateOrderCommand took 1250ms - Performance threshold exceeded!
```

### ValidationBehavior
Valida requests usando FluentValidation.

```csharp
options.RegisterValidationBehavior = true;

// Criar validator
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

// Registrar validators
services.AddValidatorsFromAssemblyContaining<Program>();
```

### CachingBehavior
Armazena respostas de queries em cache.

```csharp
options.RegisterCachingBehavior = true;

// Implementar interface ICacheableRequest
public record GetOrderByIdQuery : IMediatorQuery<OrderDto>, ICacheableRequest
{
    public Guid OrderId { get; init; }
    
    public string CacheKey => $"order:{OrderId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}
```

### TransactionBehavior
Envolve commands em transações.

```csharp
options.RegisterTransactionBehavior = true;

// Marcar command como transacional
public record CreateOrderCommand : IMediatorCommand<OrderDto>, ITransactionalCommand
{
    // ...
}
```

### AuthorizationBehavior
Verifica autorização antes de executar requests.

```csharp
options.RegisterAuthorizationBehavior = true;

// Implementar IUserContext
public interface IUserContext
{
    string? UserId { get; }
    IEnumerable<string> Roles { get; }
    bool HasPermission(string permission);
}

// Marcar request como autorizado
public record DeleteOrderCommand : IMediatorCommand, IAuthorizedRequest
{
    public Guid OrderId { get; init; }
    
    public IEnumerable<string> RequiredRoles => new[] { "Admin", "Manager" };
    public IEnumerable<string> RequiredPermissions => new[] { "orders:delete" };
}
```

### RetryBehavior
Implementa retry com backoff exponencial.

```csharp
options.RegisterRetryBehavior = true;
options.MaxRetryAttempts = 3;
options.RetryBaseDelayMilliseconds = 100;

// Marcar request como retentável
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IRetryableRequest
{
    public int MaxRetries => 3;
    public TimeSpan BaseDelay => TimeSpan.FromMilliseconds(100);
    
    public bool ShouldRetry(Exception ex) => ex is HttpRequestException;
}
```

### IdempotencyBehavior
Previne processamento duplicado de commands.

```csharp
options.RegisterIdempotencyBehavior = true;
options.IdempotencyDurationHours = 24;

// Marcar command como idempotente
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IIdempotentCommand
{
    public Guid PaymentId { get; init; }
    
    // Chave personalizada baseada no ID do pagamento
    public string? IdempotencyKey => $"payment:{PaymentId}";
    public TimeSpan? IdempotencyDuration => TimeSpan.FromHours(24);
}
```

## Criando Behaviors Customizados

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
        // Antes do handler
        var startTime = DateTime.UtcNow;
        
        // Executa próximo behavior ou handler
        var response = await next();
        
        // Depois do handler
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

// Registrar behavior customizado
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditBehavior<,>));
```

## Ordem de Execução

Os behaviors são executados na ordem em que são registrados:

1. UnhandledExceptionBehavior (primeiro - captura todas as exceções)
2. LoggingBehavior
3. PerformanceBehavior
4. AuthorizationBehavior
5. ValidationBehavior
6. IdempotencyBehavior
7. CachingBehavior
8. RetryBehavior
9. TransactionBehavior (último antes do handler)

**Handler executa aqui**

Os behaviors então retornam na ordem inversa.

