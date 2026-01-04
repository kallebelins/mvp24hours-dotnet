# Modernization Patterns for AI Agents (.NET 9)

> **AI Agent Instruction**: Use these patterns to implement modern .NET 9 features. Apply based on project requirements and the decision matrix recommendations.

---

## HTTP Resilience (Microsoft.Extensions.Http.Resilience)

### Package Installation

```xml
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
```

### Configuration

```csharp
// appsettings.json
{
  "HttpClients": {
    "ExternalAPI": {
      "BaseUrl": "https://api.external.com",
      "Timeout": "00:00:30"
    }
  },
  "Resilience": {
    "MaxRetryAttempts": 3,
    "CircuitBreakerFailureRatio": 0.5,
    "CircuitBreakerSamplingDuration": "00:00:30",
    "CircuitBreakerMinimumThroughput": 10,
    "CircuitBreakerBreakDuration": "00:00:30"
  }
}

// ServiceBuilderExtensions.cs
public static IServiceCollection AddHttpClients(this IServiceCollection services, IConfiguration configuration)
{
    services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
    {
        client.BaseAddress = new Uri(configuration["HttpClients:ExternalAPI:BaseUrl"]);
        client.Timeout = TimeSpan.Parse(configuration["HttpClients:ExternalAPI:Timeout"]);
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = int.Parse(configuration["Resilience:MaxRetryAttempts"]);
        options.Retry.Delay = TimeSpan.FromSeconds(2);
        options.Retry.UseJitter = true;
        options.Retry.BackoffType = DelayBackoffType.Exponential;

        options.CircuitBreaker.FailureRatio = double.Parse(configuration["Resilience:CircuitBreakerFailureRatio"]);
        options.CircuitBreaker.SamplingDuration = TimeSpan.Parse(configuration["Resilience:CircuitBreakerSamplingDuration"]);
        options.CircuitBreaker.MinimumThroughput = int.Parse(configuration["Resilience:CircuitBreakerMinimumThroughput"]);
        options.CircuitBreaker.BreakDuration = TimeSpan.Parse(configuration["Resilience:CircuitBreakerBreakDuration"]);

        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    });

    return services;
}
```

### Client Implementation

```csharp
public interface IExternalApiClient
{
    Task<ExternalDataDto> GetDataAsync(string id, CancellationToken cancellationToken = default);
}

public class ExternalApiClient : IExternalApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalApiClient> _logger;

    public ExternalApiClient(HttpClient httpClient, ILogger<ExternalApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ExternalDataDto> GetDataAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching data for {Id}", id);
        
        var response = await _httpClient.GetAsync($"/api/data/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<ExternalDataDto>(cancellationToken);
    }
}
```

---

## Generic Resilience (Polly v8)

### Package Installation

```xml
<PackageReference Include="Microsoft.Extensions.Resilience" Version="9.*" />
<PackageReference Include="Polly.Extensions" Version="8.*" />
```

### Configuration

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddGenericResilience(this IServiceCollection services)
{
    services.AddResiliencePipeline("database", builder =>
    {
        builder
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            })
            .AddTimeout(TimeSpan.FromSeconds(10));
    });

    services.AddResiliencePipeline<string, CustomerDto>("customer-cache", builder =>
    {
        builder
            .AddRetry(new RetryStrategyOptions<CustomerDto>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromMilliseconds(100)
            })
            .AddTimeout(TimeSpan.FromSeconds(5));
    });

    return services;
}
```

### Usage

```csharp
public class CustomerService : ICustomerService
{
    private readonly ResiliencePipeline _pipeline;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public CustomerService(
        [FromKeyedServices("database")] ResiliencePipeline pipeline,
        IUnitOfWorkAsync unitOfWork)
    {
        _pipeline = pipeline;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id)
    {
        return await _pipeline.ExecuteAsync(async ct =>
        {
            var repository = _unitOfWork.GetRepository<Customer>();
            var customer = await repository.GetByIdAsync(id);
            return _mapper.Map<CustomerDto>(customer);
        });
    }
}
```

---

## HybridCache (.NET 9)

### Package Installation

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" Version="9.*" />
```

### Configuration

```csharp
// appsettings.json
{
  "HybridCache": {
    "DefaultExpiration": "00:05:00",
    "MaximumPayloadBytes": 1048576
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  }
}

// ServiceBuilderExtensions.cs
public static IServiceCollection AddHybridCaching(this IServiceCollection services, IConfiguration configuration)
{
    services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = configuration["Redis:ConnectionString"];
    });

    services.AddHybridCache(options =>
    {
        options.DefaultEntryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.Parse(configuration["HybridCache:DefaultExpiration"]),
            LocalCacheExpiration = TimeSpan.FromMinutes(1)
        };
        options.MaximumPayloadBytes = int.Parse(configuration["HybridCache:MaximumPayloadBytes"]);
    });

    return services;
}
```

### Usage

