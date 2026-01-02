# ASP.NET Web API
In this topic you will find some resources to speed up building web services with ASP.NET Web API.

## Services

```csharp
// Program.cs

var builder = WebApplication.CreateBuilder(args);

/// essential
builder.Services.AddMvp24HoursWebEssential();

/// automapper
builder.Services.AddMvp24HoursMapService(assemblyMap: Assembly.GetExecutingAssembly());

/// json
builder.Services.AddMvp24HoursWebJson();

/// swagger (or use Native OpenAPI - see below)
builder.Services.AddMvp24HoursSwagger("MyAPI");

/// compression
builder.Services.AddMvp24HoursWebGzip();

/// exception middleware
builder.Services.AddMvp24HoursWebExceptions(options => { });

/// cors middleware
builder.Services.AddMvp24HoursWebCors(options => { });
```

### Native OpenAPI (.NET 9+)

For .NET 9+ projects, use Native OpenAPI instead of Swashbuckle:

```csharp
// Program.cs
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
    options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
});

var app = builder.Build();

app.MapMvp24HoursNativeOpenApi();
```

> 📚 See [Native OpenAPI Documentation](modernization/native-openapi.md) for complete guide.

### Health Checks ([AspNetCore.Diagnostics.HealthChecks](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks))

```csharp
// Package Manager Console >
Install-Package AspNetCore.HealthChecks.UI.Client -Version 8.0.1
/// SQLServer
Install-Package AspNetCore.HealthChecks.SqlServer -Version 8.0.2
/// PostgreSQL
Install-Package AspNetCore.HealthChecks.NpgSql -Version 8.0.2
/// MySQL
Install-Package AspNetCore.HealthChecks.MySql -Version 8.0.1
/// MongoDB
Install-Package AspNetCore.HealthChecks.MongoDb -Version 8.0.1
/// Redis
Install-Package AspNetCore.HealthChecks.Redis -Version 8.0.1
/// RabbitMQ
Install-Package AspNetCore.HealthChecks.Rabbitmq -Version 8.0.1

// Program.cs

builder.Services.AddHealthChecks()

    /// SQLServer
    .AddSqlServer(
        builder.Configuration.GetConnectionString("CustomerDbContext"),
        healthQuery: "SELECT 1;",
        name: "SqlServer",
        failureStatus: HealthStatus.Degraded)

    /// MongoDB
    .AddMongoDb(
        builder.Configuration.GetConnectionString("MongoDbContext"),
        name: "MongoDb",
        failureStatus: HealthStatus.Degraded)

    /// Redis
    .AddRedis(
        builder.Configuration.GetConnectionString("RedisDbContext"),
        name: "Redis",
        failureStatus: HealthStatus.Degraded)

    /// RabbitMQ
    .AddRabbitMQ(
        builder.Configuration.GetConnectionString("RabbitMQContext"),
        name: "RabbitMQ",
        failureStatus: HealthStatus.Degraded);

var app = builder.Build();

app.MapHealthChecks("/hc", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### Rate Limiting

```csharp
// Program.cs
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueLimit = 10;
    });
    
    options.AddSlidingWindowLimiter("sliding", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 4;
    });
    
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

var app = builder.Build();

app.UseRateLimiter();

// Apply to endpoints
app.MapGet("/api/limited", () => "Hello")
    .RequireRateLimiting("fixed");

// Or apply to controllers
[EnableRateLimiting("fixed")]
public class MyController : ControllerBase { }
```

> 📚 See [Rate Limiting Documentation](modernization/rate-limiting.md) for complete guide.

### ProblemDetails (RFC 7807)

```csharp
// Program.cs
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
        context.ProblemDetails.Extensions["traceId"] = 
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
```

### Output Caching

```csharp
// Program.cs
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));
    
    options.AddPolicy("CacheForHour", builder => 
        builder.Expire(TimeSpan.FromHours(1)));
});

var app = builder.Build();

app.UseOutputCache();

// Apply to endpoints
app.MapGet("/api/cached", () => "Hello")
    .CacheOutput("CacheForHour");
```

### Security Headers

```csharp
// Program.cs
builder.Services.AddMvp24HoursSecurityHeaders(options =>
{
    options.AddContentSecurityPolicy = true;
    options.AddXContentTypeOptions = true;
    options.AddXFrameOptions = true;
    options.AddReferrerPolicy = true;
    options.AddPermissionsPolicy = true;
    options.RemoveServerHeader = true;
});

var app = builder.Build();

app.UseMvp24HoursSecurityHeaders();
```

> 📚 See [Security Headers Documentation](webapi-advanced.md#security-headers) for complete guide.

### Idempotency Middleware

For operations that should be idempotent (e.g., payment processing):

```csharp
// Program.cs
builder.Services.AddMvp24HoursIdempotency(options =>
{
    options.IdempotencyKeyHeader = "X-Idempotency-Key";
    options.CacheExpiration = TimeSpan.FromHours(24);
    options.EnableForAllMethods = false; // Only POST, PUT, PATCH by default
});

