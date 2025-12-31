# Funcionalidades Nativas do .NET 9

Este documento fornece uma visão geral abrangente das funcionalidades nativas do .NET 9 adotadas pelo Mvp24Hours, substituindo implementações customizadas por APIs modernas e padronizadas.

## Visão Geral

O .NET 9 introduz diversas APIs que tornam implementações customizadas obsoletas. O Mvp24Hours adotou estas funcionalidades nativas para:

- **Reduzir carga de manutenção** - Menos código customizado significa menos bugs
- **Melhorar performance** - APIs nativas são altamente otimizadas
- **Aprimorar compatibilidade** - Melhor integração com o ecossistema .NET
- **Simplificar upgrades** - Seguir padrões Microsoft garante transições suaves

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                   Funcionalidades Nativas .NET 9 no Mvp24Hours             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐             │
│  │   Resiliência   │  │      Cache      │  │ Observabilidade │             │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤             │
│  │ Http.Resilience │  │  HybridCache    │  │  OpenTelemetry  │             │
│  │ Extensions.     │  │  Output Cache   │  │  ILogger        │             │
│  │  Resilience     │  │  IMemoryCache   │  │  ActivitySource │             │
│  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘             │
│           │                    │                    │                       │
│  ┌────────┴────────┐  ┌────────┴────────┐  ┌────────┴────────┐             │
│  │  Rate Limiting  │  │  Configuração   │  │     Hosting     │             │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤             │
│  │ RateLimiter     │  │  IOptions<T>    │  │  .NET Aspire    │             │
│  │ FixedWindow     │  │  TimeProvider   │  │  Keyed Services │             │
│  │ SlidingWindow   │  │  PeriodicTimer  │  │  Source Gen     │             │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘             │
│                                                                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐             │
│  │      APIs       │  │     Channels    │  │ Tratamento Erros│             │
│  ├─────────────────┤  ├─────────────────┤  ├─────────────────┤             │
│  │  Minimal APIs   │  │  Channel<T>     │  │ ProblemDetails  │             │
│  │  TypedResults   │  │  BoundedChannel │  │  RFC 7807       │             │
│  │  Native OpenAPI │  │  Producer/      │  │  TypedResults   │             │
│  │                 │  │   Consumer      │  │   .Problem()    │             │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘             │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Categorias de Funcionalidades

### 1. Resiliência e Tolerância a Falhas

| Funcionalidade | Descrição | Documentação |
|----------------|-----------|--------------|
| **Resiliência HTTP** | `Microsoft.Extensions.Http.Resilience` para clientes HTTP | [Resiliência HTTP](http-resilience.md) |
| **Resiliência Genérica** | `Microsoft.Extensions.Resilience` para qualquer operação | [Resiliência Genérica](generic-resilience.md) |
| **Rate Limiting** | `System.Threading.RateLimiting` para throttling | [Rate Limiting](rate-limiting.md) |

### 2. Cache

| Funcionalidade | Descrição | Documentação |
|----------------|-----------|--------------|
| **HybridCache** | Cache L1 (memória) + L2 (distribuído) | [HybridCache](hybrid-cache.md) |
| **Output Caching** | Cache de respostas HTTP | [Output Caching](output-caching.md) |

### 3. Tempo e Agendamento

| Funcionalidade | Descrição | Documentação |
|----------------|-----------|--------------|
| **TimeProvider** | Abstração para `DateTime.Now` / `DateTime.UtcNow` | [TimeProvider](time-provider.md) |
| **PeriodicTimer** | Timer moderno com suporte async | [PeriodicTimer](periodic-timer.md) |

### 4. Configuração

| Funcionalidade | Descrição | Documentação |
|----------------|-----------|--------------|
| **IOptions<T>** | Configuração fortemente tipada | [Configuração de Options](options-configuration.md) |
| **Keyed Services** | DI com resolução baseada em chave | [Keyed Services](keyed-services.md) |

### 5. Comunicação

| Funcionalidade | Descrição | Documentação |
|----------------|-----------|--------------|
| **Channels** | Producer/consumer de alta performance | [Channels](channels.md) |

### 6. APIs e Documentação

| Funcionalidade | Descrição | Documentação |
|----------------|-----------|--------------|
| **Minimal APIs** | Endpoints leves com TypedResults | [Minimal APIs](minimal-apis.md) |
| **ProblemDetails** | Respostas de erro RFC 7807 | [ProblemDetails](problem-details.md) |
| **OpenAPI Nativo** | Suporte OpenAPI integrado | [OpenAPI Nativo](native-openapi.md) |

### 7. Performance

| Funcionalidade | Descrição | Documentação |
|----------------|-----------|--------------|
| **Source Generators** | Geração de código AOT-friendly | [Source Generators](source-generators.md) |

### 8. Cloud-Native

| Funcionalidade | Descrição | Documentação |
|----------------|-----------|--------------|
| **.NET Aspire** | Observabilidade e orquestração | [.NET Aspire](aspire.md) |

## Novidades do .NET 9

### HybridCache (Estável)

O HybridCache agora é estável no .NET 9, fornecendo:

- **Cache L1 + L2** - Cache em memória e distribuído combinados
- **Proteção contra stampede** - Previne cache stampede automaticamente
- **Invalidação por tags** - Invalida entradas relacionadas eficientemente

