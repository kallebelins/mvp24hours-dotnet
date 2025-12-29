# HTTP Resilience with Microsoft.Extensions.Http.Resilience

> **Version**: .NET 9+ | **Package**: `Microsoft.Extensions.Http.Resilience`

## Overview

.NET 9 introduces native HTTP resilience through the `Microsoft.Extensions.Http.Resilience` package. This replaces manual Polly configurations with a simplified, integrated API that provides:

- **Simplified Configuration**: Use `IOptions` pattern for configuration
- **Built-in OpenTelemetry Integration**: Automatic tracing and metrics
- **Native Metrics Support**: Prometheus-compatible metrics out of the box
- **Better Performance**: Uses Polly v8 with improved performance
- **Reduced Boilerplate**: Less code to configure resilience strategies

## Standard Resilience Handler

The standard resilience handler includes four layers of protection:

1. **Total Request Timeout** (30s default) - Overall timeout including retries
2. **Retry** (3 attempts with exponential backoff) - Automatic retry on transient failures
3. **Circuit Breaker** (failure ratio based) - Prevents cascading failures
4. **Attempt Timeout** (10s per attempt) - Timeout for each individual attempt

### Basic Usage

```csharp
// Add HTTP client with standard resilience
services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
}).AddStandardResilienceHandler();
```

### Using Mvp24Hours Extensions

```csharp
using Mvp24Hours.Infrastructure.Http.Resilience;

// Simple approach with standard resilience
services.AddHttpClientWithStandardResilience("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});

// With custom options
services.AddHttpClientWithStandardResilience("MyApi",
    client => client.BaseAddress = new Uri("https://api.example.com"),
    options =>
    {
        options.Retry.MaxRetryAttempts = 5;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(60);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
    });
```

## Custom Resilience Configuration

For advanced scenarios, use the custom resilience handler:

```csharp
services.AddHttpClientWithCustomResilience("MyApi", "custom-pipeline",
    client => client.BaseAddress = new Uri("https://api.example.com"),
    builder =>
    {
        builder.AddRetry(new HttpRetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true
        });
        
        builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 10,
            BreakDuration = TimeSpan.FromSeconds(60)
        });
        
        builder.AddTimeout(TimeSpan.FromSeconds(30));
    });
```

## Fluent Builder API

Mvp24Hours provides a fluent builder for common scenarios:

```csharp
services.AddHttpClient("MyApi")
    .AddMvpResilience(builder => builder
        .WithOptions(NativeResilienceOptions.HighAvailability)
        .OnRetry((args, delay) => 
            logger.LogWarning("Retry attempt {Attempt} after {Delay}", 
                args.AttemptNumber, delay))
        .OnCircuitBreak(args => 
            logger.LogError("Circuit opened due to {Exception}", 
                args.Outcome.Exception?.Message)));
```

### Preset Options

Use preset options for common scenarios:

```csharp
// High availability - more retries, longer timeouts
services.AddHttpClient("CriticalApi")
    .AddMvpResilience(NativeResilienceOptions.HighAvailability);

// Low latency - fewer retries, shorter timeouts
services.AddHttpClient("RealTimeApi")
    .AddMvpResilience(NativeResilienceOptions.LowLatency);

// Batch processing - tolerance for failures
services.AddHttpClient("BatchApi")
    .AddMvpResilience(NativeResilienceOptions.BatchProcessing);

// Testing - no resilience
services.AddHttpClient("TestApi")
    .AddMvpResilience(NativeResilienceOptions.Disabled);
```

## Typed HTTP Clients

For typed HTTP clients:

```csharp
// Using Mvp24Hours typed client with resilience
services.AddMvpTypedHttpClient<IMyApi>(options =>
{
    options.BaseAddress = new Uri("https://api.example.com");
    options.Timeout = TimeSpan.FromSeconds(30);
}).AddStandardResilienceHandler();

// Or with preset options
services.AddTypedHttpClientWithStandardResilience<IMyApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
});
```

## Migration from Legacy API

### Before (Deprecated)

```csharp
// ❌ Old approach - DEPRECATED
services.AddHttpClient("MyApi")
    .AddPolicyHandler(HttpPolicyHelper.GetRetryPolicy(HttpStatusCode.TooManyRequests, 3))
    .AddPolicyHandler(HttpPolicyHelper.GetCircuitBreakerPolicy(HttpStatusCode.ServiceUnavailable));

// Or using the old extensions
services.AddHttpClientWithPolly("MyApi", builder =>
{
    builder.AddRetryPolicy(o => o.MaxRetries = 3);
    builder.AddCircuitBreakerPolicy(o => o.BreakDuration = TimeSpan.FromSeconds(30));
});
```

### After (Recommended)

```csharp
// ✅ New approach - Recommended
services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
}).AddStandardResilienceHandler();

// Or with custom configuration
services.AddHttpClient("MyApi")
    .AddMvpResilience(builder => builder
        .ConfigureOptions(o =>
        {
            o.MaxRetryAttempts = 3;
            o.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30);
        }));
```

## Configuration via appsettings.json

Configure resilience options via configuration:

```json
{
  "HttpClients": {
    "MyApi": {
      "BaseAddress": "https://api.example.com",
      "Resilience": {
        "TotalRequestTimeout": "00:02:00",
        "Retry": {
          "MaxRetryAttempts": 5,
          "Delay": "00:00:02"
        },
        "CircuitBreaker": {
          "FailureRatio": 0.1,
          "BreakDuration": "00:00:30"
        }
      }
    }
  }
}
```

```csharp
services.AddHttpClient("MyApi")
    .AddStandardResilienceHandler(options =>
    {
        configuration.GetSection("HttpClients:MyApi:Resilience").Bind(options);
    });
```

## Observability

The native API automatically integrates with OpenTelemetry:

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddHttpClientInstrumentation()
        .AddSource("Polly"))
    .WithMetrics(metrics => metrics
        .AddHttpClientInstrumentation()
        .AddMeter("Polly"));
```

### Available Metrics

- `http.client.request.duration` - Request duration histogram
- `polly.resilience.pipeline.duration` - Pipeline execution duration
- `polly.strategy.attempt_count` - Number of attempts per strategy

## Best Practices

1. **Use Standard Handler by Default**: Start with `AddStandardResilienceHandler()` and customize only when needed

2. **Configure Appropriate Timeouts**: Set timeouts based on your SLA requirements

3. **Monitor Circuit Breaker State**: Use callbacks to log circuit breaker state changes

4. **Use Jitter**: Enable jitter to prevent thundering herd problems

5. **Test Resilience**: Test your resilience configuration under failure conditions

## See Also

- [Microsoft.Extensions.Http.Resilience Documentation](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [Polly v8 Documentation](https://www.thepollyproject.org/)
- [OpenTelemetry Integration](../observability/tracing.md)

