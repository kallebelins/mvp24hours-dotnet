# Middleware & Extensibility Template - Microsoft Agent Framework

> **Purpose**: This template provides AI agents with patterns for implementing middleware pipelines and extensibility mechanisms using Microsoft.Extensions.AI.

---

## Overview

Middleware and extensibility enable:
- Request/response processing pipelines
- Cross-cutting concerns (logging, caching, retry)
- Custom AI behavior modifications
- Exception handling and resilience

---

## When to Use This Template

| Scenario | Recommendation |
|----------|----------------|
| Adding logging to AI calls | ✅ Recommended |
| Implementing caching | ✅ Recommended |
| Rate limiting AI requests | ✅ Recommended |
| Custom retry policies | ✅ Recommended |
| Simple AI integrations | ⚠️ Use Basic Agent |

---

## Required NuGet Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" Version="9.*-*" />
  <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="9.*" />
  <PackageReference Include="Microsoft.Extensions.Resilience" Version="9.*" />
  <PackageReference Include="Polly" Version="8.*" />
</ItemGroup>
```

---

## Implementation Patterns

### 1. DelegatingChatClient Base

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

    public virtual TService? GetService<TService>(object? key = null)
        where TService : class
        => InnerClient.GetService<TService>(key);
}
```

### 2. Logging Middleware

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
        var requestId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "[{RequestId}] Chat request started. Messages: {Count}",
            requestId, chatMessages.Count);

        try
        {
            var result = await base.CompleteAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "[{RequestId}] Chat completed in {Duration}ms. Tokens: {Tokens}",
                requestId, stopwatch.ElapsedMilliseconds,
                result.Usage?.TotalTokenCount ?? 0);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "[{RequestId}] Chat failed after {Duration}ms",
                requestId, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### 3. Caching Middleware

```csharp
public class CachingChatClient : DelegatingChatClient
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration;
    private readonly ILogger _logger;

    public CachingChatClient(
        IChatClient innerClient,
        IMemoryCache cache,
        ILogger<CachingChatClient> logger,
        TimeSpan? cacheExpiration = null)
        : base(innerClient)
    {
        _cache = cache;
        _logger = logger;
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(5);
    }

    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GenerateCacheKey(chatMessages, options);

        if (_cache.TryGetValue(cacheKey, out ChatCompletion? cachedResult) && cachedResult != null)
        {
            _logger.LogDebug("Cache hit for key: {Key}", cacheKey[..16]);
            return cachedResult;
        }

        var result = await base.CompleteAsync(chatMessages, options, cancellationToken);

        _cache.Set(cacheKey, result, _cacheExpiration);
        _logger.LogDebug("Cached result for key: {Key}", cacheKey[..16]);

        return result;
    }

    private static string GenerateCacheKey(IList<ChatMessage> messages, ChatOptions? options)
    {
        var content = string.Join("|", messages.Select(m => $"{m.Role}:{m.Text}"));
        var optionsKey = options != null
            ? $"{options.Temperature}:{options.MaxOutputTokens}"
            : "default";

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes($"{content}|{optionsKey}"));
        return Convert.ToHexString(hash);
    }
}
```

### 4. Retry Middleware

```csharp
public class RetryChatClient : DelegatingChatClient
{
    private readonly int _maxRetries;
    private readonly TimeSpan _baseDelay;
    private readonly ILogger _logger;

    public RetryChatClient(
        IChatClient innerClient,
        ILogger<RetryChatClient> logger,
        int maxRetries = 3,
        TimeSpan? baseDelay = null)
        : base(innerClient)
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
        var attempt = 0;

        while (true)
        {
            try
            {
                return await base.CompleteAsync(chatMessages, options, cancellationToken);
            }
            catch (HttpRequestException ex) when (ShouldRetry(ex) && attempt < _maxRetries)
            {
                attempt++;
                var delay = TimeSpan.FromTicks(_baseDelay.Ticks * (long)Math.Pow(2, attempt - 1));

                _logger.LogWarning(
                    "Request failed (attempt {Attempt}/{Max}). Retrying in {Delay}ms",
                    attempt, _maxRetries, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool ShouldRetry(HttpRequestException ex)
        => ex.StatusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
```

### 5. Rate Limiting Middleware

```csharp
public class RateLimitingChatClient : DelegatingChatClient
{
    private readonly SemaphoreSlim _semaphore;
    private readonly int _requestsPerMinute;
    private readonly Queue<DateTimeOffset> _requestTimes = new();
    private readonly object _lock = new();

    public RateLimitingChatClient(
        IChatClient innerClient,
        int requestsPerMinute = 60)
        : base(innerClient)
    {
        _requestsPerMinute = requestsPerMinute;
        _semaphore = new SemaphoreSlim(requestsPerMinute, requestsPerMinute);
    }

    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await WaitForSlotAsync(cancellationToken);

        try
        {
            return await base.CompleteAsync(chatMessages, options, cancellationToken);
        }
        finally
        {
            RecordRequest();
        }
    }

    private async Task WaitForSlotAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            lock (_lock)
            {
                CleanupOldRequests();
                if (_requestTimes.Count < _requestsPerMinute)
                    return;
            }
            await Task.Delay(100, cancellationToken);
        }
    }

    private void RecordRequest()
    {
        lock (_lock)
        {
            _requestTimes.Enqueue(DateTimeOffset.UtcNow);
        }
    }

    private void CleanupOldRequests()
    {
        var threshold = DateTimeOffset.UtcNow.AddMinutes(-1);
        while (_requestTimes.Count > 0 && _requestTimes.Peek() < threshold)
            _requestTimes.Dequeue();
    }
}
```