var app = builder.Build();

app.UseMvp24HoursIdempotency();

// Or apply to specific endpoints
app.MapPost("/api/payments", handler)
    .WithIdempotency();
```

> 📚 See [Idempotency Documentation](webapi-advanced.md#idempotency) for complete guide.

## Application / Middlewares

```csharp
// Program.cs

var app = builder.Build();

/// exception handlers
app.UseMvp24HoursExceptionHandling();

/// cors
app.UseMvp24HoursCors();

/// swagger (if using Swashbuckle)
if (!app.Environment.IsProduction())
{
    app.UseMvp24HoursSwagger();
}

/// correlation-id
app.UseMvp24HoursCorrelationId();
```

## Observability

```csharp
// Program.cs
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyAPI";
    options.ServiceVersion = "1.0.0";
    options.EnableLogging = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

> 📚 See [Observability Documentation](observability/home.md) for complete guide.

## Resilient HTTP Requests
Use IHttpClientFactory to implement resilient HTTP requests.

```csharp
// Program.cs

/// inject httpclient using custom name with native resilience
builder.Services.AddHttpClient("my-api-url", client =>
{
    client.BaseAddress = new Uri("https://myexampleapi.com");
})
.AddMvpStandardResilience();

/// inject HttpClient using Class name
builder.Services.AddHttpClient<MyClassClient>(client =>
{
    client.BaseAddress = new Uri("https://myexampleapi.com");
})
.AddMvpResilience(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.CircuitBreaker.FailureRatio = 0.5;
});

// MyClassClient.cs

/// through the constructor, if you have registered the client class "AddHttpClient<MyClassClient>"
private readonly HttpClient _httpClient;
public MyClassClient(HttpClient httpClient)
{
    _httpClient = httpClient;
}
/// return string => "_httpClient.HttpGetAsync()"
public async Task<string> GetResults() {
    return await _httpClient.HttpGetAsync("api/myService");
}
/// return object => "_httpClient.HttpGetAsync<MyResult>()"
public async Task<MyResult> GetResults() {
    return await _httpClient.HttpGetAsync<MyResult>("api/myService");
}

/// service provider using custom name
var factory = serviceProvider.GetService<IHttpClientFactory>();
var client = factory.CreateClient("my-api-url");
var result = await client.HttpGetAsync("api/myService");

/// service provider using reference class
var factory = serviceProvider.GetService<IHttpClientFactory>();
var client = factory.CreateClient(typeof(MyClassClient).Name);
var result = await client.HttpGetAsync("api/myService");
```

## Minimal API

### Using TypedResults

Use `TypedResults` for strongly-typed responses:

```csharp
app.MapGet("/api/customers/{id}", async (int id, ICustomerService service) =>
{
    var customer = await service.GetByIdAsync(id);
    return customer is not null 
        ? TypedResults.Ok(customer)
        : TypedResults.NotFound();
})
.WithName("GetCustomerById")
.WithOpenApi()
.Produces<Customer>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);
```

### Using ExtensionBinder

Use the ExtensionBinder class to handle parameter-to-object conversion:

```csharp
public class CustomerFilter : ExtensionBinder<CustomerFilter>
{
    public string Name { get; set; }
    public bool? Active { get; set; }
}
```

You can also use the ModelBinder class as an Action input parameter if you don't want to update your existing classes:

```csharp
app.MapGet("/customer", async (
    CustomerFilter model, 
    ModelBinder<PagingCriteriaRequest> pagingCriteriaBinder, 
    [FromServices] IUnitOfWorkAsync uoW) =>
{
    if (pagingCriteriaBinder.Error != null)
        return Results.BadRequest(pagingCriteriaBinder.Error);

    var pagingCriteria = pagingCriteriaBinder.Data;

    //...

    return Results.Ok(result);
})
.WithName("CustomerGetBy")
.WithOpenApi();
```

If you want to implement your own conversion, use the IExtensionBinder interface:

```csharp
public class Customer : IExtensionBinder<Customer>
{
    public string Nome { get; private set; }

    public static ValueTask<Customer> BindAsync(HttpContext context)
    {
        return ValueTask.FromResult(context.Request.GetFromQueryString<Customer>() ?? new());
    }
}
```

---

## Related Documentation

- [WebAPI Advanced](webapi-advanced.md) - Security headers, idempotency, API versioning
- [Native OpenAPI](modernization/native-openapi.md) - .NET 9 native OpenAPI documentation
- [Rate Limiting](modernization/rate-limiting.md) - Rate limiting patterns
- [ProblemDetails](modernization/problem-details.md) - RFC 7807 error responses
- [Output Caching](modernization/output-caching.md) - Response caching
- [Observability](observability/home.md) - Logging, tracing, metrics
