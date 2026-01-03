# Migration

## Version v4.2.101
### IBsonClassMap
Remove generic typing from IBsonClassMap<T>:
```csharp
// before
public class MyEntityConfiguration : IBsonClassMap<MyEntity>

// after
public class MyEntityConfiguration : IBsonClassMap
```

## Version v8.2.102
### EntityBase
```csharp
// before
public class MyEntity : EntityBase<MyEntity, int>

// after
public class MyEntity : EntityBase<int>
```

### IMapFrom
Remove generic typing from IMapFrom<T>:
```csharp
// before
public class MyDto : IMapFrom<MyEntity>

// after
public class MyDto : IMapFrom
```

### TelemetryLevel
Update TelemetryLevel enumerator name to plural:
```csharp
// before
TelemetryHelper.Execute(TelemetryLevel.Verbose, "jwt-test", $"token:xxx");

// after
TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-test", $"token:xxx");
```

### Mapping
```csharp
// injection into service class construction
private readonly IMapper mapper;
public MyEntityService(IUnitOfWorkAsync unitOfWork, IValidator<MyEntity> validator, IMapper mapper)
	: base(unitOfWork, validator)
{
	this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
}
```

#### AutoMapperHelper
```csharp
// before - anti-pattern singleton
AutoMapperHelper.Map<MyEntity>(entity, dto);

// after
mapper.Map(dto, entity);
```

#### MapTo
```csharp
// before
var entity = dto.MapTo<MyEntity>();

// after
var entity = mapper.Map<MyEntity>(dto);
```

```csharp
// before
return result.MapBusinessTo<IList<MyEntity>, IList<MyEntityIdResult>>();

// after
mapper.MapBusinessTo<IList<MyEntity>, IList<MyEntityIdResult>>(result);
```

### ServiceProviderHelper
```csharp
// before - anti-pattern singleton
public static IMyEntityService MyEntityService
{
	get { return ServiceProviderHelper.GetService<IMyEntityService>(); }
}

// after - injection into class construction
private readonly IServiceProvider provider;
public FacadeService(IServiceProvider provider)
{
	this.provider = provider;
}
public IMyEntityService MyEntityService
{
	get { return provider.GetService<IMyEntityService>(); }
}
```

### FacadeService
```csharp
// injection in controller class construction
private readonly FacadeService facade;
public MyEntityController(FacadeService facade)
{
	this.facade = facade;
}
```

```csharp
// before
var result = await FacadeService.MyEntityService.GetBy(myEntityId, cancellationToken: cancellationToken);

// after
var result = await facade.MyEntityService.GetBy(myEntityId, cancellationToken: cancellationToken);
```

### Startup
Removed UseMvp24Hours() from the Startup class.
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
	// ...
	//app.UseMvp24Hours();
}
```

---

## Version v9.0.x

Version 9.0.x introduces major modernizations aligned with .NET 9 native APIs.

### TelemetryHelper → ILogger

> ⚠️ **Deprecated:** `TelemetryHelper` is deprecated. Use `ILogger<T>` instead.

```csharp
// before
TelemetryHelper.Execute(TelemetryLevels.Information, "order-processing", orderId);

// after
private readonly ILogger<OrderService> _logger;

public OrderService(ILogger<OrderService> logger)
{
    _logger = logger;
}

public void ProcessOrder(int orderId)
{
    _logger.LogInformation("Processing order {OrderId}", orderId);
}
```

#### Configuration

```csharp
// before
services.AddMvp24HoursTelemetry(TelemetryLevels.Information | TelemetryLevels.Verbose,
    (name, state) => Console.WriteLine($"{name}|{string.Join("|", state)}"));

// after
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// or with OpenTelemetry
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyService";
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

> 📚 For complete migration guide, see [Telemetry Migration](observability/migration.md).

### HttpClientExtensions → Microsoft.Extensions.Http.Resilience

> ⚠️ **Deprecated:** Custom `HttpClientExtensions` and `HttpPolicyHelper` are deprecated. Use native resilience.

```csharp
// before
services.AddHttpClient("MyApi")
    .AddPolicyHandler(HttpPolicyHelper.GetRetryPolicy(3))
    .AddPolicyHandler(HttpPolicyHelper.GetCircuitBreakerPolicy(5, TimeSpan.FromSeconds(30)));

// after
services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddMvpStandardResilience();
// or with custom configuration:
.AddMvpResilience(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.CircuitBreaker.FailureRatio = 0.5;
});
```

