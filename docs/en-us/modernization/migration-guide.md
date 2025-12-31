# Migration Guide to Native .NET 9 APIs

This guide provides step-by-step instructions for migrating from legacy Mvp24Hours implementations to native .NET 9 APIs.

## Overview

Mvp24Hours has adopted native .NET 9 APIs to reduce custom code, improve performance, and align with industry standards. This guide covers:

1. **Identifying deprecated code** - Finding legacy implementations
2. **Migration strategies** - Step-by-step migration paths
3. **Testing changes** - Validating migrations work correctly
4. **Rollback procedures** - Reverting if issues arise

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Migration Path Overview                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Legacy (Deprecated)                    Native (.NET 9)                     │
│  ───────────────────                    ────────────────                    │
│                                                                             │
│  TelemetryHelper        ───────────►    ILogger + OpenTelemetry             │
│  HttpClientExtensions   ───────────►    Microsoft.Extensions.Http.Resilience│
│  MvpExecutionStrategy   ───────────►    Microsoft.Extensions.Resilience     │
│  MultiLevelCache        ───────────►    HybridCache                         │
│  RetryPipelineMiddleware───────────►    NativePipelineResilienceMiddleware  │
│  Custom Rate Limiting   ───────────►    System.Threading.RateLimiting       │
│  System.Timers.Timer    ───────────►    TimeProvider + PeriodicTimer        │
│  Custom Options Valid.  ───────────►    IValidateOptions<T> + ValidateOnStart│
│  Swashbuckle           ───────────►    Microsoft.AspNetCore.OpenAPI         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 1. Telemetry Migration

### From: `TelemetryHelper`
### To: `ILogger` + OpenTelemetry

#### Before (Deprecated)

```csharp
public class MyService
{
    public void DoWork()
    {
        TelemetryHelper.Execute(TelemetryLevels.Information, "Starting work");
        
        try
        {
            // ... work
            TelemetryHelper.Execute(TelemetryLevels.Information, "Work completed");
        }
        catch (Exception ex)
        {
            TelemetryHelper.Execute(TelemetryLevels.Error, $"Error: {ex.Message}");
            throw;
        }
    }
}
```

#### After (Native)

```csharp
public class MyService
{
    private readonly ILogger<MyService> _logger;

    public MyService(ILogger<MyService> logger)
    {
        _logger = logger;
    }

    public void DoWork()
    {
        _logger.LogInformation("Starting work");
        
        try
        {
            // ... work
            _logger.LogInformation("Work completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while doing work");
            throw;
        }
    }
}
```

#### Configuration

```csharp
// Program.cs
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

---

## 2. HTTP Resilience Migration

### From: `HttpClientExtensions` / `HttpPolicyHelper`
### To: `Microsoft.Extensions.Http.Resilience`

#### Before (Deprecated)

```csharp
// Old way with custom Polly policies
services.AddHttpClient("MyApi")
    .AddPolicyHandler(HttpPolicyHelper.GetRetryPolicy(3))
    .AddPolicyHandler(HttpPolicyHelper.GetCircuitBreakerPolicy(5, TimeSpan.FromSeconds(30)));
```

#### After (Native)

```csharp
// New way with native resilience
services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddMvpStandardResilience()
// Or with custom configuration:
.AddMvpResilience(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
});
```

#### Using Presets

```csharp
// High availability preset (more retries, longer timeouts)
services.AddHttpClient("CriticalApi")
    .AddMvpResilience(NativeResilienceOptions.HighAvailability);

// Low latency preset (fewer retries, shorter timeouts)
services.AddHttpClient("FastApi")
    .AddMvpResilience(NativeResilienceOptions.LowLatency);
```

---

## 3. Database Resilience Migration

### From: `MvpExecutionStrategy`
### To: `Microsoft.Extensions.Resilience`

#### Before (Deprecated)

```csharp
// Old custom execution strategy
services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.ExecutionStrategy(c => new MvpExecutionStrategy(c));
    });
});
```

#### After (Native)

```csharp
// New native resilience
services.AddNativeDbResilience(options =>
{
    options.MaxRetryAttempts = 3;
    options.BaseDelay = TimeSpan.FromMilliseconds(100);
});

services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});
```

---

## 4. Cache Migration

### From: `MultiLevelCache`
### To: `HybridCache`

#### Before (Deprecated)

```csharp
// Old multi-level cache
services.AddMultiLevelCache(options =>
{
    options.L1Options.SizeLimit = 1000;
    options.L2ConnectionString = "redis:6379";
});

// Usage
var item = await multiLevelCache.GetOrSetAsync(
    "key",
    async () => await LoadDataAsync(),
    TimeSpan.FromMinutes(5));
