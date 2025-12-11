# Circuit Breaker

## Visão Geral

O Circuit Breaker é um padrão de resiliência que previne chamadas repetidas a um serviço falho, permitindo recuperação e evitando cascata de falhas.

## Estados do Circuit Breaker

```
┌─────────────────────────────────────────────────────────────────┐
│                    Circuit Breaker States                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌──────────┐    failures >= threshold    ┌──────────┐        │
│   │  CLOSED  │ ──────────────────────────▶ │   OPEN   │        │
│   │ (normal) │                             │ (failing)│        │
│   └──────────┘                             └──────────┘        │
│        ▲                                         │              │
│        │                                         │ timeout      │
│        │ success                                 ▼              │
│        │                                   ┌───────────┐        │
│        └─────────────────────────────────  │ HALF-OPEN │        │
│                                            │  (testing)│        │
│                                            └───────────┘        │
└─────────────────────────────────────────────────────────────────┘
```

## Interface ICircuitBreakerPolicy

```csharp
public interface ICircuitBreakerPolicy
{
    int FailureThreshold { get; }
    TimeSpan OpenDuration { get; }
    int SuccessThresholdToClose { get; }
}
```

## Comando com Circuit Breaker

```csharp
public record CallExternalApiCommand 
    : IMediatorCommand<ApiResponse>, ICircuitBreakerPolicy
{
    public required string Endpoint { get; init; }
    public required object Payload { get; init; }
    
    // Configuração do circuit breaker
    public int FailureThreshold => 5;
    public TimeSpan OpenDuration => TimeSpan.FromSeconds(30);
    public int SuccessThresholdToClose => 2;
}
```

## CircuitBreakerBehavior

```csharp
public sealed class CircuitBreakerBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICircuitBreakerPolicy
{
    private readonly ICircuitBreakerStateStore _stateStore;
    private readonly ILogger<CircuitBreakerBehavior<TRequest, TResponse>> _logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var circuitKey = typeof(TRequest).Name;
        var state = await _stateStore.GetStateAsync(circuitKey);

        // Verificar se circuito está aberto
        if (state.Status == CircuitStatus.Open)
        {
            if (DateTime.UtcNow < state.OpenedAt + request.OpenDuration)
            {
                _logger.LogWarning(
                    "Circuit {Circuit} is OPEN. Rejecting request.", 
                    circuitKey);
                throw new CircuitBreakerOpenException(circuitKey);
            }
            
            // Tentar transição para Half-Open
            await _stateStore.TransitionToHalfOpenAsync(circuitKey);
        }

        try
        {
            var result = await next();
            
            // Sucesso - resetar ou contar sucesso
            if (state.Status == CircuitStatus.HalfOpen)
            {
                await _stateStore.RecordSuccessAsync(circuitKey);
                if (state.SuccessCount >= request.SuccessThresholdToClose)
                {
                    await _stateStore.CloseAsync(circuitKey);
                    _logger.LogInformation(
                        "Circuit {Circuit} is now CLOSED.", 
                        circuitKey);
                }
            }
            else
            {
                await _stateStore.ResetAsync(circuitKey);
            }
            
            return result;
        }
        catch (Exception ex) when (IsTransientFailure(ex))
        {
            await _stateStore.RecordFailureAsync(circuitKey);
            
            if (state.FailureCount >= request.FailureThreshold)
            {
                await _stateStore.OpenAsync(circuitKey);
                _logger.LogWarning(
                    "Circuit {Circuit} is now OPEN due to {FailureCount} failures.",
                    circuitKey, state.FailureCount);
            }
            
            throw;
        }
    }

    private static bool IsTransientFailure(Exception ex)
    {
        return ex is TimeoutException 
            or HttpRequestException 
            or TaskCanceledException;
    }
}
```

## State Store

### Interface

```csharp
public interface ICircuitBreakerStateStore
{
    Task<CircuitState> GetStateAsync(string circuitKey);
    Task RecordFailureAsync(string circuitKey);
    Task RecordSuccessAsync(string circuitKey);
    Task OpenAsync(string circuitKey);
    Task CloseAsync(string circuitKey);
    Task TransitionToHalfOpenAsync(string circuitKey);
    Task ResetAsync(string circuitKey);
}
```

### CircuitState

