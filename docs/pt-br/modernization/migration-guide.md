# Guia de Migração para APIs Nativas do .NET 9

Este guia fornece instruções passo a passo para migrar das implementações legadas do Mvp24Hours para as APIs nativas do .NET 9.

## Visão Geral

O Mvp24Hours adotou APIs nativas do .NET 9 para reduzir código customizado, melhorar performance e alinhar com padrões da indústria. Este guia cobre:

1. **Identificando código deprecated** - Encontrando implementações legadas
2. **Estratégias de migração** - Caminhos de migração passo a passo
3. **Testando alterações** - Validando que as migrações funcionam corretamente
4. **Procedimentos de rollback** - Revertendo se surgirem problemas

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                      Visão Geral do Caminho de Migração                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  Legado (Deprecated)                    Nativo (.NET 9)                     │
│  ───────────────────                    ────────────────                    │
│                                                                             │
│  TelemetryHelper        ───────────►    ILogger + OpenTelemetry             │
│  HttpClientExtensions   ───────────►    Microsoft.Extensions.Http.Resilience│
│  MvpExecutionStrategy   ───────────►    Microsoft.Extensions.Resilience     │
│  MultiLevelCache        ───────────►    HybridCache                         │
│  RetryPipelineMiddleware───────────►    NativePipelineResilienceMiddleware  │
│  Rate Limiting Custom   ───────────►    System.Threading.RateLimiting       │
│  System.Timers.Timer    ───────────►    TimeProvider + PeriodicTimer        │
│  Validação Manual       ───────────►    IValidateOptions<T> + ValidateOnStart│
│  Swashbuckle           ───────────►    Microsoft.AspNetCore.OpenAPI         │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 1. Migração de Telemetria

### De: `TelemetryHelper`
### Para: `ILogger` + OpenTelemetry

#### Antes (Deprecated)

```csharp
public class MyService
{
    public void DoWork()
    {
        TelemetryHelper.Execute(TelemetryLevels.Information, "Iniciando trabalho");
        
        try
        {
            // ... trabalho
            TelemetryHelper.Execute(TelemetryLevels.Information, "Trabalho concluído");
        }
        catch (Exception ex)
        {
            TelemetryHelper.Execute(TelemetryLevels.Error, $"Erro: {ex.Message}");
            throw;
        }
    }
}
```

#### Depois (Nativo)

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
        _logger.LogInformation("Iniciando trabalho");
        
        try
        {
            // ... trabalho
            _logger.LogInformation("Trabalho concluído");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao realizar trabalho");
            throw;
        }
    }
}
```

#### Configuração

```csharp
// Program.cs
builder.Services.AddMvp24HoursObservability(options =>
{
    options.ServiceName = "MeuServico";
    options.ServiceVersion = "1.0.0";
    options.EnableTracing = true;
    options.EnableMetrics = true;
});
```

---

## 2. Migração de Resiliência HTTP

### De: `HttpClientExtensions` / `HttpPolicyHelper`
### Para: `Microsoft.Extensions.Http.Resilience`

#### Antes (Deprecated)

```csharp
// Forma antiga com políticas Polly customizadas
services.AddHttpClient("MinhaApi")
    .AddPolicyHandler(HttpPolicyHelper.GetRetryPolicy(3))
    .AddPolicyHandler(HttpPolicyHelper.GetCircuitBreakerPolicy(5, TimeSpan.FromSeconds(30)));
```

#### Depois (Nativo)

```csharp
// Nova forma com resiliência nativa
services.AddHttpClient("MinhaApi", client =>
{
    client.BaseAddress = new Uri("https://api.exemplo.com");
})
.AddMvpStandardResilience()
// Ou com configuração customizada:
.AddMvpResilience(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
});
```

#### Usando Presets

```csharp
// Preset de alta disponibilidade (mais retries, timeouts maiores)
services.AddHttpClient("ApiCritica")
    .AddMvpResilience(NativeResilienceOptions.HighAvailability);

// Preset de baixa latência (menos retries, timeouts menores)
services.AddHttpClient("ApiRapida")
    .AddMvpResilience(NativeResilienceOptions.LowLatency);
```

---

## 3. Migração de Resiliência de Banco de Dados

### De: `MvpExecutionStrategy`
### Para: `Microsoft.Extensions.Resilience`

#### Antes (Deprecated)

```csharp
// Estratégia de execução customizada antiga
services.AddDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.ExecutionStrategy(c => new MvpExecutionStrategy(c));
    });
});
```

#### Depois (Nativo)

```csharp
// Nova resiliência nativa
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

## 4. Migração de Cache

### De: `MultiLevelCache`
### Para: `HybridCache`