### 6. Metrics Middleware

```csharp
public interface IMetricsService
{
    void RecordLatency(string operation, long milliseconds);
    void IncrementCounter(string name, Dictionary<string, string>? tags = null);
}

public class MetricsChatClient : DelegatingChatClient
{
    private readonly IMetricsService _metrics;

    public MetricsChatClient(IChatClient innerClient, IMetricsService metrics)
        : base(innerClient)
    {
        _metrics = metrics;
    }

    public override async Task<ChatCompletion> CompleteAsync(
        IList<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await base.CompleteAsync(chatMessages, options, cancellationToken);
            stopwatch.Stop();

            _metrics.RecordLatency("chat_completion", stopwatch.ElapsedMilliseconds);
            _metrics.IncrementCounter("chat_completion_success");

            if (result.Usage != null)
            {
                _metrics.IncrementCounter("tokens_used", new Dictionary<string, string>
                {
                    ["type"] = "total",
                    ["count"] = result.Usage.TotalTokenCount.ToString()
                });
            }

            return result;
        }
        catch
        {
            _metrics.IncrementCounter("chat_completion_error");
            throw;
        }
    }
}
```

---

## Pipeline Builder

```csharp
public class ChatClientBuilder
{
    private IChatClient _client;

    public ChatClientBuilder(IChatClient innerClient)
    {
        _client = innerClient;
    }

    public ChatClientBuilder UseLogging(ILoggerFactory loggerFactory)
    {
        _client = new LoggingChatClient(_client,
            loggerFactory.CreateLogger<LoggingChatClient>());
        return this;
    }

    public ChatClientBuilder UseCaching(IMemoryCache cache, TimeSpan? expiration = null)
    {
        _client = new CachingChatClient(_client, cache,
            NullLogger<CachingChatClient>.Instance, expiration);
        return this;
    }

    public ChatClientBuilder UseRetry(int maxRetries = 3)
    {
        _client = new RetryChatClient(_client,
            NullLogger<RetryChatClient>.Instance, maxRetries);
        return this;
    }

    public ChatClientBuilder UseRateLimiting(int requestsPerMinute = 60)
    {
        _client = new RateLimitingChatClient(_client, requestsPerMinute);
        return this;
    }

    public ChatClientBuilder UseMetrics(IMetricsService metrics)
    {
        _client = new MetricsChatClient(_client, metrics);
        return this;
    }

    public ChatClientBuilder Use(Func<IChatClient, IChatClient> middleware)
    {
        _client = middleware(_client);
        return this;
    }

    public IChatClient Build() => _client;
}
```

---

## Dependency Injection

```csharp
public static class MiddlewareExtensions
{
    public static IServiceCollection AddAIWithMiddleware(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<IMetricsService, PrometheusMetricsService>();

        services.AddSingleton<IChatClient>(sp =>
        {
            var baseChatClient = ChatClientFactory.CreateChatClient(configuration);
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            var metrics = sp.GetRequiredService<IMetricsService>();

            return new ChatClientBuilder(baseChatClient)
                .UseLogging(loggerFactory)
                .UseMetrics(metrics)
                .UseRetry(maxRetries: 3)
                .UseRateLimiting(requestsPerMinute: 60)
                .UseCaching(cache, TimeSpan.FromMinutes(5))
                .Build();
        });

        return services;
    }
}
```

---

## Testing Middleware

```csharp
public class LoggingChatClientTests
{
    [Fact]
    public async Task CompleteAsync_LogsRequestAndResponse()
    {
        // Arrange
        var mockInnerClient = new Mock<IChatClient>();
        mockInnerClient
            .Setup(x => x.CompleteAsync(
                It.IsAny<IList<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ChatCompletion(new ChatMessage(ChatRole.Assistant, "Response")));

        var mockLogger = new Mock<ILogger<LoggingChatClient>>();
        var client = new LoggingChatClient(mockInnerClient.Object, mockLogger.Object);

        // Act
        await client.CompleteAsync(new List<ChatMessage>
        {
            new(ChatRole.User, "Test")
        });

        // Assert
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("started")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

---

## Best Practices

1. **Order Matters**: Place middleware in correct order (logging first, caching last)
2. **Stateless Design**: Keep middleware stateless when possible
3. **Error Handling**: Always propagate exceptions appropriately
4. **Resource Cleanup**: Implement IDisposable when needed
5. **Testing**: Test each middleware in isolation

---

## Related Templates

- [Agent Framework Basic](template-agent-framework-basic.md) - Agent setup
- [Graph Workflows](template-agent-framework-workflows.md) - Workflow patterns
- [Observability](template-skg-observability.md) - Metrics and monitoring

---

## External References

- [Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/ai-extensions)
- [Polly Resilience](https://github.com/App-vNext/Polly)
- [Middleware Pattern](https://docs.microsoft.com/aspnet/core/fundamentals/middleware)

