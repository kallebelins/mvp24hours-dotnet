# Observabilidade de CronJob

Funcionalidades completas de observabilidade para serviços CronJob incluindo health checks, métricas, tracing distribuído e logging estruturado.

## Visão Geral

O módulo CronJob fornece observabilidade de nível enterprise através de:

- **Health Checks**: Monitorar status dos CronJobs via ASP.NET Core Health Checks
- **Métricas**: Métricas compatíveis com Prometheus via `ICronJobMetrics`
- **Tracing**: Tracing distribuído com OpenTelemetry `ActivitySource`
- **Logging Estruturado**: Logging de alta performance com source generators `[LoggerMessage]`

## Instalação

```bash
dotnet add package Mvp24Hours.Infrastructure.CronJob
```

## Configuração Rápida

Adicione todas as funcionalidades de observabilidade com uma única extensão:

```csharp
// Em Program.cs
builder.Services.AddMvp24HoursCronJobObservability();
```

Ou configure individualmente:

```csharp
// Adicionar apenas métricas
builder.Services.AddCronJobMetrics();

// Adicionar apenas health check
builder.Services.AddHealthChecks()
    .AddCronJobHealthCheck(name: "CronJobs", tags: new[] { "ready", "cronjob" });
```

## Health Checks

### Configuração

```csharp
// Adicionar health check de CronJob
builder.Services.AddHealthChecks()
    .AddCronJobHealthCheck(
        name: "CronJob Health",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "cronjob", "background" });

// Mapear endpoints de health
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
```

### Resposta do Health Check

O health check fornece status detalhado para cada CronJob registrado:

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0012345",
  "entries": {
    "CronJob Health": {
      "data": {
        "EmailSenderJob_LastExecution": "2024-12-31T10:00:00.0000000Z",
        "EmailSenderJob_NextExecution": "2024-12-31T10:05:00.0000000Z",
        "EmailSenderJob_ExecutionCount": 42,
        "EmailSenderJob_IsRunning": false,
        "ReportGeneratorJob_LastExecution": "2024-12-31T09:30:00.0000000Z",
        "ReportGeneratorJob_NextExecution": "2024-12-31T10:30:00.0000000Z",
        "ReportGeneratorJob_ExecutionCount": 12,
        "ReportGeneratorJob_IsRunning": true
      },
      "status": "Healthy"
    }
  }
}
```

### Interface ICronJobServiceStatus

Serviços CronJob implementam `ICronJobServiceStatus` para reporte de saúde:

```csharp
public interface ICronJobServiceStatus
{
    string JobName { get; }
    DateTimeOffset? LastExecutionTime { get; }
    DateTimeOffset? NextExecutionTime { get; }
    long ExecutionCount { get; }
    bool IsRunning { get; }
}
```

## Métricas

### Interface ICronJobMetrics

A interface `ICronJobMetrics` fornece uma forma padronizada de registrar métricas de CronJob:

```csharp
public interface ICronJobMetrics
{
    void RecordExecution(string jobType, double durationMs, bool success);
    void IncrementActive(string jobType);
    void DecrementActive(string jobType);
    void UpdateScheduledCount(int delta);
    void RecordLastExecutionAge(string jobType, double ageSeconds);
    void RecordRetry(string jobType, int attempt);
    void RecordSkipped(string jobType, string reason);
    void RecordCircuitBreakerStateChange(string jobType, string newState);
}
```

### Métricas Disponíveis

| Nome da Métrica | Tipo | Descrição |
|-----------------|------|-----------|
| `mvp24hours.cronjob.executions.total` | Counter | Número total de execuções de jobs |
| `mvp24hours.cronjob.executions.failed.total` | Counter | Número total de execuções com falha |
| `mvp24hours.cronjob.execution.duration` | Histogram | Duração das execuções em milissegundos |
| `mvp24hours.cronjob.active.count` | UpDownCounter | Número de jobs atualmente em execução |
| `mvp24hours.cronjob.scheduled.count` | UpDownCounter | Número de jobs agendados |
| `mvp24hours.cronjob.skipped.total` | Counter | Número total de execuções puladas |
| `mvp24hours.cronjob.retries.total` | Counter | Número total de tentativas de retry |
| `mvp24hours.cronjob.circuit_breaker.state_changes` | Counter | Mudanças de estado do circuit breaker |

### Configuração Prometheus

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(Mvp24HoursMeters.CronJob.Name) // "Mvp24Hours.CronJob"
            .AddPrometheusExporter();
    });

app.MapPrometheusScrapingEndpoint("/metrics");
```

### Métricas Customizadas

Você pode injetar `ICronJobMetrics` para registrar métricas customizadas:

```csharp
public class RelatorioCustomizadoJob : CronJobService<RelatorioCustomizadoJob>
{
    private readonly ICronJobMetrics _metrics;

    public RelatorioCustomizadoJob(
        ICronJobMetrics metrics,
        // ... outras dependências
    ) : base(/* ... */)
    {
        _metrics = metrics;
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await GerarRelatoriosAsync(cancellationToken);
            _metrics.RecordExecution(JobName, sw.ElapsedMilliseconds, success: true);
        }
        catch (Exception)
        {
            _metrics.RecordExecution(JobName, sw.ElapsedMilliseconds, success: false);
            throw;
        }
    }
}
```

## Tracing Distribuído

