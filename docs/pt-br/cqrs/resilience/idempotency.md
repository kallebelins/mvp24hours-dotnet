# Idempotência de Commands

## Visão Geral

Idempotência garante que uma operação pode ser executada múltiplas vezes sem alterar o resultado além da execução inicial. Isso é essencial para sistemas distribuídos onde retries e duplicações podem ocorrer.

## Por que Idempotência?

```
┌──────────────────────────────────────────────────────────────┐
│                    Cenários de Duplicação                     │
├──────────────────────────────────────────────────────────────┤
│  1. Timeout de rede → Cliente faz retry                      │
│  2. Erro 500 → Cliente reenvia                               │
│  3. Message broker → Redelivery da mensagem                  │
│  4. Failover → Processamento duplicado                       │
└──────────────────────────────────────────────────────────────┘
```

## Implementação

### Interface IIdempotentCommand

```csharp
public interface IIdempotentCommand
{
    string? IdempotencyKey { get; }
}
```

### Command Idempotente

```csharp
public record CreateOrderCommand : IMediatorCommand<OrderDto>, IIdempotentCommand
{
    public required string CustomerEmail { get; init; }
    public required List<OrderItemDto> Items { get; init; }
    
    // Chave gerada pelo cliente
    public string? IdempotencyKey { get; init; }
}
```

### IdempotencyBehavior

```csharp
public sealed class IdempotencyBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
{
    private readonly IDistributedCache _cache;
    private readonly IIdempotencyKeyGenerator _keyGenerator;
    private readonly MediatorOptions _options;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var key = request.IdempotencyKey 
            ?? _keyGenerator.Generate(request);
        
        var cacheKey = $"idempotency:{key}";
        
        // Verificar se já foi processado
        var cached = await _cache.GetStringAsync(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        // Processar e armazenar resultado
        var result = await next();

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = 
                TimeSpan.FromHours(_options.IdempotencyDurationHours)
        };

        await _cache.SetStringAsync(
            cacheKey,
            JsonSerializer.Serialize(result),
            options,
            cancellationToken);

        return result;
    }
}
```

## Geração de Chaves

### IIdempotencyKeyGenerator

```csharp
public interface IIdempotencyKeyGenerator
{
    string Generate<TRequest>(TRequest request);
}
```

### DefaultIdempotencyKeyGenerator

```csharp
public class DefaultIdempotencyKeyGenerator : IIdempotencyKeyGenerator
{
    public string Generate<TRequest>(TRequest request)
    {
        var json = JsonSerializer.Serialize(request);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hash);
    }
}
```

### Chave Personalizada

```csharp
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IIdempotentCommand
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    
    // Chave baseada no OrderId para garantir um pagamento por pedido
    public string? IdempotencyKey => $"payment:{OrderId}";
}
```

## Configuração

```csharp
// Com cache em memória
services.AddMediatorMemoryCache();

// Ou com Redis (recomendado para produção)
services.AddMediatorRedisCache("localhost:6379");

services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterIdempotencyBehavior = true;
    options.IdempotencyDurationHours = 24; // Padrão
});
```

## Fluxo de Execução

```
┌─────────────────────────────────────────────────────────────────┐
│                    IdempotencyBehavior                           │
├─────────────────────────────────────────────────────────────────┤
│  1. Extrair/gerar IdempotencyKey                                │
│  2. Verificar cache: "idempotency:{key}"                        │
│     ├── HIT  → Retornar resultado cacheado                      │
│     └── MISS → Continuar processamento                          │
│  3. Executar handler                                            │
│  4. Armazenar resultado no cache                                │
│  5. Retornar resultado                                          │
└─────────────────────────────────────────────────────────────────┘
```

## Estratégias de Chave

### Por Usuário + Operação

```csharp
public string? IdempotencyKey => $"user:{UserId}:order:{DateTime.Today:yyyyMMdd}";
```

### Por Request ID (do cliente)

```csharp
// Cliente envia header X-Idempotency-Key
[HttpPost]
public async Task<ActionResult<OrderDto>> Create(
    CreateOrderCommand command,
    [FromHeader(Name = "X-Idempotency-Key")] string? idempotencyKey)
{
    var commandWithKey = command with { IdempotencyKey = idempotencyKey };
    var result = await _mediator.SendAsync(commandWithKey);
    return Ok(result);
}
```

### Por Hash do Conteúdo

```csharp
public string? IdempotencyKey => null; // Usa DefaultIdempotencyKeyGenerator
```

## Boas Práticas

1. **TTL Adequado**: Configure duração baseada no caso de uso
2. **Chaves Semânticas**: Use chaves que façam sentido para o domínio
3. **Redis em Produção**: Use Redis para ambientes distribuídos
4. **Cliente Gera Chave**: Permita que o cliente forneça a chave
5. **Logging**: Registre quando operações são dedupadas
6. **Monitoramento**: Monitore taxa de duplicação