#### Antes (Deprecated)

```csharp
// Cache multi-level antigo
services.AddMultiLevelCache(options =>
{
    options.L1Options.SizeLimit = 1000;
    options.L2ConnectionString = "redis:6379";
});

// Uso
var item = await multiLevelCache.GetOrSetAsync(
    "chave",
    async () => await CarregarDadosAsync(),
    TimeSpan.FromMinutes(5));
```

#### Depois (Nativo)

```csharp
// Novo HybridCache
services.AddMvpHybridCache(options =>
{
    options.DefaultEntryOptions.Expiration = TimeSpan.FromMinutes(5);
    options.DefaultEntryOptions.LocalCacheExpiration = TimeSpan.FromMinutes(1);
});

// Uso
var item = await hybridCache.GetOrCreateAsync(
    "chave",
    async cancel => await CarregarDadosAsync(cancel),
    new HybridCacheEntryOptions
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(1)
    });
```

#### Invalidação por Tags

```csharp
// Registrar com tags
await hybridCache.GetOrCreateAsync(
    "usuario:123",
    async cancel => await CarregarUsuarioAsync(123, cancel),
    tags: new[] { "usuarios", "usuario:123" });

// Invalidar por tag
await tagManager.InvalidateByTagAsync("usuarios");
```

---

## 5. Migração de Resiliência do Pipeline

### De: `RetryPipelineMiddleware` / `CircuitBreakerPipelineMiddleware`
### Para: `NativePipelineResilienceMiddleware`

#### Antes (Deprecated)

```csharp
// Middleware customizado antigo
services.AddPipelineResilience(options =>
{
    options.RetryCount = 3;
    options.RetryDelay = TimeSpan.FromSeconds(1);
    options.CircuitBreakerThreshold = 5;
});
```

#### Depois (Nativo)

```csharp
// Novo middleware de resiliência nativo
services.AddNativePipelineResilience(options =>
{
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.CircuitBreaker.FailureRatio = 0.5;
});
```

---

## 6. Migração de Rate Limiting

### De: Rate Limiting Customizado
### Para: `System.Threading.RateLimiting`

#### Antes (Customizado)

```csharp
// Implementação customizada de rate limiting
services.AddCustomRateLimiting(options =>
{
    options.PermitLimit = 100;
    options.Window = TimeSpan.FromMinutes(1);
});
```

#### Depois (Nativo)

```csharp
// Rate limiting nativo
services.AddNativeRateLimiting(options =>
{
    options.DefaultPolicy = NativeRateLimiterOptions.FixedWindow(
        permitLimit: 100,
        window: TimeSpan.FromMinutes(1));
});

// Para operações de pipeline
services.AddPipelineRateLimiting();

// Para consumers RabbitMQ
services.AddRabbitMQRateLimiting(options =>
{
    options.ConsumerLimit = NativeRateLimiterOptions.SlidingWindow(
        permitLimit: 50,
        window: TimeSpan.FromSeconds(10),
        segmentsPerWindow: 5);
});
```

---

## 7. Migração de Abstração de Tempo

### De: `DateTime.Now` / `DateTime.UtcNow`
### Para: `TimeProvider`

#### Antes (Uso Direto)

```csharp
public class MyService
{
    public void DoWork()
    {
        var agora = DateTime.UtcNow;
        var prazo = agora.AddHours(1);
        // ...
    }
}
```

#### Depois (TimeProvider)

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
        var agora = _timeProvider.GetUtcNow();
        var prazo = agora.AddHours(1);
        // ...
    }
}

// Registro
services.AddTimeProvider();

// Testes
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero));
services.ReplaceTimeProvider(fakeTime);
```

---

## 8. Migração de Timer

### De: `System.Timers.Timer`
### Para: `PeriodicTimer`

#### Antes (Timer Antigo)

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
        // Fazer trabalho
    }
}
```

#### Depois (PeriodicTimer)

```csharp
public class BackgroundWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            // Fazer trabalho
        }
    }
}
```

---

## 9. Migração de Validação de Options

### De: Validação Manual
### Para: `IValidateOptions<T>` + `ValidateOnStart`

#### Antes (Manual)

```csharp
public class MyOptions
{
    public string ConnectionString { get; set; }
    public int MaxRetries { get; set; }
}

// Validação no startup
var options = configuration.GetSection("MyOptions").Get<MyOptions>();
if (string.IsNullOrEmpty(options.ConnectionString))
    throw new InvalidOperationException("ConnectionString é obrigatório");
```

#### Depois (Nativo)

