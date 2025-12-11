# Políticas de Retry

## Visão Geral

O `RetryBehavior` implementa políticas de retry com backoff exponencial para comandos que podem falhar temporariamente.

## Interface IRetryable

```csharp
public interface IRetryable
{
    int MaxRetryAttempts { get; }
    int BaseDelayMilliseconds { get; }
}
```

## Comando Retryable

```csharp
public record ProcessPaymentCommand 
    : IMediatorCommand<PaymentResult>, IRetryable
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CardToken { get; init; }
    
    // Configuração de retry
    public int MaxRetryAttempts => 3;
    public int BaseDelayMilliseconds => 100;
}
```

## RetryBehavior

```csharp
public sealed class RetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRetryable
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;
    private readonly MediatorOptions _options;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var maxAttempts = request.MaxRetryAttempts > 0 
            ? request.MaxRetryAttempts 
            : _options.MaxRetryAttempts;
            
        var baseDelay = request.BaseDelayMilliseconds > 0 
            ? request.BaseDelayMilliseconds 
            : _options.RetryBaseDelayMilliseconds;

        var attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                return await next();
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                var delay = CalculateDelay(attempt, baseDelay);
                
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{MaxAttempts} failed for {RequestType}. " +
                    "Retrying in {Delay}ms...",
                    attempt, maxAttempts, typeof(TRequest).Name, delay);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is TimeoutException
            or HttpRequestException
            or TaskCanceledException
            or OperationCanceledException;
    }

    private static int CalculateDelay(int attempt, int baseDelay)
    {
        // Exponential backoff with jitter
        var exponential = (int)Math.Pow(2, attempt - 1) * baseDelay;
        var jitter = Random.Shared.Next(0, 100);
        return Math.Min(exponential + jitter, 30000); // Max 30s
    }
}
```

## Configuração

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterRetryBehavior = true;
    
    // Configurações padrão
    options.MaxRetryAttempts = 3;
    options.RetryBaseDelayMilliseconds = 100;
});
```

## Estratégias de Backoff

### Linear

```
Attempt 1: 100ms
Attempt 2: 200ms
Attempt 3: 300ms
```

### Exponencial

```
Attempt 1: 100ms
Attempt 2: 200ms
Attempt 3: 400ms
Attempt 4: 800ms
```

### Exponencial com Jitter (recomendado)

```
Attempt 1: 100ms + random(0-100)
Attempt 2: 200ms + random(0-100)
Attempt 3: 400ms + random(0-100)
```

## Exceções Transientes

### Padrão

```csharp
private static bool IsTransient(Exception ex)
{
    return ex is TimeoutException
        or HttpRequestException
        or TaskCanceledException
        or OperationCanceledException;
}
```

### Personalizado

```csharp
public interface IRetryable
{
    int MaxRetryAttempts { get; }
    int BaseDelayMilliseconds { get; }
    bool ShouldRetry(Exception ex) => IsDefaultTransient(ex);
    
    static bool IsDefaultTransient(Exception ex) => 
        ex is TimeoutException or HttpRequestException;
}

// Uso
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IRetryable
{
    public int MaxRetryAttempts => 3;
    public int BaseDelayMilliseconds => 500;
    
    public bool ShouldRetry(Exception ex) => 
        ex is TimeoutException 
        or PaymentGatewayException { IsRetryable: true };
}
```

## Integração com Polly

```csharp
public sealed class PollyRetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRetryable
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<TimeoutException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: request.MaxRetryAttempts,
                sleepDurationProvider: attempt => 
                    TimeSpan.FromMilliseconds(
                        Math.Pow(2, attempt) * request.BaseDelayMilliseconds),
                onRetry: (ex, delay, attempt, _) =>
                {
                    // Log retry
                });

        return await policy.ExecuteAsync(async () => await next());
    }
}
```

## Fluxo de Execução

```
┌─────────────────────────────────────────────────────────────────┐
│                      RetryBehavior                               │
├─────────────────────────────────────────────────────────────────┤
│  Attempt 1:                                                     │
│    ├── Execute handler                                          │
│    ├── ❌ TimeoutException                                      │
│    └── Wait 100ms                                               │
│                                                                 │
│  Attempt 2:                                                     │
│    ├── Execute handler                                          │
│    ├── ❌ HttpRequestException                                  │
│    └── Wait 200ms                                               │
│                                                                 │
│  Attempt 3:                                                     │
│    ├── Execute handler                                          │
│    └── ✅ Success → Return result                               │
└─────────────────────────────────────────────────────────────────┘
```

## Boas Práticas

1. **Apenas Transientes**: Retry apenas para erros transientes
2. **Idempotência**: Commands devem ser idempotentes
3. **Max Attempts**: Limite razoável (3-5 tentativas)
4. **Backoff**: Use exponencial com jitter
5. **Timeout Total**: Considere timeout máximo da operação
6. **Logging**: Registre todas as tentativas
7. **Métricas**: Monitore taxa de retry