```csharp
public class CustomerService : ICustomerService
{
    private readonly HybridCache _cache;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public CustomerService(HybridCache cache, IUnitOfWorkAsync unitOfWork)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            $"customer:{id}",
            async ct =>
            {
                var repository = _unitOfWork.GetRepository<Customer>();
                var customer = await repository.GetByIdAsync(id);
                return _mapper.Map<CustomerDto>(customer);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            },
            cancellationToken: cancellationToken);
    }

    public async Task InvalidateCacheAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync($"customer:{id}", cancellationToken);
    }
}
```

---

## Rate Limiting

### Package Installation

```xml
<PackageReference Include="Microsoft.AspNetCore.RateLimiting" Version="9.*" />
```

### Configuration

```csharp
// appsettings.json
{
  "RateLimiting": {
    "PermitLimit": 100,
    "WindowInSeconds": 60,
    "QueueLimit": 10
  }
}

// ServiceBuilderExtensions.cs
public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
{
    services.AddRateLimiter(options =>
    {
        // Global rate limiter
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.User.Identity?.Name ?? httpContext.Request.Headers.Host.ToString(),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = int.Parse(configuration["RateLimiting:PermitLimit"]),
                    Window = TimeSpan.FromSeconds(int.Parse(configuration["RateLimiting:WindowInSeconds"])),
                    QueueLimit = int.Parse(configuration["RateLimiting:QueueLimit"]),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));

        // Named policy for specific endpoints
        options.AddFixedWindowLimiter("api", opt =>
        {
            opt.PermitLimit = 50;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 5;
        });

        options.AddSlidingWindowLimiter("sliding", opt =>
        {
            opt.PermitLimit = 100;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.SegmentsPerWindow = 6;
            opt.QueueLimit = 10;
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "Too many requests. Please try again later.",
                RetryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)
                    ? retryAfter.TotalSeconds
                    : 60
            }, cancellationToken);
        };
    });

    return services;
}

// Program.cs
app.UseRateLimiter();

// Controller usage
[EnableRateLimiting("api")]
[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("sliding")]
    public async Task<IActionResult> GetAll() { }

    [HttpPost]
    [DisableRateLimiting]
    public async Task<IActionResult> Create([FromBody] CustomerCreateDto dto) { }
}
```

---

## Keyed Services

### Configuration

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddKeyedServices(this IServiceCollection services)
{
    // Database strategies
    services.AddKeyedScoped<IDatabaseStrategy, SqlServerStrategy>("sqlserver");
    services.AddKeyedScoped<IDatabaseStrategy, PostgresStrategy>("postgres");
    services.AddKeyedScoped<IDatabaseStrategy, MongoDbStrategy>("mongodb");

    // Cache strategies
    services.AddKeyedSingleton<ICacheStrategy, RedisCacheStrategy>("redis");
    services.AddKeyedSingleton<ICacheStrategy, MemoryCacheStrategy>("memory");

    // Notification strategies
    services.AddKeyedTransient<INotificationService, EmailNotificationService>("email");
    services.AddKeyedTransient<INotificationService, SmsNotificationService>("sms");
    services.AddKeyedTransient<INotificationService, PushNotificationService>("push");

    return services;
}
```

### Usage

```csharp
public class CustomerService : ICustomerService
{
    private readonly IDatabaseStrategy _database;
    private readonly ICacheStrategy _cache;
    private readonly INotificationService _emailNotification;

    public CustomerService(
        [FromKeyedServices("sqlserver")] IDatabaseStrategy database,
        [FromKeyedServices("redis")] ICacheStrategy cache,
        [FromKeyedServices("email")] INotificationService emailNotification)
    {
        _database = database;
        _cache = cache;
        _emailNotification = emailNotification;
    }
}

// Controller with keyed services
[ApiController]
[Route("api/[controller]")]
public class NotificationController : ControllerBase
{
    [HttpPost("email")]
    public async Task<IActionResult> SendEmail(
        [FromKeyedServices("email")] INotificationService service,
        [FromBody] NotificationDto dto)
    {
        await service.SendAsync(dto);
        return Ok();
    }

    [HttpPost("sms")]
    public async Task<IActionResult> SendSms(
        [FromKeyedServices("sms")] INotificationService service,
        [FromBody] NotificationDto dto)
    {
        await service.SendAsync(dto);
        return Ok();
    }
}
```

---

## ProblemDetails (RFC 7807)

### Configuration

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddProblemDetails(this IServiceCollection services)
{
    services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;
        };
    });

    return services;
}

// Program.cs
app.UseExceptionHandler();
app.UseStatusCodePages();
```

### Custom Problem Details

```csharp
public static class ProblemDetailsExtensions
{
    public static IResult ToProblemDetails(this ValidationResult validationResult, HttpContext context)
    {
        return Results.Problem(
            title: "Validation Error",
            detail: "One or more validation errors occurred.",
            statusCode: StatusCodes.Status400BadRequest,
            extensions: new Dictionary<string, object?>
            {
                ["errors"] = validationResult.Errors.Select(e => new
                {
                    Field = e.PropertyName,
                    Error = e.ErrorMessage
                }),
                ["traceId"] = context.TraceIdentifier
            });
    }

    public static IResult ToNotFoundProblem(string message, HttpContext context)
    {
        return Results.Problem(
            title: "Resource Not Found",
            detail: message,
            statusCode: StatusCodes.Status404NotFound,
            extensions: new Dictionary<string, object?>
            {
                ["traceId"] = context.TraceIdentifier
            });
    }
}
```

