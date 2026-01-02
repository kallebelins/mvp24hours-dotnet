# ASP.NET Web API
Neste tópico você encontrará alguns recursos para acelerar a construção de serviços web com ASP.NET Web API.

## Serviços

```csharp
// Program.cs

var builder = WebApplication.CreateBuilder(args);

/// essencial
builder.Services.AddMvp24HoursWebEssential();

/// automapper
builder.Services.AddMvp24HoursMapService(assemblyMap: Assembly.GetExecutingAssembly());

/// json
builder.Services.AddMvp24HoursWebJson();

/// swagger (ou use OpenAPI Nativo - veja abaixo)
builder.Services.AddMvp24HoursSwagger("MyAPI");

/// compressão
builder.Services.AddMvp24HoursWebGzip();

/// exception middleware
builder.Services.AddMvp24HoursWebExceptions(options => { });

/// cors middleware
builder.Services.AddMvp24HoursWebCors(options => { });
```

### OpenAPI Nativo (.NET 9+)

Para projetos .NET 9+, use OpenAPI Nativo ao invés do Swashbuckle:

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

> 📚 Consulte [Documentação do OpenAPI Nativo](modernization/native-openapi.md) para guia completo.

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

// Aplicar em endpoints
app.MapGet("/api/limited", () => "Hello")
    .RequireRateLimiting("fixed");

// Ou aplicar em controllers
[EnableRateLimiting("fixed")]
public class MyController : ControllerBase { }
```

> 📚 Consulte [Documentação de Rate Limiting](modernization/rate-limiting.md) para guia completo.

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

// Aplicar em endpoints
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

> 📚 Consulte [Documentação de Security Headers](webapi-advanced.md#security-headers) para guia completo.

### Middleware de Idempotência

Para operações que devem ser idempotentes (ex: processamento de pagamentos):

```csharp
// Program.cs
builder.Services.AddMvp24HoursIdempotency(options =>
{
    options.IdempotencyKeyHeader = "X-Idempotency-Key";
    options.CacheExpiration = TimeSpan.FromHours(24);
    options.EnableForAllMethods = false; // Apenas POST, PUT, PATCH por padrão
});

var app = builder.Build();

app.UseMvp24HoursIdempotency();

// Ou aplicar em endpoints específicos
app.MapPost("/api/payments", handler)
    .WithIdempotency();
```

> 📚 Consulte [Documentação de Idempotência](webapi-advanced.md#idempotency) para guia completo.

## Aplicação / Middlewares

```csharp
// Program.cs

var app = builder.Build();

/// exception handlers
app.UseMvp24HoursExceptionHandling();

/// cors
app.UseMvp24HoursCors();

/// swagger (se usar Swashbuckle)
if (!app.Environment.IsProduction())
{
    app.UseMvp24HoursSwagger();
}

/// correlation-id
app.UseMvp24HoursCorrelationId();
```

## Observabilidade

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

> 📚 Consulte [Documentação de Observabilidade](observability/home.md) para guia completo.

## Requisições HTTP Resilientes
Use IHttpClientFactory para implementar solicitações de HTTP resilientes.

```csharp
// Program.cs

/// injetar httpclient usando nome personalizado com resiliência nativa
builder.Services.AddHttpClient("my-api-url", client =>
{
    client.BaseAddress = new Uri("https://myexampleapi.com");
})
.AddMvpStandardResilience();

/// injetar HttpClient usando nome da Classe
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

/// através do construtor, caso tenha registrado a classe cliente "AddHttpClient<MyClassClient>"
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

/// provedor de serviço usando nome personalizado
var factory = serviceProvider.GetService<IHttpClientFactory>();
var client = factory.CreateClient("my-api-url");
var result = await client.HttpGetAsync("api/myService");

/// provedor de serviço usando a classe de referência
var factory = serviceProvider.GetService<IHttpClientFactory>();
var client = factory.CreateClient(typeof(MyClassClient).Name);
var result = await client.HttpGetAsync("api/myService");
```

## Minimal API

### Usando TypedResults

Use `TypedResults` para respostas fortemente tipadas:

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

### Usando ExtensionBinder

Utilize a classe ExtensionBinder para manipular conversão de parâmetros em objeto:

```csharp
public class CustomerFilter : ExtensionBinder<CustomerFilter>
{
    public string Name { get; set; }
    public bool? Active { get; set; }
}
```

Você também pode usar a classe ModelBinder como parâmetro de entrada da Action, caso não queira atualizar suas classes existentes:

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

Caso tenha desejo de implementar sua própria conversão, use a interface IExtensionBinder:

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

## Documentação Relacionada

- [WebAPI Avançado](webapi-advanced.md) - Security headers, idempotência, versionamento de API
- [OpenAPI Nativo](modernization/native-openapi.md) - Documentação OpenAPI nativo do .NET 9
- [Rate Limiting](modernization/rate-limiting.md) - Padrões de limitação de taxa
- [ProblemDetails](modernization/problem-details.md) - Respostas de erro RFC 7807
- [Output Caching](modernization/output-caching.md) - Cache de resposta
- [Observabilidade](observability/home.md) - Logging, tracing, métricas
