# Rate Limiting with System.Threading.RateLimiting

> Native .NET 7+ rate limiting using `System.Threading.RateLimiting`.

## Overview

The Mvp24Hours framework provides native rate limiting using .NET's built-in `System.Threading.RateLimiting` namespace, which was introduced in .NET 7. This replaces custom implementations with the standard, high-performance rate limiting APIs.

### Supported Algorithms

| Algorithm | Description | Use Case |
|-----------|-------------|----------|
| **FixedWindow** | Counts requests in fixed time windows | Simple rate limiting with potential bursts at window boundaries |
| **SlidingWindow** | Smooths fixed window boundaries | More accurate rate limiting, recommended for most APIs |
| **TokenBucket** | Allows controlled bursts with smooth refill | APIs that allow occasional bursts |
| **Concurrency** | Limits concurrent requests | Resource-intensive operations |

## Installation

Rate limiting is included in the following packages:

- `Mvp24Hours.Core` - Base abstractions and provider
- `Mvp24Hours.Infrastructure.Pipe` - Pipeline middleware
- `Mvp24Hours.Infrastructure.RabbitMQ` - Consumer/Publisher filters
- `Mvp24Hours.WebAPI` - HTTP middleware (already implemented)

## Core Abstractions

### IRateLimiterProvider

The `IRateLimiterProvider` interface provides rate limiters for different keys/partitions:

```csharp
public interface IRateLimiterProvider : IDisposable
{
    RateLimiter GetRateLimiter(string key, NativeRateLimiterOptions options);
    
    ValueTask<RateLimitLease> AcquireAsync(
        string key,
        NativeRateLimiterOptions options,
        int permitCount = 1,
        CancellationToken cancellationToken = default);
    
    bool TryRemoveRateLimiter(string key);
}
```

### NativeRateLimiterOptions

Configuration options for rate limiters:

```csharp
var options = new NativeRateLimiterOptions
{
    Algorithm = RateLimitingAlgorithm.SlidingWindow,
    PermitLimit = 100,
    Window = TimeSpan.FromMinutes(1),
    SegmentsPerWindow = 4,
    QueueLimit = 10,
    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
};
```

### Factory Methods

```csharp
// Fixed Window
var fixedWindow = NativeRateLimiterOptions.FixedWindow(
    permitLimit: 100,
    window: TimeSpan.FromMinutes(1));

// Sliding Window
var slidingWindow = NativeRateLimiterOptions.SlidingWindow(
    permitLimit: 100,
    window: TimeSpan.FromMinutes(1),
    segmentsPerWindow: 4);

// Token Bucket
var tokenBucket = NativeRateLimiterOptions.TokenBucket(
    tokenLimit: 100,
    replenishmentPeriod: TimeSpan.FromSeconds(10),
    tokensPerPeriod: 10);

// Concurrency
var concurrency = NativeRateLimiterOptions.Concurrency(
    permitLimit: 10,
    queueLimit: 5);
```

## WebAPI Rate Limiting

Rate limiting for WebAPI is already implemented using the built-in .NET rate limiter. See the WebAPI documentation for configuration details.

## Pipeline Rate Limiting

### Basic Configuration

```csharp
services.AddPipelineRateLimiting(options =>
{
    options.DefaultKey = "pipeline_default";
    options.DefaultRateLimiterOptions = NativeRateLimiterOptions.SlidingWindow(
        permitLimit: 100,
        window: TimeSpan.FromMinutes(1));
    options.OnRateLimited = (key, retryAfter) =>
    {
        Console.WriteLine($"Rate limit exceeded for {key}. Retry after: {retryAfter}");
    };
});
```

### Algorithm-Specific Extensions

```csharp
// Sliding Window
services.AddPipelineRateLimitingSlidingWindow(
    permitLimit: 100,
    window: TimeSpan.FromMinutes(1),
    segmentsPerWindow: 4);

// Token Bucket
services.AddPipelineRateLimitingTokenBucket(
    tokenLimit: 100,
    replenishmentPeriod: TimeSpan.FromSeconds(10),
    tokensPerPeriod: 10);

// Concurrency
services.AddPipelineRateLimitingConcurrency(
    permitLimit: 10,
    queueLimit: 5);
```