---

## Minimal APIs with TypedResults

### Endpoint Definition

```csharp
public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers")
            .WithOpenApi();

        group.MapGet("/", GetAll)
            .WithName("GetAllCustomers")
            .WithSummary("Get all customers with pagination")
            .Produces<IPagingResult<CustomerDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetCustomerById")
            .WithSummary("Get customer by ID")
            .Produces<CustomerDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", Create)
            .WithName("CreateCustomer")
            .WithSummary("Create a new customer")
            .Produces<CustomerDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateCustomer")
            .WithSummary("Update an existing customer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteCustomer")
            .WithSummary("Delete a customer")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<IPagingResult<CustomerDto>>, ProblemHttpResult>> GetAll(
        [AsParameters] PaginationParams pagination,
        ICustomerService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetAllAsync(pagination.Page, pagination.Limit, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<CustomerDto>, NotFound<ProblemDetails>>> GetById(
        Guid id,
        ICustomerService service,
        CancellationToken cancellationToken)
    {
        var result = await service.GetByIdAsync(id, cancellationToken);
        if (!result.HasData)
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Customer Not Found",
                Detail = $"Customer with ID {id} was not found."
            });

        return TypedResults.Ok(result.Data);
    }

    private static async Task<Results<Created<CustomerDto>, ValidationProblem>> Create(
        CustomerCreateDto dto,
        IValidator<CustomerCreateDto> validator,
        ICustomerService service,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var result = await service.CreateAsync(dto, cancellationToken);
        return TypedResults.Created($"/api/customers/{result.Data.Id}", result.Data);
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, ValidationProblem>> Update(
        Guid id,
        CustomerUpdateDto dto,
        IValidator<CustomerUpdateDto> validator,
        ICustomerService service,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var result = await service.UpdateAsync(id, dto, cancellationToken);
        if (!result.HasData)
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Customer Not Found",
                Detail = $"Customer with ID {id} was not found."
            });

        return TypedResults.NoContent();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>>> Delete(
        Guid id,
        ICustomerService service,
        CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        if (!result.Data)
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Customer Not Found",
                Detail = $"Customer with ID {id} was not found."
            });

        return TypedResults.NoContent();
    }
}

public record PaginationParams(int Page = 1, int Limit = 10);
```

---

## TimeProvider (Testability)

### Configuration

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddTimeProvider(this IServiceCollection services)
{
    services.AddSingleton(TimeProvider.System);
    return services;
}
```

### Usage

```csharp
public class CustomerService : ICustomerService
{
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public CustomerService(TimeProvider timeProvider, IUnitOfWorkAsync unitOfWork)
    {
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<IBusinessResult<CustomerDto>> CreateAsync(CustomerCreateDto dto)
    {
        var customer = new Customer
        {
            Name = dto.Name,
            Email = dto.Email,
            Created = _timeProvider.GetUtcNow().DateTime
        };

        await _unitOfWork.GetRepository<Customer>().AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        return new BusinessResult<CustomerDto>(_mapper.Map<CustomerDto>(customer));
    }
}

// Unit Test with FakeTimeProvider
public class CustomerServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldUseCurrentTime()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new CustomerService(fakeTime, _mockUnitOfWork.Object);

        var result = await service.CreateAsync(new CustomerCreateDto { Name = "Test" });

        Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), result.Data.Created);
    }
}
```

---

## Output Caching

### Configuration

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddOutputCaching(this IServiceCollection services)
{
    services.AddOutputCache(options =>
    {
        options.AddBasePolicy(builder => builder.Expire(TimeSpan.FromMinutes(5)));

        options.AddPolicy("ByIdCache", builder => builder
            .SetVaryByRouteValue("id")
            .Expire(TimeSpan.FromMinutes(10)));

        options.AddPolicy("ByQueryCache", builder => builder
            .SetVaryByQuery("page", "limit", "filter")
            .Expire(TimeSpan.FromMinutes(2)));

        options.AddPolicy("AuthenticatedCache", builder => builder
            .SetVaryByHeader("Authorization")
            .Expire(TimeSpan.FromMinutes(5)));
    });

    return services;
}

// Program.cs
app.UseOutputCache();

// Controller usage
[OutputCache(PolicyName = "ByQueryCache")]
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination) { }

[OutputCache(PolicyName = "ByIdCache")]
[HttpGet("{id:guid}")]
public async Task<IActionResult> GetById(Guid id) { }
```

---

## Native OpenAPI (.NET 9)

### Configuration

```csharp
// Program.cs
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "ProjectName API";
        document.Info.Version = "v1";
        document.Info.Description = "API for managing customers";
        document.Info.Contact = new OpenApiContact
        {
            Name = "Support",
            Email = "support@example.com"
        };
        return Task.CompletedTask;
    });
});

// app configuration
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ProjectName API v1");
    });
}
```

---

## Related Documentation

- [Architecture Templates](architecture-templates.md)
- [Decision Matrix](decision-matrix.md)
- [Observability Patterns](observability-patterns.md)

