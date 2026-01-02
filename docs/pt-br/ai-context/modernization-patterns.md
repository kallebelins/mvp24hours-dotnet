# Padrões de Modernização para Agentes de IA (.NET 9)

> **Instrução para Agente de IA**: Use estes padrões para implementar funcionalidades modernas do .NET 9. Aplique com base nos requisitos do projeto e recomendações da matriz de decisão.

---

## Resiliência HTTP (Microsoft.Extensions.Http.Resilience)

### Instalação de Pacotes

```xml
<PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="9.*" />
```

### Configuração

```csharp
// appsettings.json
{
  "HttpClients": {
    "ExternalAPI": {
      "BaseUrl": "https://api.externa.com",
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
        options.CircuitBreaker.BreakDuration = TimeSpan.Parse(configuration["Resilience:CircuitBreakerBreakDuration"]);

        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
    });

    return services;
}
```

---

## HybridCache (.NET 9)

### Instalação de Pacotes

```xml
<PackageReference Include="Microsoft.Extensions.Caching.Hybrid" Version="9.*" />
```

### Configuração

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

### Uso

```csharp
public class ClienteService : IClienteService
{
    private readonly HybridCache _cache;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public ClienteService(HybridCache cache, IUnitOfWorkAsync unitOfWork)
    {
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<ClienteDto> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            $"cliente:{id}",
            async ct =>
            {
                var repository = _unitOfWork.GetRepository<Cliente>();
                var cliente = await repository.GetByIdAsync(id);
                return _mapper.Map<ClienteDto>(cliente);
            },
            new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(10),
                LocalCacheExpiration = TimeSpan.FromMinutes(2)
            },
            cancellationToken: cancellationToken);
    }

    public async Task InvalidarCacheAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync($"cliente:{id}", cancellationToken);
    }
}
```

---

## Rate Limiting