```

#### After (Native)

```csharp
// New HybridCache
services.AddMvpHybridCache(options =>
{
    options.DefaultEntryOptions.Expiration = TimeSpan.FromMinutes(5);
    options.DefaultEntryOptions.LocalCacheExpiration = TimeSpan.FromMinutes(1);
});

// Usage
var item = await hybridCache.GetOrCreateAsync(
    "key",
    async cancel => await LoadDataAsync(cancel),
    new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    });
```

#### Tag-Based Invalidation

```csharp
// Register with tags
await hybridCache.GetOrCreateAsync(
    "user:123",
    async cancel => await LoadUserAsync(123, cancel),
    tags: new[] { "users", "user:123" });

// Invalidate by tag
await tagManager.InvalidateByTagAsync("users");
```

---

## 5. Pipeline Resilience Migration

### From: `RetryPipelineMiddleware` / `CircuitBreakerPipelineMiddleware`
### To: `NativePipelineResilienceMiddleware`

#### Before (Deprecated)

```csharp
// Old custom middleware
services.AddPipelineResilience(options =>
{
    options.RetryCount = 3;
    options.RetryDelay = TimeSpan.FromSeconds(1);
    options.CircuitBreakerThreshold = 5;
});
```

#### After (Native)

```csharp
// New native resilience middleware
services.AddNativePipelineResilience(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.CircuitBreaker.FailureRatio = 0.5;
});
```

---

## 6. Rate Limiting Migration

### From: Custom Rate Limiting
### To: `System.Threading.RateLimiting`

#### Before (Custom)

```csharp
// Custom rate limiting implementation
services.AddCustomRateLimiting(options =>
{
    options.PermitLimit = 100;
    options.Window = TimeSpan.FromMinutes(1);
});
```

#### After (Native)

```csharp
// Native rate limiting
services.AddNativeRateLimiting(options =>
{
    options.DefaultPolicy = NativeRateLimiterOptions.FixedWindow(
        permitLimit: 100,
        window: TimeSpan.FromMinutes(1));
});

// For pipeline operations
services.AddPipelineRateLimiting();

// For RabbitMQ consumers
services.AddRabbitMQRateLimiting(options =>
{
    options.ConsumerLimit = NativeRateLimiterOptions.SlidingWindow(
        permitLimit: 50,
        window: TimeSpan.FromSeconds(10),
        segmentsPerWindow: 5);
});
```

---

## 7. Time Abstraction Migration

### From: `DateTime.Now` / `DateTime.UtcNow`
### To: `TimeProvider`

#### Before (Direct Usage)

```csharp
public class MyService
{
    public void DoWork()
    {
        var now = DateTime.UtcNow;
        var deadline = now.AddHours(1);
        // ...
    }
}
```

#### After (TimeProvider)

```csharp
public class MyService
{
    private readonly TimeProvider _timeProvider;

    public MyService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public void DoWork()
    {
        var now = _timeProvider.GetUtcNow();
        var deadline = now.AddHours(1);
        // ...
    }
}

// Registration
services.AddTimeProvider();

// Testing
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
services.ReplaceTimeProvider(fakeTime);
```

---

## 8. Timer Migration

### From: `System.Timers.Timer`
### To: `PeriodicTimer`

#### Before (Old Timer)

```csharp
public class BackgroundWorker : BackgroundService
{
    private Timer _timer;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _timer = new Timer(5000);
        _timer.Elapsed += OnTimerElapsed;
        _timer.Start();
        return Task.CompletedTask;
    }

    private void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
        // Do work
    }
}
```

#### After (PeriodicTimer)

```csharp
public class BackgroundWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Do work
        }
    }
}
```

---

## 9. Options Validation Migration

### From: Manual Validation
### To: `IValidateOptions<T>` + `ValidateOnStart`

#### Before (Manual)

```csharp
public class MyOptions
{
    public string ConnectionString { get; set; }
    public int MaxRetries { get; set; }
}

// Validation in startup
var options = configuration.GetSection("MyOptions").Get<MyOptions>();
if (string.IsNullOrEmpty(options.ConnectionString))
    throw new InvalidOperationException("ConnectionString is required");
```

#### After (Native)

```csharp
public class MyOptions
{
    [Required]
    public string ConnectionString { get; set; } = default!;
    
    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;
}

// Registration with validation
services.AddOptionsWithValidation<MyOptions>("MyOptions");

// Custom validator
public class MyOptionsValidator : OptionsValidatorBase<MyOptions>
{
    protected override void Validate(OptionsValidationContext<MyOptions> context)
    {
        context
            .EnsureNotNullOrEmpty(o => o.ConnectionString, "Connection string is required")
            .EnsureInRange(o => o.MaxRetries, 1, 10, "MaxRetries must be between 1 and 10");
    }
}

