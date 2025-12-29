# Generic Resilience with Microsoft.Extensions.Resilience

> **New in Mvp24Hours for .NET 9**: Native resilience using `Microsoft.Extensions.Resilience` and Polly v8.

## Overview

Starting with .NET 9, Mvp24Hours adopts the native `Microsoft.Extensions.Resilience` package for generic (non-HTTP) resilience operations. This replaces custom implementations of retry policies, circuit breakers, and timeouts with industry-standard, well-tested components.

## Benefits of Native Resilience

| Feature | Custom Implementation | Native (Microsoft.Extensions.Resilience) |
|---------|----------------------|------------------------------------------|
| **Configuration** | Custom options classes | IOptions pattern + IConfiguration |
| **Telemetry** | Manual integration | Automatic OpenTelemetry integration |
| **DI Integration** | Custom registrations | Built-in with Keyed Services |
| **Performance** | Variable | Optimized for .NET 9 |
| **Maintenance** | Mvp24Hours team | Microsoft + Polly team |

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    Microsoft.Extensions.Resilience               │
│                         (Polly v8 foundation)                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │   Timeout    │  │Circuit Breaker│  │       Retry         │  │
│  │  (outermost) │→ │              │→ │    (innermost)       │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│                    Mvp24Hours Integration Layer                  │
│                                                                  │
│  ┌────────────────┐ ┌────────────────┐ ┌────────────────────┐  │
│  │  Database      │ │    MongoDB     │ │     Pipeline       │  │
│  │  Resilience    │ │   Resilience   │ │    Resilience      │  │
│  └────────────────┘ └────────────────┘ └────────────────────┘  │
│                                                                  │
│  ┌────────────────┐ ┌────────────────┐                         │
│  │ CQRS Behavior  │ │    Generic     │                         │
│  │   Resilience   │ │   Operations   │                         │
│  └────────────────┘ └────────────────┘                         │
└─────────────────────────────────────────────────────────────────┘
```

## Quick Start

### 1. Generic Operations

```csharp
// Register resilience pipeline
services.AddNativeResilience(options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 3;
    options.EnableCircuitBreaker = true;
    options.EnableTimeout = true;
    options.TimeoutDuration = TimeSpan.FromSeconds(30);
});

// Use in your code
public class MyService
{
    private readonly INativeResiliencePipeline _pipeline;
    
    public MyService(INativeResiliencePipeline pipeline)
    {
        _pipeline = pipeline;
    }
    
    public async Task<string> GetDataAsync(CancellationToken ct)
    {
        return await _pipeline.ExecuteTaskAsync(async token =>
        {
            // Your operation here
            return await externalService.CallAsync(token);
        }, ct);
    }
}
```

### 2. Database Operations (EF Core)

```csharp
// Register database resilience
services.AddNativeDbResilience(options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 5;
    options.EnableCircuitBreaker = true;
});

// Use with Keyed Services
public class CustomerRepository
{
    private readonly ResiliencePipeline _pipeline;
    
    public CustomerRepository(
        [FromKeyedServices("database")] ResiliencePipeline pipeline,
        ApplicationDbContext dbContext)
    {
        _pipeline = pipeline;
        _dbContext = dbContext;
    }
    
    public async Task<Customer?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            return await _dbContext.Customers.FindAsync(new object[] { id }, token);
        }, ct);
    }
}
```

### 3. MongoDB Operations

```csharp
// Register MongoDB resilience
services.AddNativeMongoDbResilience(options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 3;
    options.EnableCircuitBreaker = true;
});

// Use in repository
public class ProductRepository
{
    private readonly ResiliencePipeline _pipeline;
    private readonly IMongoCollection<Product> _collection;
    
    public ProductRepository(
        [FromKeyedServices("mongodb")] ResiliencePipeline pipeline,
        IMongoDatabase database)
    {
        _pipeline = pipeline;
        _collection = database.GetCollection<Product>("products");
    }
    
    public async Task<Product?> GetByIdAsync(string id, CancellationToken ct)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            return await _collection.Find(p => p.Id == id).FirstOrDefaultAsync(token);
        }, ct);
    }
}
```

### 4. Pipeline Operations

```csharp
// Register pipeline resilience
services.AddNativePipelineResilience(options =>
{
    options.EnableRetry = true;
    options.RetryMaxAttempts = 3;
    options.EnableCircuitBreaker = true;
});
```

### 5. CQRS Mediator

```csharp
// Register CQRS resilience behavior
services.AddNativeCqrsResilience(options =>
{
    options.ApplyToAllRequests = true; // Apply to all requests
    options.EnableRetry = true;
    options.EnableCircuitBreaker = true;
});

// Or opt-in per request
public class GetCustomerQuery : IMediatorQuery<Customer>, INativeResilient
{
    public int CustomerId { get; set; }
    
