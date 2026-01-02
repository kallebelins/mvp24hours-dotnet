# Migração

## Versão v4.2.101
### IBsonClassMap
Remover tipagem genérica de IBsonClassMap<T>:
```csharp
// antes
public class MyEntityConfiguration : IBsonClassMap<MyEntity>

// depois
public class MyEntityConfiguration : IBsonClassMap
```

## Versão v9.1.200
### EntityBase
```csharp
// antes
public class MyEntity : EntityBase<MyEntity, int>

// depois
public class MyEntity : EntityBase<int>
```

### IMapFrom
Remover tipagem genérica de IMapFrom<T>:
```csharp
// antes
public class MyDto : IMapFrom<MyEntity>

// depois
public class MyDto : IMapFrom
```

### TelemetryLevel
Atualizar nome do enumerador TelemetryLevel para plural:
```csharp
// antes
TelemetryHelper.Execute(TelemetryLevel.Verbose, "jwt-test", $"token:xxx");

// depois
TelemetryHelper.Execute(TelemetryLevels.Verbose, "jwt-test", $"token:xxx");
```

### Mapping
```csharp
// injeção na construção da classe de serviço
private readonly IMapper mapper;
public MyEntityService(IUnitOfWorkAsync unitOfWork, IValidator<MyEntity> validator, IMapper mapper)
	: base(unitOfWork, validator)
{
	this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
}
```

#### AutoMapperHelper
```csharp
// antes - anti-pattern singleton
AutoMapperHelper.Map<MyEntity>(entity, dto);

// depois
mapper.Map(dto, entity);
```

#### MapTo
```csharp
// antes
var entity = dto.MapTo<MyEntity>();

// depois
var entity = mapper.Map<MyEntity>(dto);
```

```csharp
// antes
return result.MapBusinessTo<IList<MyEntity>, IList<MyEntityIdResult>>();

// depois
mapper.MapBusinessTo<IList<MyEntity>, IList<MyEntityIdResult>>(result);
```

### ServiceProviderHelper
```csharp
// antes - anti-pattern singleton
public static IMyEntityService MyEntityService
{
	get { return ServiceProviderHelper.GetService<IMyEntityService>(); }
}

// depois - injeção na construção da classe
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
// injeção na construção da classe de controlador
private readonly FacadeService facade;
public MyEntityController(FacadeService facade)
{
	this.facade = facade;
}
```

```csharp
// antes
var result = await FacadeService.MyEntityService.GetBy(myEntityId, cancellationToken: cancellationToken);

// depois
var result = await facade.MyEntityService.GetBy(myEntityId, cancellationToken: cancellationToken);
```

### Startup
Remoção de UseMvp24Hours() da classe de Startup.
```csharp
public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
	// ...
	//app.UseMvp24Hours();
}
```

---

## Versão v9.0.x

A versão 9.0.x introduz modernizações importantes alinhadas com as APIs nativas do .NET 9.

### TelemetryHelper → ILogger

> ⚠️ **Deprecado:** `TelemetryHelper` está deprecado. Use `ILogger<T>` em seu lugar.

```csharp
// antes
TelemetryHelper.Execute(TelemetryLevels.Information, "order-processing", orderId);

// depois
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

#### Configuração

```csharp
// antes
services.AddMvp24HoursTelemetry(TelemetryLevels.Information | TelemetryLevels.Verbose,
    (name, state) => Console.WriteLine($"{name}|{string.Join("|", state)}"));

// depois
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Debug);
});

// ou com OpenTelemetry
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MyService";
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

> 📚 Para guia completo de migração, consulte [Migração de Telemetria](observability/migration.md).

### HttpClientExtensions → Microsoft.Extensions.Http.Resilience

> ⚠️ **Deprecado:** `HttpClientExtensions` e `HttpPolicyHelper` customizados estão deprecados. Use resiliência nativa.

