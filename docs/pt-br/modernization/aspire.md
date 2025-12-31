# Integração com .NET Aspire 9

## Visão Geral

O .NET Aspire 9 é o stack oficial da Microsoft para construir aplicações cloud-native observáveis. Ele fornece uma abordagem unificada para telemetria, health checks, resiliência e descoberta de serviços, facilitando o desenvolvimento e operação de sistemas distribuídos.

O Mvp24Hours oferece integração perfeita com o .NET Aspire, permitindo que você aproveite seus recursos poderosos enquanto usa o framework Mvp24Hours.

## Principais Funcionalidades

### Observabilidade
- **Integração OpenTelemetry**: Suporte nativo para logs, traces e métricas
- **Dashboard do Desenvolvedor**: Visualização em tempo real dos dados de telemetria
- **Telemetria do Navegador**: Suporte para SPAs enviarem telemetria diretamente para o dashboard

### Orquestração
- **Padrão AppHost**: Desenvolvimento local simplificado com orquestração de containers
- **Descoberta de Serviços**: Injeção automática de connection strings
- **Health Checks**: Probes de liveness e readiness prontos para uso

### Resiliência
- **Políticas de Retry**: Retry automático com backoff exponencial
- **Circuit Breaker**: Proteção contra falhas em cascata
- **Políticas de Timeout**: Timeouts configuráveis para todas as operações

## Instalação

Adicione os pacotes NuGet necessários ao seu projeto:

```bash
# Para o projeto API/Serviço
dotnet add package Aspire.Hosting.AppHost --version 9.*

# Para integrações de componentes
dotnet add package Aspire.StackExchange.Redis
dotnet add package Aspire.RabbitMQ.Client
dotnet add package Aspire.Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Aspire.MongoDB.Driver
```

## Início Rápido

### 1. Criar um Projeto AppHost

O projeto AppHost orquestra sua aplicação distribuída:

```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Adicionar componentes de infraestrutura
var redis = builder.AddRedis("cache")
    .WithDataVolume();

var rabbitmq = builder.AddRabbitMQ("messaging")
    .WithManagementPlugin();

var sql = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("appdb");

var mongo = builder.AddMongoDB("mongo")
    .AddDatabase("documents");

// Adicionar seu projeto API com referências
builder.AddProject<Projects.MyApi>("api")
    .WithReference(redis)
    .WithReference(rabbitmq)
    .WithReference(sql)
    .WithReference(mongo)
    .WithExternalHttpEndpoints();

builder.Build().Run();
```

### 2. Configurar seu Projeto API

Use as extensões Aspire do Mvp24Hours na sua API:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Adicionar service defaults do Aspire com integração Mvp24Hours
builder.AddMvp24HoursAspireDefaults(options =>
{
    options.ServiceName = "MyApi";
    options.EnableOpenTelemetry = true;
    options.EnableHealthChecks = true;
    options.EnableResilience = true;
    
    // Configurar telemetria
    options.Telemetry.EnableTracing = true;
    options.Telemetry.EnableMetrics = true;
    options.Telemetry.EnableMvp24HoursInstrumentation = true;
});

// Adicionar componentes Mvp24Hours usando conexões Aspire
builder.Services.AddMvp24HoursRedisFromAspire("cache");
builder.Services.AddMvp24HoursRabbitMQFromAspire("messaging");
builder.Services.AddMvp24HoursSqlServerFromAspire("appdb");
builder.Services.AddMvp24HoursMongoDbFromAspire("documents");

// Adicionar seus serviços
builder.Services.AddMvp24HoursDbContext<MyDbContext>();
builder.Services.AddMvpRabbitMQ();

var app = builder.Build();

// Mapear endpoints de health check do Aspire
app.MapMvp24HoursAspireHealthChecks();