```csharp
public class CircuitState
{
    public string CircuitKey { get; set; } = string.Empty;
    public CircuitStatus Status { get; set; } = CircuitStatus.Closed;
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime LastFailureAt { get; set; }
}

public enum CircuitStatus
{
    Closed,
    Open,
    HalfOpen
}
```

### Implementação Redis

```csharp
public class RedisCircuitBreakerStateStore : ICircuitBreakerStateStore
{
    private readonly IDistributedCache _cache;

    public async Task<CircuitState> GetStateAsync(string circuitKey)
    {
        var json = await _cache.GetStringAsync($"circuit:{circuitKey}");
        return json is null 
            ? new CircuitState { CircuitKey = circuitKey }
            : JsonSerializer.Deserialize<CircuitState>(json)!;
    }

    public async Task RecordFailureAsync(string circuitKey)
    {
        var state = await GetStateAsync(circuitKey);
        state.FailureCount++;
        state.LastFailureAt = DateTime.UtcNow;
        await SaveAsync(state);
    }

    public async Task OpenAsync(string circuitKey)
    {
        var state = await GetStateAsync(circuitKey);
        state.Status = CircuitStatus.Open;
        state.OpenedAt = DateTime.UtcNow;
        await SaveAsync(state);
    }

    private async Task SaveAsync(CircuitState state)
    {
        await _cache.SetStringAsync(
            $"circuit:{state.CircuitKey}",
            JsonSerializer.Serialize(state),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
            });
    }
}
```

## Integração com Polly

```csharp
public sealed class PollyCircuitBreakerBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICircuitBreakerPolicy
{
    private static readonly ConcurrentDictionary<string, AsyncCircuitBreakerPolicy> 
        _policies = new();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var circuitKey = typeof(TRequest).Name;
        
        var policy = _policies.GetOrAdd(circuitKey, _ =>
            Policy
                .Handle<TimeoutException>()
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: request.FailureThreshold,
                    durationOfBreak: request.OpenDuration,
                    onBreak: (ex, duration) =>
                    {
                        // Log circuit opened
                    },
                    onReset: () =>
                    {
                        // Log circuit closed
                    },
                    onHalfOpen: () =>
                    {
                        // Log half-open
                    }));

        try
        {
            return await policy.ExecuteAsync(async () => await next());
        }
        catch (BrokenCircuitException)
        {
            throw new CircuitBreakerOpenException(circuitKey);
        }
    }
}
```

## Exceção CircuitBreakerOpenException

```csharp
public class CircuitBreakerOpenException : Exception
{
    public string CircuitKey { get; }

    public CircuitBreakerOpenException(string circuitKey)
        : base($"Circuit '{circuitKey}' is currently open. Request rejected.")
    {
        CircuitKey = circuitKey;
    }
}
```

## Tratamento no Controller

```csharp
[HttpPost]
public async Task<ActionResult<ApiResponse>> CallApi(CallExternalApiCommand command)
{
    try
    {
        var result = await _mediator.SendAsync(command);
        return Ok(result);
    }
    catch (CircuitBreakerOpenException ex)
    {
        return StatusCode(503, new
        {
            Error = "Service temporarily unavailable",
            Circuit = ex.CircuitKey,
            RetryAfter = 30
        });
    }
}
```

## Métricas e Monitoramento

```csharp
public class CircuitBreakerMetrics
{
    private readonly IMetricsCollector _metrics;

    public void RecordOpen(string circuitKey)
    {
        _metrics.Increment("circuit_breaker_opens", 
            new[] { ("circuit", circuitKey) });
    }

    public void RecordClose(string circuitKey)
    {
        _metrics.Increment("circuit_breaker_closes",
            new[] { ("circuit", circuitKey) });
    }

    public void RecordRejection(string circuitKey)
    {
        _metrics.Increment("circuit_breaker_rejections",
            new[] { ("circuit", circuitKey) });
    }
}
```

## Boas Práticas

1. **Threshold Adequado**: Configure baseado em padrões de falha
2. **Timeout Razoável**: Tempo suficiente para recuperação
3. **Half-Open Testing**: Permita testes graduais
4. **Logging**: Registre todas as transições de estado
5. **Métricas**: Monitore opens, closes e rejeições
6. **Fallback**: Tenha estratégia de fallback quando aberto
7. **Per-Service**: Use circuitos separados por serviço externo