services.AddOptionsWithValidation<MyOptions, MyOptionsValidator>("MyOptions");
```

---

## 10. OpenAPI Migration

### From: Swashbuckle
### To: `Microsoft.AspNetCore.OpenAPI`

#### Before (Swashbuckle)

```csharp
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
});

app.UseSwagger();
app.UseSwaggerUI();
```

#### After (Native OpenAPI)

```csharp
services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "v1";
    options.Description = "My API Description";
    options.EnableSecuritySchemes = true;
});

app.MapMvp24HoursNativeOpenApi();

// Still use Swagger UI for visualization
app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "My API v1"));
```

---

## Testing Migrations

### 1. Unit Tests

```csharp
[Fact]
public async Task Should_Use_Native_Resilience()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddHttpClient("test")
        .AddMvpStandardResilience();
    
    var provider = services.BuildServiceProvider();
    var factory = provider.GetRequiredService<IHttpClientFactory>();
    
    // Act
    var client = factory.CreateClient("test");
    
    // Assert
    Assert.NotNull(client);
}
```

### 2. Integration Tests

```csharp
[Fact]
public async Task Should_Retry_On_Transient_Failure()
{
    // Arrange
    var handler = new TestHttpMessageHandler();
    handler.SetupSequence(
        () => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
        () => new HttpResponseMessage(HttpStatusCode.OK));
    
    var services = new ServiceCollection();
    services.AddHttpClient("test")
        .AddMvpResilience(o => o.Retry.MaxRetryAttempts = 2)
        .ConfigurePrimaryHttpMessageHandler(() => handler);
    
    var provider = services.BuildServiceProvider();
    var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("test");
    
    // Act
    var response = await client.GetAsync("http://test.com/api");
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(2, handler.RequestCount);
}
```

---

## Rollback Procedures

### 1. Feature Flags

```csharp
services.AddFeatureManagement();

if (featureManager.IsEnabled("UseNativeResilience"))
{
    services.AddHttpClient("api").AddMvpStandardResilience();
}
else
{
    // Legacy
    services.AddHttpClient("api")
        .AddPolicyHandler(HttpPolicyHelper.GetRetryPolicy(3));
}
```

### 2. Configuration-Based Switching

```json
{
  "Features": {
    "UseNativeApis": true,
    "UseHybridCache": true,
    "UseNativeOpenApi": false
  }
}
```

```csharp
var useNative = configuration.GetValue<bool>("Features:UseNativeApis");
if (useNative)
{
    services.AddMvpStandardResilience();
}
```

---

## Checklist

Use this checklist to track your migration progress:

- [ ] **Telemetry**
  - [ ] Replace `TelemetryHelper` with `ILogger`
  - [ ] Configure OpenTelemetry exporters
  - [ ] Update all logging calls

- [ ] **HTTP Resilience**
  - [ ] Replace `HttpClientExtensions` with native resilience
  - [ ] Update HttpClient registrations
  - [ ] Test retry and circuit breaker behavior

- [ ] **Database Resilience**
  - [ ] Replace `MvpExecutionStrategy` with native resilience
  - [ ] Update DbContext configurations
  - [ ] Test transient failure handling

- [ ] **Caching**
  - [ ] Replace `MultiLevelCache` with `HybridCache`
  - [ ] Update cache access patterns
  - [ ] Configure tag-based invalidation

- [ ] **Pipeline**
  - [ ] Replace custom middleware with native
  - [ ] Update pipeline configurations
  - [ ] Test resilience behavior

- [ ] **Rate Limiting**
  - [ ] Replace custom rate limiting
  - [ ] Configure per-endpoint policies
  - [ ] Test rate limit enforcement

- [ ] **Time & Timers**
  - [ ] Replace `DateTime.Now` with `TimeProvider`
  - [ ] Replace `System.Timers.Timer` with `PeriodicTimer`
  - [ ] Update tests to use `FakeTimeProvider`

- [ ] **Configuration**
  - [ ] Add `IValidateOptions<T>` validators
  - [ ] Enable `ValidateOnStart()`
  - [ ] Test configuration validation

- [ ] **OpenAPI**
  - [ ] Replace Swashbuckle with native OpenAPI
  - [ ] Configure transformers
  - [ ] Update UI configuration

## See Also

- [.NET 9 Features Overview](dotnet9-features.md)
- [HTTP Resilience](http-resilience.md)
- [Generic Resilience](generic-resilience.md)
- [HybridCache](hybrid-cache.md)
- [TimeProvider](time-provider.md)
- [Native OpenAPI](native-openapi.md)