### Operation-Specific Rate Limiting

Implement `IRateLimitedOperation` on your pipeline operations:

```csharp
public class MyRateLimitedOperation : OperationBase, IRateLimitedOperation
{
    public string RateLimiterKey => "my_operation";
    public RateLimitingAlgorithm Algorithm => RateLimitingAlgorithm.TokenBucket;
    public int PermitLimit => 50;
    public TimeSpan Window => TimeSpan.FromSeconds(30);
    public int SegmentsPerWindow => 4;
    public TimeSpan ReplenishmentPeriod => TimeSpan.FromSeconds(5);
    public int TokensPerPeriod => 5;
    public bool AutoReplenishment => true;
    public int QueueLimit => 10;
    public QueueProcessingOrder QueueProcessingOrder => QueueProcessingOrder.OldestFirst;
    public TimeSpan? QueueTimeout => TimeSpan.FromSeconds(5);

    public void OnRateLimited(TimeSpan? retryAfter)
    {
        // Handle rate limit notification
    }

    public override async Task ExecuteAsync(IPipelineMessage input)
    {
        // Operation logic
    }
}
```

## RabbitMQ Rate Limiting

### Consumer Rate Limiting

```csharp
services.AddRabbitMQConsumerRateLimiting(options =>
{
    options.KeyMode = RateLimitKeyMode.ByQueue;
    options.ExceededBehavior = RateLimitExceededBehavior.Retry;
    options.DefaultRetryDelay = TimeSpan.FromSeconds(5);
    options.DefaultRateLimiterOptions = NativeRateLimiterOptions.SlidingWindow(
        permitLimit: 100,
        window: TimeSpan.FromSeconds(1));
});
```

### Rate Limit Key Modes

| Mode | Description |
|------|-------------|
| `ByQueue` | Rate limit per queue |
| `ByMessageType` | Rate limit per message type |
| `ByExchange` | Rate limit per exchange |
| `ByRoutingKey` | Rate limit per routing key |
| `ByConsumerTag` | Rate limit per consumer tag |
| `Global` | Global rate limit for all consumers |

### Exceeded Behaviors

| Behavior | Description |
|----------|-------------|
| `Throw` | Throws `RateLimitExceededException` |
| `Retry` | Requests a retry with delay |
| `DeadLetter` | Sends message to dead letter queue |
| `Skip` | Skips the message (acknowledges without processing) |

### Publisher Rate Limiting

```csharp
services.AddRabbitMQPublisherRateLimiting(options =>
{
    options.KeyMode = PublishRateLimitKeyMode.Global;
    options.WaitWhenExceeded = true; // Wait and retry instead of throwing
    options.DefaultRateLimiterOptions = NativeRateLimiterOptions.TokenBucket(
        tokenLimit: 1000,
        replenishmentPeriod: TimeSpan.FromSeconds(1),
        tokensPerPeriod: 100);
});
```

### Combined Consumer and Publisher

```csharp
services.AddRabbitMQRateLimiting(
    configureConsumeOptions: consume =>
    {
        consume.KeyMode = RateLimitKeyMode.ByQueue;
        consume.ExceededBehavior = RateLimitExceededBehavior.Retry;
    },
    configurePublishOptions: publish =>
    {
        publish.KeyMode = PublishRateLimitKeyMode.Global;
    });
```

### Type-Specific Rate Limiting

```csharp
services.AddRabbitMQConsumerRateLimiting(options =>
{
    // Default options
    options.DefaultRateLimiterOptions = NativeRateLimiterOptions.SlidingWindow(100);

    // More restrictive for specific message types
    options.TypeSpecificOptions[typeof(HighVolumeMessage)] = 
        NativeRateLimiterOptions.TokenBucket(10, TimeSpan.FromSeconds(1), 1);
    
    options.TypeSpecificOptions[typeof(CriticalMessage)] = 
        NativeRateLimiterOptions.Concurrency(5);
});
```

