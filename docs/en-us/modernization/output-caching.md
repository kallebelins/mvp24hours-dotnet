# Output Caching - ASP.NET Core Native Server-Side Caching

## Overview

**Output Caching** is a server-side caching mechanism introduced in .NET 7 that stores complete HTTP responses on the server and serves them directly without re-executing endpoint logic. Unlike Response Caching (which relies on HTTP cache headers), Output Caching is entirely server-controlled.

## Key Features

| Feature | Response Caching | Output Caching |
|---------|------------------|----------------|
| Cache Location | Client/Proxy (HTTP headers) | Server-side |
| Control | HTTP Cache-Control headers | Server policies |
| Invalidation | Limited (time-based) | ✅ Tag-based programmatic |
| Distributed Support | ❌ | ✅ Redis backend |
| Policy-based | ❌ | ✅ Named policies |
| Vary Support | Limited | ✅ Query, Header, Route |

## Getting Started

### Basic Setup (In-Memory)

```csharp
// Program.cs
builder.Services.AddMvp24HoursOutputCache();

var app = builder.Build();

app.UseRouting();
app.UseMvp24HoursOutputCache();
app.MapControllers();
```

### With Standard Policies

```csharp
builder.Services.AddMvp24HoursOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(5);
    options.AddStandardPolicies(); // Adds: Default, Short, Medium, Long, NoCache
});
```

### With Redis Backend (Distributed)

```csharp
// For multi-instance deployments
builder.Services.AddMvp24HoursOutputCacheWithRedis(
    "localhost:6379",
    options =>
    {
        options.AddStandardPolicies();
        options.RedisInstanceName = "myapp:oc:";
    });
```

## Named Policies

Mvp24Hours provides several preset policies:

| Policy | Duration | Use Case |
|--------|----------|----------|
| `NoCache` | None | Disable caching |
| `Short` | 1 minute | Frequently changing data |
| `Medium` | 10 minutes | Moderately changing data |
| `Long` | 1 hour | Rarely changing data |
| `VeryLong` | 24 hours | Static content |
| `Authenticated` | 5 minutes | User-specific data (varies by Authorization) |
| `Api` | 5 minutes | API responses (varies by Accept header) |

### Using Policies with Minimal APIs

```csharp
// Using named policy
app.MapGet("/products", GetProducts)
   .CacheOutput("Medium");

// Using Mvp24Hours extension
app.MapGet("/products", GetProducts)
   .CacheOutputWithPolicy("Medium");

// Inline configuration
app.MapGet("/products", GetProducts)
   .CacheOutputFor(
       TimeSpan.FromMinutes(5),
       tags: new[] { "products" },
       varyByQuery: new[] { "category", "page" });

// Disable caching
app.MapPost("/orders", CreateOrder)
   .NoCacheOutput();
```

### Using Policies with Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    // Use named policy
    [HttpGet]
    [OutputCache(PolicyName = "Medium")]
    public async Task<IActionResult> GetAll()
    {
        // ...
    }

    // Custom configuration
    [HttpGet("{id}")]
    [OutputCache(Duration = 60, VaryByRouteValueNames = new[] { "id" })]
    public async Task<IActionResult> GetById(int id)
    {
        // ...
    }

    // Disable caching
    [HttpPost]
    [OutputCache(NoStore = true)]
    public async Task<IActionResult> Create([FromBody] ProductDto dto)
    {
        // ...
    }
}
```

## Custom Policies

### Creating Named Policies

```csharp
builder.Services.AddMvp24HoursOutputCache(options =>
{
    // Products policy with tags for selective invalidation
    options.AddPolicy("Products", p => p
        .Expire(TimeSpan.FromMinutes(10))
        .SetTags("products", "catalog")
        .SetVaryByQuery("category", "page", "sort"));

    // User-specific content
    options.AddPolicy("UserProfile", p => p
        .Expire(TimeSpan.FromMinutes(5))
        .SetVaryByHeader("Authorization")
        .SetTags("users")
        .AllowAuthenticatedRequests());

    // Search results with all query parameters
    options.AddPolicy("Search", p => p
    {
        p.ExpirationTimeSpan = TimeSpan.FromMinutes(2);
        p.VaryByAllQueryKeys = true;
        p.Tags.Add("search");
    });

    // Localized content
    options.AddPolicy("Localized", p => p
        .Expire(TimeSpan.FromHours(1))
        .SetVaryByHeader("Accept-Language")
        .SetTags("content"));
});
```

## Cache Invalidation

### Tag-based Invalidation

Output Caching supports tag-based invalidation through `IOutputCacheInvalidator`:

```csharp
public class ProductService
{
    private readonly IProductRepository _repository;
    private readonly IOutputCacheInvalidator _cacheInvalidator;