### Configuração

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
        // Rate limiter global
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

        // Política nomeada para endpoints específicos
        options.AddFixedWindowLimiter("api", opt =>
        {
            opt.PermitLimit = 50;
            opt.Window = TimeSpan.FromMinutes(1);
            opt.QueueLimit = 5;
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                Message = "Muitas requisições. Por favor, tente novamente mais tarde.",
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

// Uso no Controller
[EnableRateLimiting("api")]
[ApiController]
[Route("api/[controller]")]
public class ClienteController : ControllerBase
{
    [HttpGet]
    [EnableRateLimiting("sliding")]
    public async Task<IActionResult> ObterTodos() { }

    [HttpPost]
    [DisableRateLimiting]
    public async Task<IActionResult> Criar([FromBody] ClienteCreateDto dto) { }
}
```

---

## Keyed Services

### Configuração

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddKeyedServices(this IServiceCollection services)
{
    // Estratégias de banco de dados
    services.AddKeyedScoped<IDatabaseStrategy, SqlServerStrategy>("sqlserver");
    services.AddKeyedScoped<IDatabaseStrategy, PostgresStrategy>("postgres");
    services.AddKeyedScoped<IDatabaseStrategy, MongoDbStrategy>("mongodb");

    // Estratégias de cache
    services.AddKeyedSingleton<ICacheStrategy, RedisCacheStrategy>("redis");
    services.AddKeyedSingleton<ICacheStrategy, MemoryCacheStrategy>("memory");

    // Estratégias de notificação
    services.AddKeyedTransient<INotificationService, EmailNotificationService>("email");
    services.AddKeyedTransient<INotificationService, SmsNotificationService>("sms");
    services.AddKeyedTransient<INotificationService, PushNotificationService>("push");

    return services;
}
```

### Uso

```csharp
public class ClienteService : IClienteService
{
    private readonly IDatabaseStrategy _database;
    private readonly ICacheStrategy _cache;
    private readonly INotificationService _emailNotification;

    public ClienteService(
        [FromKeyedServices("sqlserver")] IDatabaseStrategy database,
        [FromKeyedServices("redis")] ICacheStrategy cache,
        [FromKeyedServices("email")] INotificationService emailNotification)
    {
        _database = database;
        _cache = cache;
        _emailNotification = emailNotification;
    }
}

// Controller com keyed services
[ApiController]
[Route("api/[controller]")]
public class NotificacaoController : ControllerBase
{
    [HttpPost("email")]
    public async Task<IActionResult> EnviarEmail(
        [FromKeyedServices("email")] INotificationService service,
        [FromBody] NotificacaoDto dto)
    {
        await service.EnviarAsync(dto);
        return Ok();
    }

    [HttpPost("sms")]
    public async Task<IActionResult> EnviarSms(
        [FromKeyedServices("sms")] INotificationService service,
        [FromBody] NotificacaoDto dto)
    {
        await service.EnviarAsync(dto);
        return Ok();
    }
}
```

---

## ProblemDetails (RFC 7807)

### Configuração

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

### ProblemDetails Customizado

```csharp
public static class ProblemDetailsExtensions
{
    public static IResult ToProblemDetails(this ValidationResult validationResult, HttpContext context)
    {
        return Results.Problem(
            title: "Erro de Validação",
            detail: "Um ou mais erros de validação ocorreram.",
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
            title: "Recurso Não Encontrado",
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

## Minimal APIs com TypedResults

### Definição de Endpoints

```csharp
public static class ClienteEndpoints
{
    public static void MapClienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clientes")
            .WithTags("Clientes")
            .WithOpenApi();

        group.MapGet("/", ObterTodos)
            .WithName("ObterTodosClientes")
            .WithSummary("Obtém todos os clientes com paginação")
            .Produces<IPagingResult<ClienteDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", ObterPorId)
            .WithName("ObterClientePorId")
            .WithSummary("Obtém cliente por ID")
            .Produces<ClienteDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/", Criar)
            .WithName("CriarCliente")
            .WithSummary("Cria um novo cliente")
            .Produces<ClienteDto>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", Atualizar)
            .WithName("AtualizarCliente")
            .WithSummary("Atualiza um cliente existente")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", Excluir)
            .WithName("ExcluirCliente")
            .WithSummary("Exclui um cliente")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<Results<Ok<IPagingResult<ClienteDto>>, ProblemHttpResult>> ObterTodos(
        [AsParameters] PaginationParams pagination,
        IClienteService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ObterTodosAsync(pagination.Pagina, pagination.Limite, cancellationToken);
        return TypedResults.Ok(result);
    }

    private static async Task<Results<Ok<ClienteDto>, NotFound<ProblemDetails>>> ObterPorId(
        Guid id,
        IClienteService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ObterPorIdAsync(id, cancellationToken);
        if (!result.HasData)
            return TypedResults.NotFound(new ProblemDetails
            {
                Title = "Cliente Não Encontrado",
                Detail = $"Cliente com ID {id} não foi encontrado."
            });

        return TypedResults.Ok(result.Data);
    }

    private static async Task<Results<Created<ClienteDto>, ValidationProblem>> Criar(
        ClienteCreateDto dto,
        IValidator<ClienteCreateDto> validator,
        IClienteService service,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var result = await service.CriarAsync(dto, cancellationToken);
        return TypedResults.Created($"/api/clientes/{result.Data.Id}", result.Data);
    }
}

public record PaginationParams(int Pagina = 1, int Limite = 10);
```

---

## TimeProvider (Testabilidade)

### Configuração

```csharp
// ServiceBuilderExtensions.cs
public static IServiceCollection AddTimeProvider(this IServiceCollection services)
{
    services.AddSingleton(TimeProvider.System);
    return services;
}
```

### Uso

```csharp
public class ClienteService : IClienteService
{
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWorkAsync _unitOfWork;

    public ClienteService(TimeProvider timeProvider, IUnitOfWorkAsync unitOfWork)
    {
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<IBusinessResult<ClienteDto>> CriarAsync(ClienteCreateDto dto)
    {
        var cliente = new Cliente
        {
            Nome = dto.Nome,
            Email = dto.Email,
            Created = _timeProvider.GetUtcNow().DateTime
        };

        await _unitOfWork.GetRepository<Cliente>().AddAsync(cliente);
        await _unitOfWork.SaveChangesAsync();

        return new BusinessResult<ClienteDto>(_mapper.Map<ClienteDto>(cliente));
    }
}

// Teste unitário com FakeTimeProvider
public class ClienteServiceTests
{
    [Fact]
    public async Task CriarAsync_DeveUsarHoraAtual()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new ClienteService(fakeTime, _mockUnitOfWork.Object);

        var result = await service.CriarAsync(new ClienteCreateDto { Nome = "Teste" });

        Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), result.Data.Created);
    }
}
```

---

## OpenAPI Nativo (.NET 9)

### Configuração

```csharp
// Program.cs
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "NomeProjeto API";
        document.Info.Version = "v1";
        document.Info.Description = "API para gerenciamento de clientes";
        document.Info.Contact = new OpenApiContact
        {
            Name = "Suporte",
            Email = "suporte@exemplo.com"
        };
        return Task.CompletedTask;
    });
});

// Configuração do app
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "NomeProjeto API v1");
    });
}
```

---

## Documentação Relacionada

- [Templates de Arquitetura](ai-context/architecture-templates.md)
- [Matriz de Decisão](ai-context/decision-matrix.md)
- [Padrões de Observabilidade](ai-context/observability-patterns.md)

