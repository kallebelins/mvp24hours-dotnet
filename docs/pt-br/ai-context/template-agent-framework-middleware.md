# Template Middleware e Extensibilidade - Microsoft Agent Framework

> **Propósito**: Este template fornece padrões para pipelines de middleware e mecanismos de extensibilidade.

---

## Visão Geral

Middleware e extensibilidade permitem:
- Pipelines de processamento de request/response
- Concerns transversais (logging, caching, retry)
- Modificações de comportamento de IA
- Tratamento de exceções e resiliência

---

## Quando Usar Este Template

| Cenário | Recomendação |
|---------|--------------|
| Adicionar logging a chamadas de IA | ✅ Recomendado |
| Implementar caching | ✅ Recomendado |
| Rate limiting de requests | ✅ Recomendado |
| Integrações simples | ⚠️ Use Agente Básico |

---

## Implementação

### DelegatingChatClient Base

```csharp
using Microsoft.Extensions.AI;

public abstract class DelegatingChatClient : IChatClient
{
    protected IChatClient InnerClient { get; }

    protected DelegatingChatClient(IChatClient innerClient)
    {
        InnerClient = innerClient ?? throw new ArgumentNullException(nameof(innerClient));
    }

    public virtual ChatClientMetadata Metadata => InnerClient.Metadata;

    public virtual Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => InnerClient.CompleteAsync(chatMessages, options, cancellationToken);

    public virtual IAsyncEnumerable<StreamingChatCompletionUpdate> CompleteStreamingAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => InnerClient.CompleteStreamingAsync(chatMessages, options, cancellationToken);

    public virtual void Dispose() => InnerClient.Dispose();
}
```

### Middleware de Logging

```csharp
public class LoggingChatClient : DelegatingChatClient
{
    private readonly ILogger _logger;

    public LoggingChatClient(IChatClient innerClient, ILogger<LoggingChatClient> logger)
        : base(innerClient)
    {
        _logger = logger;
    }

    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Chat request iniciado. Mensagens: {Count}", chatMessages.Count);

        try
        {
            var result = await base.CompleteAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();
            _logger.LogInformation("Chat completado em {Duration}ms", stopwatch.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Chat falhou após {Duration}ms", stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Middleware de Caching

```csharp
public class CachingChatClient : DelegatingChatClient
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration;

    public CachingChatClient(IChatClient innerClient, IMemoryCache cache, TimeSpan? expiration = null)
        : base(innerClient)
    {
        _cache = cache;
        _cacheExpiration = expiration ?? TimeSpan.FromMinutes(5);
    }

    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(chatMessages, options);

        if (_cache.TryGetValue(cacheKey, out ChatCompletion? cachedResult) && cachedResult != null)
            return cachedResult;

        var result = await base.CompleteAsync(chatMessages, options, cancellationToken);
        _cache.Set(cacheKey, result, _cacheExpiration);

        return result;
    }

    private static string GenerateCacheKey(IList<ChatMessage> messages, ChatOptions? options)
    {
        var content = string.Join("|", messages.Select(m => $"{m.Role}:{m.Text}"));
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash);
    }
}
```

### Middleware de Retry

```csharp
public class RetryChatClient : DelegatingChatClient
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly ILogger _logger;

    public RetryChatClient(IChatClient innerClient, ILogger<RetryChatClient> logger,
        int maxRetries = 3, TimeSpan? baseDelay = null) : base(innerClient)
    {
        _logger = logger;
        _maxRetries = maxRetries;
        _baseDelay = baseDelay ?? TimeSpan.FromSeconds(1);
    }

    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        for (int attempt = 0; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await base.CompleteAsync(chatMessages, options, cancellationToken);
            }
            catch (HttpRequestException ex) when (ShouldRetry(ex) && attempt < _maxRetries)
            {
                var delay = TimeSpan.FromTicks(_baseDelay.Ticks * (long)Math.Pow(2, attempt));
                _logger.LogWarning("Tentativa {Attempt} falhou. Retry em {Delay}ms", attempt + 1, delay.TotalMilliseconds);
                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("Todas as tentativas falharam");
    }

    private static bool ShouldRetry(HttpRequestException ex)
        => ex.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
}
```

### Pipeline Builder

```csharp
public class ChatClientBuilder
{
    private IChatClient _client;

    public ChatClientBuilder(IChatClient innerClient) => _client = innerClient;

    public ChatClientBuilder UseLogging(ILoggerFactory loggerFactory)
    {
        _client = new LoggingChatClient(_client, loggerFactory.CreateLogger<LoggingChatClient>());
        return this;
    }

    public ChatClientBuilder UseCaching(IMemoryCache cache, TimeSpan? expiration = null)
    {
        _client = new CachingChatClient(_client, cache, expiration);
        return this;
    }

    public ChatClientBuilder UseRetry(int maxRetries = 3)
    {
        _client = new RetryChatClient(_client, NullLogger<RetryChatClient>.Instance, maxRetries);
        return this;
    }

    public IChatClient Build() => _client;
}
```

---

## Registro de Dependência

```csharp
public static class MiddlewareExtensions
{
    public static IServiceCollection AddAIWithMiddleware(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        services.AddSingleton<IChatClient>(sp =>
        {
            var baseChatClient = ChatClientFactory.CreateChatClient(configuration);
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var cache = sp.GetRequiredService<IMemoryCache>();

            return new ChatClientBuilder(baseChatClient)
                .UseLogging(loggerFactory)
                .UseRetry(maxRetries: 3)
                .UseCaching(cache, TimeSpan.FromMinutes(5))
                .Build();
        });

        return services;
    }
}
```

---

## Boas Práticas

1. **Ordem Importa**: Coloque middleware na ordem correta (logging primeiro, caching por último)
2. **Design Stateless**: Mantenha middleware stateless quando possível
3. **Tratamento de Erros**: Propague exceções apropriadamente
4. **Cleanup de Recursos**: Implemente IDisposable quando necessário

---

## Templates Relacionados

- [Agent Framework Básico](template-agent-framework-basic.md) - Configuração de agente
- [Graph Workflows](template-agent-framework-workflows.md) - Padrões de workflow
- [Observability](template-skg-observability.md) - Métricas e monitoramento