```csharp
// antes
services.AddHttpClient("MyApi")
    .AddPolicyHandler(HttpPolicyHelper.GetRetryPolicy(3))
    .AddPolicyHandler(HttpPolicyHelper.GetCircuitBreakerPolicy(5, TimeSpan.FromSeconds(30)));

// depois
services.AddHttpClient("MyApi", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
})
.AddMvpStandardResilience();
// ou com configuração personalizada:
.AddMvpResilience(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.CircuitBreaker.FailureRatio = 0.5;
});
```

### MultiLevelCache → HybridCache

> ⚠️ **Deprecado:** `MultiLevelCache` está deprecado. Use `HybridCache` do .NET 9.

```csharp
// antes
services.AddMultiLevelCache(options =>
{
    options.L1Options.SizeLimit = 1000;
    options.L2ConnectionString = "redis:6379";
});

var item = await multiLevelCache.GetOrSetAsync("key", 
    async () => await LoadDataAsync(), 
    TimeSpan.FromMinutes(5));

// depois
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

### Swagger → OpenAPI Nativo

> ⚠️ **Nota:** Swashbuckle ainda é suportado, mas OpenAPI Nativo é preferido para .NET 9+.

```csharp
// antes (Swashbuckle)
services.AddMvp24HoursSwagger("My API", version: "v1", 
    oAuthScheme: SwaggerAuthorizationScheme.Bearer);

app.UseSwagger();
app.UseSwaggerUI();

// depois (OpenAPI Nativo)
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

.NET 6+ usa modelo de hospedagem mínima com `Program.cs`:

```csharp
// antes (Startup.cs)
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

// depois (Program.cs)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMvp24HoursDbContext<MyDbContext>(...);

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
```

---

## Versão v9.1.x

A versão 9.1.x adiciona CQRS, observabilidade aprimorada e novos recursos de infraestrutura.

### Integração CQRS

Nova implementação CQRS com API compatível com MediatR:

```csharp
// Registrar serviços CQRS
builder.Services.AddMvp24HoursCqrs(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

// Definir um command
public record CreateOrderCommand(string CustomerId, List<OrderItem> Items) 
    : ICommand<OrderResult>;

// Definir um handler
public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderResult>
{
    public async Task<OrderResult> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // ... implementação
    }
}

// Uso com IMediator
var result = await _mediator.Send(new CreateOrderCommand(customerId, items));
```

> 📚 Consulte [Documentação CQRS](cqrs/getting-started.md) para guia completo.

### ValidationBehavior para CQRS

```csharp
// Registrar behavior de validação
builder.Services.AddMvp24HoursCqrs(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddValidationBehavior(); // Validação automática
});

// Definir validador
public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
    }
}
```

> 📚 Consulte [Validation Behavior](cqrs/validation-behavior.md) para detalhes.

### Observabilidade (Logs, Traces, Métricas)

```csharp
// Configuração completa de observabilidade
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

> 📚 Consulte [Documentação de Observabilidade](observability/home.md) para guia completo.

### Integração TimeProvider

```csharp
// antes
var now = DateTime.UtcNow;

// depois
private readonly TimeProvider _timeProvider;

public MyService(TimeProvider timeProvider)
{
    _timeProvider = timeProvider;
}

public void DoWork()
{
    var now = _timeProvider.GetUtcNow();
}

// Registro
services.AddTimeProvider();

// Para testes
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
services.ReplaceTimeProvider(fakeTime);
```

---

## Recursos de Migração

Para documentação completa de migração:

- [Guia de Migração para APIs Nativas .NET 9](modernization/migration-guide.md) - Guia completo de todas as migrações de API nativa
- [Migração do TelemetryHelper](observability/migration.md) - Migração detalhada para ILogger/OpenTelemetry
- [CQRS Getting Started](cqrs/getting-started.md) - Guia de implementação CQRS
- [OpenAPI Nativo](modernization/native-openapi.md) - Guia de migração OpenAPI
- [HybridCache](modernization/hybrid-cache.md) - Guia de modernização de cache