## Generic Rate Limiting

Use the core extensions for any scenario:

```csharp
// Register the provider
services.AddNativeRateLimiting();

// Pre-configure specific rate limiters
services.AddSlidingWindowRateLimiter("api_calls", permitLimit: 100);
services.AddTokenBucketRateLimiter("batch_processing", tokenLimit: 50);
services.AddConcurrencyRateLimiter("heavy_operations", permitLimit: 5);
```

### Manual Usage

```csharp
public class MyService
{
    private readonly IRateLimiterProvider _rateLimiterProvider;

    public MyService(IRateLimiterProvider rateLimiterProvider)
    {
        _rateLimiterProvider = rateLimiterProvider;
    }

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        var options = NativeRateLimiterOptions.SlidingWindow(100);
        
        using var lease = await _rateLimiterProvider.AcquireAsync(
            "my_operation",
            options,
            permitCount: 1,
            cancellationToken);

        if (!lease.IsAcquired)
        {
            var retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var retry)
                ? retry
                : TimeSpan.FromSeconds(60);
            
            throw RateLimitExceededException.ForKey("my_operation", retryAfter);
        }

        // Proceed with operation
    }
}
```

## Exception Handling

The `RateLimitExceededException` provides detailed information:

```csharp
try
{
    await ProcessAsync();
}
catch (RateLimitExceededException ex)
{
    Console.WriteLine($"Rate limit exceeded for: {ex.RateLimiterKey}");
    Console.WriteLine($"Retry after: {ex.RetryAfter}");
    Console.WriteLine($"Permit limit: {ex.PermitLimit}");
    Console.WriteLine($"Error code: {ex.ErrorCode}");
    
    // HTTP 429 response with Retry-After header
    return Results.Problem(
        title: "Too Many Requests",
        statusCode: 429,
        extensions: new Dictionary<string, object?>
        {
            ["retryAfter"] = ex.RetryAfter?.TotalSeconds
        });
}
```

## Best Practices

1. **Choose the Right Algorithm**
   - Use **SlidingWindow** for most API rate limiting
   - Use **TokenBucket** when bursts are acceptable
   - Use **Concurrency** for resource-intensive operations
   - Use **FixedWindow** only for simple cases

2. **Key Selection**
   - Use fine-grained keys (per-user, per-tenant) for fairness
   - Use coarse-grained keys (global) for system protection

3. **Queue Configuration**
   - Set `QueueLimit > 0` to allow queuing instead of immediate rejection
   - Use `QueueTimeout` to prevent indefinite waiting

4. **Monitoring**
   - Log rate limit events for debugging
   - Track rate limit metrics for capacity planning
   - Alert on excessive rate limiting

5. **Graceful Degradation**
   - Return proper `Retry-After` headers
   - Implement exponential backoff in clients
   - Consider fallback strategies

## Migration from Custom Implementations

If you have custom rate limiting implementations, migrate to the native API:

```csharp
// Before (custom implementation)
public class CustomRateLimiter
{
    public bool TryAcquire() { /* custom logic */ }
}

// After (native implementation)
services.AddNativeRateLimiting();

// Use the provider
public class MyService
{
    private readonly IRateLimiterProvider _provider;
    
    public async Task<bool> TryAcquireAsync(CancellationToken ct)
    {
        using var lease = await _provider.AcquireAsync(
            "my_key",
            NativeRateLimiterOptions.SlidingWindow(100),
            cancellationToken: ct);
        
        return lease.IsAcquired;
    }
}
```

## See Also

- [HTTP Resilience](http-resilience.md)
- [Generic Resilience](generic-resilience.md)
- [Microsoft.Extensions.RateLimiting Documentation](https://learn.microsoft.com/en-us/dotnet/core/extensions/http-ratelimiter)