### Configuração do ActivitySource

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.CronJob.Name) // "Mvp24Hours.CronJob"
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    });
```

### Nomes de Activity

| Activity | Descrição |
|----------|-----------|
| `Mvp24Hours.CronJob.JobExecution` | Execução individual do job |
| `Mvp24Hours.CronJob.JobScheduling` | Operações de agendamento |

### Tags Semânticas

| Tag | Descrição |
|-----|-----------|
| `cronjob.name` | Nome do CronJob |
| `cronjob.expression` | Expressão CRON |
| `cronjob.timezone` | Timezone utilizado |
| `cronjob.duration_ms` | Duração da execução em milissegundos |
| `cronjob.success` | Se a execução foi bem-sucedida |
| `cronjob.execution_count` | Contagem total de execuções |
| `cronjob.retry.enabled` | Se retry está habilitado (ResilientCronJob) |
| `cronjob.circuit_breaker.enabled` | Se circuit breaker está habilitado |
| `cronjob.prevent_overlapping` | Se prevenção de sobreposição está habilitada |

### Exemplo de Tracing

```csharp
// Traces são criados automaticamente para cada execução
// Exemplo de span:
// - Name: Mvp24Hours.CronJob.JobExecution
// - Duration: 1234ms
// - Status: Ok
// - Tags:
//   - cronjob.name: EmailSenderJob
//   - cronjob.expression: */5 * * * *
//   - cronjob.success: true
//   - cronjob.execution_count: 42
```

## Logging Estruturado

### Logging de Alta Performance

O módulo usa `[LoggerMessage]` source-generated para logging de alta performance sem alocações:

```csharp
// Eventos automaticamente logados com dados estruturados:

// Iniciando job
// EventId: 1001, Level: Debug
// "CronJob starting. Name: {CronJobName}, Scheduler: {CronExpression}"

// Execução concluída
// EventId: 1003, Level: Debug  
// "CronJob execute once after. Name: {CronJobName}, Duration: {DurationMs}ms"

// Execução falhou
// EventId: 1005, Level: Error
// "CronJob execute once failure. Name: {CronJobName}, Duration: {DurationMs}ms"
```

### Referência de Event IDs

| EventId | Nível | Descrição |
|---------|-------|-----------|
| 1001 | Debug | CronJob iniciando |
| 1002 | Debug | Executar uma vez antes |
| 1003 | Debug | Executar uma vez depois |
| 1004 | Debug | Executar uma vez cancelado |
| 1005 | Error | Executar uma vez falha |
| 1006 | Debug | Executar uma vez finalizando |
| 1007 | Debug | Agendador iniciado |
| 1008 | Warning | Sem próxima ocorrência |
| 1009 | Debug | Próxima execução agendada |
| 1010 | Debug | Agendador cancelado |
| 1011 | Debug | Agendador parado |
| 1012 | Debug | Executar antes |
| 1013 | Debug | Executar depois |
| 1014 | Debug | Execução cancelada |
| 1015 | Error | Executar falha |
| 1016 | Debug | CronJob parando |
| 1017 | Debug | CronJob parado |
| 1018 | Warning | Shutdown timeout |
| 1019 | Warning | Pulado - circuit breaker aberto |
| 1020 | Warning | Pulado - sobreposição |
| 1021 | Warning | Execução timeout |
| 1022 | Warning | Tentativa de retry |
| 1023 | Debug | CronJob resiliente iniciando |
| 1024 | Debug | CronJob resiliente parando |

### Configuração de Log

```csharp
// Configurar níveis de log por namespace
builder.Logging.AddFilter("Mvp24Hours.Infrastructure.CronJob", LogLevel.Debug);

// Ou via appsettings.json
{
  "Logging": {
    "LogLevel": {
      "Mvp24Hours.Infrastructure.CronJob": "Debug"
    }
  }
}
```

## Exemplo Completo

```csharp
var builder = WebApplication.CreateBuilder(args);

// Adicionar CronJob com toda observabilidade
builder.Services.AddMvp24HoursCronJobObservability();

// Registrar seu CronJob
builder.Services.AddResilientCronJob<EnviadorEmailJob>(config =>
{
    config.CronExpression = "*/5 * * * *";
    config.TimeZoneInfo = TimeZoneInfo.Utc;
},
resilience =>
{
    resilience.EnableRetry = true;
    resilience.MaxRetryAttempts = 3;
    resilience.EnableCircuitBreaker = true;
    resilience.PreventOverlapping = true;
});

// Configurar OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(Mvp24HoursActivitySources.CronJob.Name)
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(Mvp24HoursMeters.CronJob.Name)
            .AddPrometheusExporter();
    });

var app = builder.Build();

// Endpoints de health check
app.MapHealthChecks("/health");
app.MapPrometheusScrapingEndpoint("/metrics");

app.Run();
```

## Integração com Aspire Dashboard

Ao usar .NET Aspire, todos os dados de observabilidade do CronJob são automaticamente visíveis no Aspire Dashboard:

```csharp
// No AppHost
var app = DistributedApplication.CreateBuilder(args);

var api = app.AddProject<Projects.MinhaApi>("api")
    .WithOtlpExporter();

app.Build().Run();
```

## Veja Também

- [Visão Geral do CronJob](cronjob.md)
- [Funcionalidades Avançadas](cronjob-advanced.md) - Contexto, dependências, distributed locking, event hooks
- [Resiliência do CronJob](cronjob-resilience.md)
- [Métricas OpenTelemetry](observability/metrics.md)
- [Tracing OpenTelemetry](observability/tracing.md)
- [Integração .NET Aspire](modernization/aspire.md)