app.Run();
```

## Opções de Configuração

### AspireOptions

| Propriedade | Tipo | Padrão | Descrição |
|-------------|------|--------|-----------|
| `ServiceName` | string | Nome do Assembly | Nome do serviço para telemetria |
| `ServiceVersion` | string | Versão do Assembly | Versão do serviço para telemetria |
| `Environment` | string | Ambiente do host | Ambiente de deploy |
| `EnableOpenTelemetry` | bool | true | Habilitar integração OpenTelemetry |
| `EnableHealthChecks` | bool | true | Habilitar health checks |
| `EnableResilience` | bool | true | Habilitar políticas de resiliência |
| `EnableServiceDiscovery` | bool | true | Habilitar descoberta de serviços |
| `OtlpEndpoint` | string | null | Endpoint do exportador OTLP |

### Opções de Telemetria

```csharp
options.Telemetry.EnableLogging = true;
options.Telemetry.EnableTracing = true;
options.Telemetry.EnableMetrics = true;
options.Telemetry.EnableAspNetCoreInstrumentation = true;
options.Telemetry.EnableHttpClientInstrumentation = true;
options.Telemetry.EnableEfCoreInstrumentation = true;
options.Telemetry.EnableMvp24HoursInstrumentation = true;
options.Telemetry.TraceSamplingRatio = 1.0;
```

### Opções de Health Check

```csharp
options.HealthChecks.LivenessPath = "/health/live";
options.HealthChecks.ReadinessPath = "/health/ready";
options.HealthChecks.StartupPath = "/health/startup";
options.HealthChecks.EnableDatabaseHealthChecks = true;
options.HealthChecks.EnableCacheHealthChecks = true;
options.HealthChecks.EnableMessagingHealthChecks = true;
```

### Opções de Resiliência

```csharp
options.Resilience.EnableRetry = true;
options.Resilience.EnableCircuitBreaker = true;
options.Resilience.EnableTimeout = true;
options.Resilience.MaxRetryAttempts = 3;
options.Resilience.CircuitBreakerFailureThreshold = 5;
options.Resilience.CircuitBreakerBreakDurationSeconds = 30;
options.Resilience.TimeoutSeconds = 30;
```

## Integração de Componentes

### Redis

```csharp
// No AppHost
var redis = builder.AddRedis("cache");

// Na API
builder.Services.AddMvp24HoursRedisFromAspire("cache");

// Usar com caching do Mvp24Hours
builder.Services.AddMvpHybridCache();
```

### RabbitMQ

```csharp
// No AppHost
var rabbitmq = builder.AddRabbitMQ("messaging");

// Na API
builder.Services.AddMvp24HoursRabbitMQFromAspire("messaging", options =>
{
    options.AutoDeclareQueues = true;
    options.EnableMessageDeduplication = true;
    options.PrefetchCount = 10;
});

// Usar com mensageria do Mvp24Hours
builder.Services.AddMvpRabbitMQ(cfg =>
{
    cfg.ConfigureEndpoints(context);
});
```

### SQL Server

```csharp
// No AppHost
var sql = builder.AddSqlServer("sql").AddDatabase("mydb");

// Na API
builder.Services.AddMvp24HoursSqlServerFromAspire("mydb");

// Usar com EFCore do Mvp24Hours
builder.Services.AddMvp24HoursDbContext<MyDbContext>(options =>
{
    options.UseSqlServer(builder.GetAspireConnectionString("mydb"));
});
```

### MongoDB

```csharp
// No AppHost
var mongo = builder.AddMongoDB("mongo").AddDatabase("documents");

// Na API
builder.Services.AddMvp24HoursMongoDbFromAspire("documents");