    // Optional: custom options for this request
    public NativeCqrsResilienceOptions? ResilienceOptions => new()
    {
        RetryMaxAttempts = 5,
        TimeoutDuration = TimeSpan.FromSeconds(10)
    };
}
```

## Presets

### Generic Presets

```csharp
// High availability (more retries, longer timeouts)
services.AddNativeResilience(NativeResilienceOptions.HighAvailability);

// Low latency (fewer retries, shorter timeouts)
services.AddNativeResilience(NativeResilienceOptions.LowLatency);

// Batch processing (many retries, no timeout)
services.AddNativeResilience(NativeResilienceOptions.BatchProcessing);
```

### Database Presets

```csharp
// SQL Server optimized
services.AddNativeDbResilience("sqlserver", NativeDbResilienceOptions.SqlServer);

// PostgreSQL optimized
services.AddNativeDbResilience("postgres", NativeDbResilienceOptions.PostgreSql);

// MySQL optimized
services.AddNativeDbResilience("mysql", NativeDbResilienceOptions.MySql);
```

### MongoDB Presets

```csharp
// Replica set optimized
services.AddNativeMongoDbResilience("replica", NativeMongoDbResilienceOptions.ReplicaSet);

// Sharded cluster optimized
services.AddNativeMongoDbResilience("sharded", NativeMongoDbResilienceOptions.ShardedCluster);
```

## Configuration via appsettings.json

```json
{
  "Resilience": {
    "Database": {
      "EnableRetry": true,
      "RetryMaxAttempts": 3,
      "RetryDelayMs": 500,
      "EnableCircuitBreaker": true,
      "CircuitBreakerFailureRatio": 0.5,
      "EnableTimeout": true,
      "TimeoutSeconds": 30
    }
  }
}
```

```csharp
services.AddNativeDbResilience(options =>
{
    configuration.GetSection("Resilience:Database").Bind(options);
});
```

## Migration from Legacy Implementations

### Before (Deprecated)

```csharp
// ❌ Deprecated - will be removed in future versions
services.AddSingleton<IRetryPolicy<Customer>>(sp =>
{
    var options = new RetryOptions { MaxRetries = 3 };
    return new RetryPolicy<Customer>(options);
});

var result = await retryPolicy.ExecuteAsync(async ct =>
{
    return await GetCustomerAsync(id, ct);
}, cancellationToken);
```

### After (Native)

```csharp
// ✅ Modern approach using Microsoft.Extensions.Resilience
services.AddNativeResilience<Customer>(options =>
{
    options.RetryMaxAttempts = 3;
});

var result = await pipeline.ExecuteAsync(async ct =>
{
    return await GetCustomerAsync(id, ct);
}, cancellationToken);
```

## Deprecated Classes

The following classes are now deprecated and will be removed in a future major version:

| Deprecated Class | Replacement |
|-----------------|-------------|
| `MvpExecutionStrategy` | `AddNativeDbResilience()` |
| `MongoDbResiliencyPolicy` | `AddNativeMongoDbResilience()` |
| `RetryPipelineMiddleware` | `AddNativePipelineResilience()` |
| `CircuitBreakerPipelineMiddleware` | `AddNativePipelineResilience()` |
| `RetryPolicy<T>` | `INativeResiliencePipeline<T>` |
| `CircuitBreaker<T>` | `INativeResiliencePipeline<T>` |
| `RetryBehavior` | `NativeResilienceBehavior` |
| `CircuitBreakerBehavior` | `NativeResilienceBehavior` |
| `TimeoutBehavior` | `NativeResilienceBehavior` |

## Advanced Configuration

### Custom Exception Handling

```csharp
services.AddNativeResilience(options =>
{
    options.ShouldRetryOnException = ex =>
    {
        // Retry on specific exceptions only
        return ex is TimeoutException or 
               TransientException or
               DbUpdateConcurrencyException;
    };
    
    options.OnRetry = (ex, attempt, delay) =>
    {
        // Custom logging or metrics
        logger.LogWarning(ex, 
            "Retry {Attempt} after {Delay}ms", 
            attempt, 
            delay.TotalMilliseconds);
    };
});
```

### Using ResiliencePipeline Directly

For advanced scenarios, you can use Polly's `ResiliencePipelineBuilder` directly:

```csharp
services.AddResiliencePipeline("custom", builder =>
{
    builder
        .AddTimeout(TimeSpan.FromSeconds(30))
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        });
});
```

## Telemetry

Native resilience automatically integrates with OpenTelemetry:

```csharp
services.AddOpenTelemetry()
    .WithTracing(builder =>
    {
        builder.AddSource("Polly"); // Polly's activity source
    })
    .WithMetrics(builder =>
    {
        builder.AddMeter("Polly"); // Polly's meter
    });
```

## See Also

- [HTTP Resilience (Microsoft.Extensions.Http.Resilience)](http-resilience.md)
- [Rate Limiting (.NET 9)](rate-limiting.md)
- [Polly v8 Documentation](https://www.pollydocs.org/)
- [Microsoft.Extensions.Resilience Documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/)