```csharp
public class MyOptions
{
    [Required]
    public string ConnectionString { get; set; } = default!;
    
    [Range(1, 10)]
    public int MaxRetries { get; set; } = 3;
}

// Registro com validação
services.AddOptionsWithValidation<MyOptions>("MyOptions");

// Validador customizado
public class MyOptionsValidator : OptionsValidatorBase<MyOptions>
{
    protected override void Validate(OptionsValidationContext<MyOptions> context)
    {
        context
            .EnsureNotNullOrEmpty(o => o.ConnectionString, "Connection string é obrigatório")
            .EnsureInRange(o => o.MaxRetries, 1, 10, "MaxRetries deve estar entre 1 e 10");
    }
}

services.AddOptionsWithValidation<MyOptions, MyOptionsValidator>("MyOptions");
```

---

## 10. Migração de OpenAPI

### De: Swashbuckle
### Para: `Microsoft.AspNetCore.OpenAPI`

#### Antes (Swashbuckle)

```csharp
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Minha API", Version = "v1" });
});

app.UseSwagger();
app.UseSwaggerUI();
```

#### Depois (OpenAPI Nativo)

```csharp
services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "Minha API";
    options.Version = "v1";
    options.Description = "Descrição da Minha API";
    options.EnableSecuritySchemes = true;
});

app.MapMvp24HoursNativeOpenApi();

// Ainda usar Swagger UI para visualização
app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Minha API v1"));
```

---

## Testando Migrações

### 1. Testes Unitários

```csharp
[Fact]
public async Task Deve_Usar_Resiliencia_Nativa()
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

### 2. Testes de Integração

```csharp
[Fact]
public async Task Deve_Fazer_Retry_Em_Falha_Transiente()
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

## Procedimentos de Rollback

### 1. Feature Flags

```csharp
services.AddFeatureManagement();

if (featureManager.IsEnabled("UsarResilienciaNativa"))
{
    services.AddHttpClient("api").AddMvpStandardResilience();
}
else
{
    // Legado
    services.AddHttpClient("api")
        .AddPolicyHandler(HttpPolicyHelper.GetRetryPolicy(3));
}
```

### 2. Alternância Baseada em Configuração

```json
{
  "Features": {
    "UsarApisNativas": true,
    "UsarHybridCache": true,
    "UsarOpenApiNativo": false
  }
}
```

```csharp
var usarNativo = configuration.GetValue<bool>("Features:UsarApisNativas");
if (usarNativo)
{
    services.AddMvpStandardResilience();
}
```

---

## Checklist

Use este checklist para acompanhar seu progresso de migração:

- [ ] **Telemetria**
  - [ ] Substituir `TelemetryHelper` por `ILogger`
  - [ ] Configurar exporters OpenTelemetry
  - [ ] Atualizar todas as chamadas de logging

- [ ] **Resiliência HTTP**
  - [ ] Substituir `HttpClientExtensions` por resiliência nativa
  - [ ] Atualizar registros de HttpClient
  - [ ] Testar comportamento de retry e circuit breaker

- [ ] **Resiliência de Banco de Dados**
  - [ ] Substituir `MvpExecutionStrategy` por resiliência nativa
  - [ ] Atualizar configurações de DbContext
  - [ ] Testar tratamento de falhas transientes

- [ ] **Cache**
  - [ ] Substituir `MultiLevelCache` por `HybridCache`
  - [ ] Atualizar padrões de acesso ao cache
  - [ ] Configurar invalidação por tags

- [ ] **Pipeline**
  - [ ] Substituir middleware customizado por nativo
  - [ ] Atualizar configurações de pipeline
  - [ ] Testar comportamento de resiliência

- [ ] **Rate Limiting**
  - [ ] Substituir rate limiting customizado
  - [ ] Configurar políticas por endpoint
  - [ ] Testar aplicação de limites

- [ ] **Tempo & Timers**
  - [ ] Substituir `DateTime.Now` por `TimeProvider`
  - [ ] Substituir `System.Timers.Timer` por `PeriodicTimer`
  - [ ] Atualizar testes para usar `FakeTimeProvider`

- [ ] **Configuração**
  - [ ] Adicionar validadores `IValidateOptions<T>`
  - [ ] Habilitar `ValidateOnStart()`
  - [ ] Testar validação de configuração

- [ ] **OpenAPI**
  - [ ] Substituir Swashbuckle por OpenAPI nativo
  - [ ] Configurar transformers
  - [ ] Atualizar configuração de UI

## Veja Também

- [Visão Geral das Funcionalidades .NET 9](dotnet9-features.md)
- [Resiliência HTTP](http-resilience.md)
- [Resiliência Genérica](generic-resilience.md)
- [HybridCache](hybrid-cache.md)
- [TimeProvider](time-provider.md)
- [OpenAPI Nativo](native-openapi.md)