// Usar com MongoDB do Mvp24Hours
builder.Services.AddMvp24HoursMongoDb<MyContext>();
```

## Endpoints de Health Check

A integração fornece endpoints de health check compatíveis com Kubernetes:

| Endpoint | Propósito | Tags |
|----------|-----------|------|
| `/health/live` | Probe de liveness | live |
| `/health/ready` | Probe de readiness | ready |
| `/health/startup` | Probe de startup | startup, live |
| `/health` | Saúde geral | all |

### Resposta do Health Check

```json
{
  "status": "Healthy",
  "duration": 45.23,
  "checks": [
    {
      "name": "self",
      "status": "Healthy",
      "duration": 0.12,
      "tags": ["live"]
    },
    {
      "name": "redis-cache",
      "status": "Healthy",
      "duration": 15.45,
      "tags": ["ready"]
    },
    {
      "name": "sqlserver-mydb",
      "status": "Healthy",
      "duration": 28.67,
      "tags": ["ready"]
    }
  ]
}
```

## Dashboard do Desenvolvedor

O Dashboard do Desenvolvedor Aspire fornece observabilidade em tempo real:

### Funcionalidades
- **Logs Estruturados**: Filtre e pesquise logs com correlação de traces
- **Traces Distribuídos**: Visualize fluxos de requisição entre serviços
- **Métricas**: Visualize gráficos e dashboards de métricas-chave
- **Recursos**: Monitore saúde e status de todos os componentes

### Acessando o Dashboard

Ao executar com o AppHost, a URL do dashboard é exibida no console:

```
Dashboard: https://localhost:18888
```

## Comparação: Aspire vs Configuração Manual

| Funcionalidade | Aspire | Manual |
|----------------|--------|--------|
| Setup OpenTelemetry | Automático | Configuração manual necessária |
| Health Checks | Endpoints nativos | Mapeamento manual |
| Connection Strings | Injetadas automaticamente | Configuração manual |
| Descoberta de Serviços | Nativo | Biblioteca externa necessária |
| Orquestração de Containers | Integrada | Docker Compose/K8s |
| Dashboard do Desenvolvedor | Incluído | Ferramentas externas (Jaeger, Grafana) |

### Quando Usar Aspire

- Novas aplicações cloud-native
- Arquiteturas de microsserviços
- Aplicações que requerem observabilidade abrangente
- Times que desejam desenvolvimento local simplificado

### Quando Usar Configuração Manual

- Aplicações existentes com observabilidade customizada
- Aplicações simples sem distribuição
- Ambientes sem suporte a containers
- Requisitos específicos de compliance

## Boas Práticas

### 1. Nomenclatura de Serviços

Use nomes de serviços consistentes e descritivos:

```csharp
options.ServiceName = "order-api";
options.ServiceVersion = "1.2.3";
```

### 2. Tags de Health Check

Use tags apropriadas para probes:

```csharp
services.AddHealthChecks()
    .AddCheck("db", () => ..., tags: new[] { "ready" })
    .AddCheck("cache", () => ..., tags: new[] { "ready" })
    .AddCheck("self", () => ..., tags: new[] { "live" });
```

### 3. Configuração de Resiliência

Configure resiliência baseada nos seus SLOs:

```csharp
options.Resilience.MaxRetryAttempts = 3;
options.Resilience.TimeoutSeconds = 30;
options.Resilience.CircuitBreakerFailureThreshold = 10;
```

### 4. Sampling de Telemetria

Ajuste o sampling para ambientes de produção:

```csharp
options.Telemetry.TraceSamplingRatio = 0.1; // 10% sampling em produção
```

## Solução de Problemas

### Connection String Não Encontrada

Certifique-se que o nome da conexão corresponde no AppHost e API:

```csharp
// AppHost
var redis = builder.AddRedis("cache"); // nome: "cache"

// API
builder.Services.AddMvp24HoursRedisFromAspire("cache"); // deve corresponder
```

### Health Checks Falhando

Verifique se os serviços estão referenciados corretamente:

```csharp
builder.AddProject<Projects.Api>("api")
    .WithReference(redis)  // Adicionar referência
    .WithReference(sql);   // Adicionar referência
```

### Telemetria Não Aparecendo

Verifique se o endpoint OTLP está configurado:

```csharp
options.OtlpEndpoint = "http://localhost:4317"; // Ou use a variável de ambiente OTEL_EXPORTER_OTLP_ENDPOINT
```

## Veja Também

- [Visão Geral da Observabilidade](../observability/home.md)
- [Configuração OpenTelemetry](../observability/exporters.md)
- [Health Checks](../webapi/health-checks.md)
- [Documentação .NET Aspire](https://learn.microsoft.com/dotnet/aspire/)