    public ProductService(
        IProductRepository repository,
        IOutputCacheInvalidator cacheInvalidator)
    {
        _repository = repository;
        _cacheInvalidator = cacheInvalidator;
    }

    public async Task<Product> CreateProductAsync(ProductDto dto)
    {
        var product = await _repository.CreateAsync(dto);
        
        // Invalidate all product-related cache entries
        await _cacheInvalidator.EvictByTagAsync("products");
        
        return product;
    }

    public async Task UpdateProductAsync(int id, ProductDto dto)
    {
        await _repository.UpdateAsync(id, dto);
        
        // Invalidate specific product and general product list
        await _cacheInvalidator.EvictByTagsAsync(new[] 
        { 
            "products",
            $"product:{id}" 
        });
    }
}
```

### Using with CQRS Commands

```csharp
public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Product>
{
    private readonly IProductRepository _repository;
    private readonly IOutputCacheInvalidator _cacheInvalidator;

    public async Task<Product> Handle(
        CreateProductCommand command, 
        CancellationToken cancellationToken)
    {
        var product = await _repository.CreateAsync(command.Data);
        
        // Invalidate cache after successful command
        await _cacheInvalidator.EvictByTagAsync("products", cancellationToken);
        
        return product;
    }
}
```

## Vary-By Strategies

### Vary By Query String

```csharp
// Vary by specific keys
options.AddPolicy("Search", p => p
    .SetVaryByQuery("q", "page", "size")
    .Expire(TimeSpan.FromMinutes(2)));

// Vary by all query keys
options.AddPolicy("DynamicSearch", p => p
{
    p.VaryByAllQueryKeys = true;
    p.ExpirationTimeSpan = TimeSpan.FromMinutes(2);
});
```

### Vary By Header

```csharp
// Vary by Accept-Language for localized content
options.AddPolicy("Localized", p => p
    .SetVaryByHeader("Accept-Language")
    .Expire(TimeSpan.FromHours(1)));

// Vary by multiple headers
options.AddPolicy("MultiHeader", p => p
    .SetVaryByHeader("Accept", "Accept-Language", "Accept-Encoding")
    .Expire(TimeSpan.FromMinutes(30)));
```

### Vary By Route Values

```csharp
// Vary by route parameters
options.AddPolicy("EntityDetails", p => p
    .SetVaryByRouteValue("id")
    .Expire(TimeSpan.FromMinutes(10)));

// Minimal API usage
app.MapGet("/products/{id}", GetProductById)
   .CacheOutput(policy => policy
       .Expire(TimeSpan.FromMinutes(10))
       .SetVaryByRouteValue("id")
       .Tag($"products"));
```

## Redis Integration

### Why Use Redis for Output Caching?

- **Multi-instance deployments:** Cache is shared across all instances
- **Persistence:** Cache survives application restarts
- **Scalability:** Offload memory usage to Redis
- **Centralized invalidation:** Invalidate across all instances

### Configuration

```csharp
builder.Services.AddMvp24HoursOutputCacheWithRedis(
    "localhost:6379,abortConnect=false",
    options =>
    {
        options.RedisInstanceName = "myapp:oc:";
        options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(10);
        options.AddStandardPolicies();
        
        // Custom policies
        options.AddPolicy("Products", p => p
            .Expire(TimeSpan.FromMinutes(5))
            .SetTags("products"));
    });