```csharp
// Registrar HybridCache
services.AddMvpHybridCache(options =>
{
    options.DefaultEntryOptions.Expiration = TimeSpan.FromMinutes(5);
    options.DefaultEntryOptions.LocalCacheExpiration = TimeSpan.FromMinutes(1);
});

// Usar no seu código
var user = await hybridCache.GetOrCreateAsync(
    $"user:{userId}",
    async cancel => await userRepository.GetByIdAsync(userId, cancel),
    options: new() { Expiration = TimeSpan.FromHours(1) }
);
```

### OpenAPI Nativo

O .NET 9 inclui suporte OpenAPI integrado via `Microsoft.AspNetCore.OpenAPI`:

```csharp
// Adicionar OpenAPI nativo
builder.Services.AddMvp24HoursNativeOpenApi(options =>
{
    options.Title = "Minha API";
    options.Version = "v1";
    options.EnableSecuritySchemes = true;
});

// Mapear endpoint OpenAPI
app.MapMvp24HoursNativeOpenApi();
```

### TypedResults.InternalServerError

O .NET 9 adiciona `TypedResults.InternalServerError()`:

```csharp
// Antes do .NET 9
return Results.StatusCode(500);

// .NET 9+
return TypedResults.InternalServerError();

// Com Mvp24Hours
return businessResult.ToNativeTypedResult();
```

### .NET Aspire 9

O .NET Aspire 9 fornece suporte cloud-native abrangente:

```csharp
// Adicionar service defaults do Aspire
builder.AddMvp24HoursAspireDefaults();

// Adicionar Redis da configuração Aspire
builder.AddMvp24HoursRedisFromAspire("cache");

// Adicionar RabbitMQ da configuração Aspire
builder.AddMvp24HoursRabbitMQFromAspire("messaging");
```

## Início Rápido

### 1. Habilitar Todas as Funcionalidades Modernas

```csharp
var builder = WebApplication.CreateBuilder(args);

// Adicionar Mvp24Hours com funcionalidades .NET 9
builder.Services
    // Observabilidade
    .AddMvp24HoursObservability()
    
    // Cache
    .AddMvpHybridCache()
    
    // Resiliência
    .AddMvpStandardResilience()
    
    // Validação de configuração
    .AddOptionsWithValidation<MyOptions>("MyOptions")
    
    // Abstração de tempo
    .AddTimeProvider();

var app = builder.Build();

// Adicionar middleware
app.UseNativeProblemDetailsHandling();

// Adicionar OpenAPI nativo
app.MapMvp24HoursNativeOpenApi();

// Adicionar Output Caching
app.UseOutputCache();

app.Run();
```

### 2. Habilitar Integração Aspire

Para aplicações cloud-native:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Adicionar service defaults do Aspire (inclui observabilidade, health checks, resiliência)
builder.AddMvp24HoursAspireDefaults();

// Adicionar componentes distribuídos
builder.AddMvp24HoursRedisFromAspire("redis");
builder.AddMvp24HoursSqlServerFromAspire("sql");
builder.AddMvp24HoursRabbitMQFromAspire("rabbitmq");

var app = builder.Build();

// Mapear health checks para dashboard Aspire
app.MapMvp24HoursAspireHealthChecks();

app.Run();
```

## Caminho de Migração

Para instruções detalhadas de migração das implementações legadas do Mvp24Hours para APIs nativas do .NET 9, consulte o [Guia de Migração](migration-guide.md).

### Resumo de Breaking Changes

| Legado (Deprecated) | Nativo (.NET 9) |
|---------------------|-----------------|
| `TelemetryHelper` | `ILogger` + OpenTelemetry |
| `HttpClientExtensions` | `Microsoft.Extensions.Http.Resilience` |
| `MvpExecutionStrategy` | `Microsoft.Extensions.Resilience` |
| `MultiLevelCache` | `HybridCache` |
| `RetryPipelineMiddleware` | `NativePipelineResilienceMiddleware` |
| `CircuitBreakerPipelineMiddleware` | `NativePipelineResilienceMiddleware` |
| Rate Limiting Customizado | `System.Threading.RateLimiting` |
| Timers Customizados | `TimeProvider` + `PeriodicTimer` |

## Compatibilidade

### Requisitos Mínimos

- **.NET 9.0** ou posterior
- **C# 13** para suporte completo às funcionalidades da linguagem

### Compatibilidade Retroativa

Todas as APIs deprecated permanecem funcionais, mas emitirão warnings do compilador. Migre para APIs nativas no seu tempo antes da próxima versão major.

## Benefícios de Performance

| Funcionalidade | Melhoria |
|----------------|----------|
| HybridCache | Até 50% mais rápido que cache multi-level customizado |
| Source Generators | Overhead de reflexão em runtime próximo de zero |
| OpenAPI Nativo | 30% mais rápido na geração de documentos |
| PeriodicTimer | Alocações de memória reduzidas vs System.Timers.Timer |
| Channels | Padrões producer/consumer lock-free |

## Veja Também

- [Guia de Migração](migration-guide.md) - Instruções de migração passo a passo
- [Novidades do .NET 9](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9/overview)
- [Novidades do ASP.NET Core 9](https://learn.microsoft.com/aspnet/core/release-notes/aspnetcore-9.0)