### MultiLevelCache → HybridCache

> ⚠️ **Deprecated:** `MultiLevelCache` is deprecated. Use .NET 9 `HybridCache` instead.

```csharp
// before
services.AddMultiLevelCache(options =>
{
    options.L1Options.SizeLimit = 1000;
    options.L2ConnectionString = "redis:6379";
});

var item = await multiLevelCache.GetOrSetAsync("key", 
    async () => await LoadDataAsync(), 
    TimeSpan.FromMinutes(5));

// after
services.AddMvpHybridCache(options =>
{
    options.DefaultEntryOptions.Expiration = TimeSpan.FromMinutes(5);
    options.DefaultEntryOptions.LocalCacheExpiration = TimeSpan.FromMinutes(1);
});

var item = await hybridCache.GetOrCreateAsync("key",
    async cancel => await LoadDataAsync(cancel),
    new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    });
```

### Swagger → Native OpenAPI

> ⚠️ **Note:** Swashbuckle is still supported but Native OpenAPI is preferred for .NET 9+.

```csharp
// before (Swashbuckle)
services.AddMvp24HoursSwagger("My API", version: "v1", 
    oAuthScheme: SwaggerAuthorizationScheme.Bearer);

app.UseSwagger();
app.UseSwaggerUI();

// after (Native OpenAPI)
services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "My API";
    options.Version = "1.0.0";
    options.EnableSwaggerUI = true;
    options.AuthenticationScheme = OpenApiAuthenticationScheme.Bearer;
});

app.MapMvp24HoursNativeOpenApi();
```

### Startup.cs → Program.cs (Minimal Hosting)

.NET 6+ uses minimal hosting model with `Program.cs`:

```csharp
// before (Startup.cs)
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddMvp24HoursDbContext<MyDbContext>(...);
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseRouting();
        app.UseEndpoints(endpoints => ...);
    }
}

// after (Program.cs)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvp24HoursDbContext<MyDbContext>(...);

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
```

---

## Version v9.1.x

Version 9.1.x adds CQRS, enhanced observability, and new infrastructure features.

### CQRS Integration

New CQRS implementation with MediatR-compatible API:

```csharp
// Register CQRS services
builder.Services.AddMvp24HoursCqrs(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Define a command
public record CreateOrderCommand(string CustomerId, List<OrderItem> Items) 
    : ICommand<OrderResult>;

// Define a handler
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderResult>
{
    public async Task<OrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // ... implementation
    }
}

// Usage with IMediator
var result = await _mediator.Send(new CreateOrderCommand(customerId, items));
```

> 📚 See [CQRS Documentation](cqrs/getting-started.md) for complete guide.

### ValidationBehavior for CQRS

```csharp
// Register validation behavior
builder.Services.AddMvp24HoursCqrs(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddValidationBehavior(); // Automatic validation
});

// Define validator
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

> 📚 See [Validation Behavior](cqrs/validation-behavior.md) for details.

### Observability (Logs, Traces, Metrics)

```csharp
// All-in-one observability configuration
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyService";
    options.ServiceVersion = "1.0.0";
    
    options.EnableLogging = true;
    options.EnableTracing = true;
    options.EnableMetrics = true;
    
    options.Logging.EnableTraceCorrelation = true;
    options.Tracing.EnableCorrelationIdPropagation = true;
});
```

> 📚 See [Observability Documentation](observability/home.md) for complete guide.

### TimeProvider Integration

```csharp
// before
var now = DateTime.UtcNow;

// after
private readonly TimeProvider _timeProvider;

public MyService(TimeProvider timeProvider)
{
    _timeProvider = timeProvider;
}

public void DoWork()
{
    var now = _timeProvider.GetUtcNow();
}

// Registration
services.AddTimeProvider();

// For testing
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
services.ReplaceTimeProvider(fakeTime);
```

---

## Migration Resources

For complete migration documentation:

- [.NET 9 Native APIs Migration Guide](modernization/migration-guide.md) - Complete guide to all native API migrations
- [TelemetryHelper Migration](observability/migration.md) - Detailed ILogger/OpenTelemetry migration
- [CQRS Getting Started](cqrs/getting-started.md) - CQRS implementation guide
- [Native OpenAPI](modernization/native-openapi.md) - OpenAPI migration guide
- [HybridCache](modernization/hybrid-cache.md) - Cache modernization guide