```

### Redis Connection Options

```csharp
builder.Services.AddMvp24HoursOutputCache(options =>
{
    options.UseDistributedCache = true;
    options.RedisConnectionString = 
        "redis-server:6379,password=secret,ssl=True,abortConnect=false";
    options.RedisInstanceName = "prod:oc:";
});
```

## Excluded Paths

```csharp
builder.Services.AddMvp24HoursOutputCache(options =>
{
    // Exclude specific paths from caching
    options.ExcludedPaths.Add("/api/health");
    options.ExcludedPaths.Add("/api/admin/*");
    options.ExcludedPaths.Add("/api/auth/*");
});
```

## Configuration Options

### OutputCachingOptions Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Enabled` | bool | `true` | Enable/disable output caching |
| `DefaultExpirationTimeSpan` | TimeSpan | 5 min | Default cache duration |
| `MaximumBodySize` | long | 100 MB | Maximum response size to cache |
| `SizeLimit` | long | 100 MB | Total cache size limit |
| `UseDistributedCache` | bool | `false` | Enable Redis backend |
| `RedisConnectionString` | string? | null | Redis connection string |
| `RedisInstanceName` | string | `"mvp24h-oc:"` | Redis key prefix |
| `UseCaseSensitivePaths` | bool | `false` | Case-sensitive cache keys |
| `VaryByQueryStringByDefault` | bool | `true` | Default vary by query |
| `CacheableMethods` | HashSet | GET, HEAD | HTTP methods to cache |
| `CacheableStatusCodes` | HashSet | 200 | Status codes to cache |

### OutputCachePolicyOptions Properties

| Property | Type | Description |
|----------|------|-------------|
| `ExpirationTimeSpan` | TimeSpan? | Cache duration |
| `NoCache` | bool | Disable caching |
| `Tags` | HashSet | Invalidation tags |
| `VaryByHeader` | HashSet | Headers to vary by |
| `VaryByQueryKeys` | HashSet | Query keys to vary by |
| `VaryByAllQueryKeys` | bool | Vary by all query keys |
| `VaryByRouteValue` | HashSet | Route values to vary by |
| `LockDuringPopulation` | bool | Prevent stampede |
| `CacheAuthenticatedRequests` | bool | Cache authenticated requests |

## Best Practices

### 1. Use Tags for Invalidation Groups

```csharp
options.AddPolicy("Products", p => p
    .Expire(TimeSpan.FromMinutes(10))
    .SetTags("products", "catalog"));

// Invalidate by tag when data changes
await _cacheInvalidator.EvictByTagAsync("products");
```

### 2. Appropriate Cache Durations

```csharp
// Frequently changing data - short cache
options.AddPolicy("RealTimeData", p => p.Expire(TimeSpan.FromSeconds(30)));

// Reference data - long cache
options.AddPolicy("ReferenceData", p => p.Expire(TimeSpan.FromHours(24)));

// User-specific data - medium cache with Authorization vary
options.AddPolicy("UserData", p => p
    .Expire(TimeSpan.FromMinutes(5))
    .SetVaryByHeader("Authorization"));
```

### 3. Exclude Sensitive Endpoints

```csharp
options.ExcludedPaths.Add("/api/auth/*");
options.ExcludedPaths.Add("/api/payments/*");
options.ExcludedPaths.Add("/api/admin/*");
```

### 4. Use Redis for Production

```csharp
if (builder.Environment.IsProduction())
{
    builder.Services.AddMvp24HoursOutputCacheWithRedis(
        configuration["Redis:ConnectionString"]!);
}
else
{
    builder.Services.AddMvp24HoursOutputCache();
}
```

### 5. Combine with HybridCache

Output Caching and HybridCache serve different purposes:

- **Output Caching:** Cache complete HTTP responses
- **HybridCache:** Cache application-level data

```csharp
// Output caching for HTTP responses
builder.Services.AddMvp24HoursOutputCache(options =>
{
    options.AddStandardPolicies();
});

// HybridCache for application data
builder.Services.AddMvpHybridCache();
```

## Pipeline Position

```csharp
var app = builder.Build();

// Exception handling first
app.UseMvp24HoursProblemDetails();

// Then CORS
app.UseCors();

// Then authentication/authorization
app.UseAuthentication();
app.UseAuthorization();

// Output caching after auth (to respect Authorization vary)
app.UseMvp24HoursOutputCache();

// Then routing
app.MapControllers();
```

## See Also

- [HybridCache](hybrid-cache.md) - Application-level caching
- [Rate Limiting](rate-limiting.md) - Request throttling
- [HTTP Resilience](http-resilience.md) - HTTP client resilience

