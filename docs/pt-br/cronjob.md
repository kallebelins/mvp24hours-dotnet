# CronJob

Solução criada para permitir tarefas agendadas em background usando expressões CRON. Este módulo se integra com o modelo de hosting do .NET e fornece suporte a tracing OpenTelemetry para observabilidade.

## Funcionalidades

- **Suporte a Expressões CRON**: Formato padrão de 5 campos (minuto hora dia-do-mês mês dia-da-semana)
- **Suporte a Timezone**: Configure jobs para executar em fusos horários específicos
- **Tracing OpenTelemetry**: Tracing distribuído integrado via `CronJobActivitySource`
- **Integração com TimeProvider**: Abstração de tempo testável para testes unitários
- **PeriodicTimer**: Padrões modernos async/await com suporte a cancelamento
- **IAsyncDisposable**: Limpeza assíncrona apropriada de recursos
- **DI com Escopo**: Cada execução cria um novo escopo de DI para ciclo de vida correto dos serviços

## Instalação

```bash
dotnet add package Mvp24Hours.Infrastructure.CronJob
```

## Criando um Serviço CronJob

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Services;

public class MeuJobBackground : CronJobService<MeuJobBackground>
{
    private readonly ILogger<MeuJobBackground> _jobLogger;

    public MeuJobBackground(
        IScheduleConfig<MeuJobBackground> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ILogger<CronJobService<MeuJobBackground>> logger,
        ILogger<MeuJobBackground> jobLogger,
        TimeProvider? timeProvider = null)
        : base(config, hostApplication, rootServiceProvider, logger, timeProvider)
    {
        _jobLogger = jobLogger;
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        _jobLogger.LogInformation("Iniciando execução do job em background...");
        
        // Acesse serviços com escopo via _serviceProvider
        using var scope = _serviceProvider!.CreateScope();
        var meuServico = scope.ServiceProvider.GetRequiredService<IMeuServico>();
        
        await meuServico.ProcessarAsync(cancellationToken);
        
        _jobLogger.LogInformation("Job em background concluído com sucesso.");
    }
}
```

## Configuração

### Configuração Básica

```csharp
// Em Program.cs ou Startup.cs
builder.Services.AddCronJob<MeuJobBackground>(config =>
{
    config.CronExpression = "*/5 * * * *"; // A cada 5 minutos
    config.TimeZoneInfo = TimeZoneInfo.Utc;
});
```

### Overloads de Conveniência

```csharp
// Simples: apenas expressão CRON (usa timezone local)
services.AddCronJob<RelatorioHorarioJob>("0 * * * *"); // A cada hora

// Com timezone
services.AddCronJob<LimpezaDiariaJob>(
    "30 2 * * *", // Diariamente às 2:30 AM
    TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));

// Executar uma vez imediatamente (sem expressão CRON)
services.AddCronJobRunOnce<MigracaoBancoDadosJob>();
```

## Referência de Expressões CRON

| Expressão | Descrição |
|-----------|-----------|
| `* * * * *` | A cada minuto |
| `*/5 * * * *` | A cada 5 minutos |
| `0 * * * *` | A cada hora no minuto 0 |
| `0 0 * * *` | Diariamente à meia-noite |
| `0 0 * * 0` | Semanalmente aos domingos à meia-noite |
| `0 0 1 * *` | Mensalmente no dia 1 à meia-noite |
| `0 9 * * 1-5` | Dias úteis às 9h |

Use o [Crontab Guru](https://crontab.guru/) para construir e validar suas expressões.

## Tracing OpenTelemetry

O módulo fornece tracing integrado via `CronJobActivitySource`:

```csharp
// Configurar OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(CronJobActivitySource.SourceName) // "Mvp24Hours.CronJob"
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter();
    });
```

### Nomes de Activities

- `Mvp24Hours.CronJob.Execute` - Operações de execução do job
- `Mvp24Hours.CronJob.Schedule` - Operações de agendamento
- `Mvp24Hours.CronJob.Start` - Operações de inicialização
- `Mvp24Hours.CronJob.Stop` - Operações de parada

### Tags Semânticas

| Tag | Descrição |
|-----|-----------|
| `cronjob.name` | Nome do CronJob |
| `cronjob.expression` | Expressão CRON |
| `cronjob.timezone` | Timezone utilizado |
| `cronjob.duration_ms` | Duração da execução em milissegundos |
| `cronjob.success` | Se a execução foi bem-sucedida |
| `cronjob.execution_count` | Contagem total de execuções |

## Testando com TimeProvider

Use `FakeTimeProvider` do `Microsoft.Extensions.TimeProvider.Testing` para testes unitários:

```csharp
[Fact]
public async Task CronJob_Deve_Executar_No_Horario_Agendado()
{
    var fakeTimeProvider = new FakeTimeProvider();
    fakeTimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    
    var job = new MeuJobBackground(
        config,
        hostApplication,
        serviceProvider,
        logger,
        jobLogger,
        fakeTimeProvider);
    
    // Avançar o tempo para disparar a execução
    fakeTimeProvider.Advance(TimeSpan.FromMinutes(5));
    
    // Assert que o job executou
}
```

## Acessando Serviços com Escopo

Cada execução cria um novo escopo de DI. Acesse serviços via `_serviceProvider`:

```csharp
public override async Task DoWork(CancellationToken cancellationToken)
{
    // _serviceProvider já está com escopo para esta execução
    var dbContext = _serviceProvider!.GetRequiredService<MeuDbContext>();
    var repository = _serviceProvider!.GetRequiredService<IMeuRepository>();
    
    // Serviços têm escopo apropriado e são descartados após a execução
    await repository.ProcessarItensPendentesAsync(cancellationToken);
}
```

## Encerramento Gracioso

O `CronJobService` trata adequadamente cancelamento e encerramento gracioso:

- Suporta propagação de `CancellationToken`
- Implementa `IAsyncDisposable` para limpeza assíncrona
- Usa `PeriodicTimer` para espera amigável a cancelamento
- Descarta serviços com escopo após cada execução

## Propriedades

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `ExecutionCount` | `long` | Número total de execuções |
| `JobName` | `string` | Nome do tipo CronJob |
| `CronExpression` | `string` | Expressão CRON configurada |

## Arquitetura

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                       Arquitetura do Módulo CronJob                           │
├──────────────────────────────────────────────────────────────────────────────┤
│                                                                               │
│  ┌─────────────────────────────────────────────────────────────────────────┐ │
│  │                         CronJobService<T>                                │ │
│  │  ┌──────────────┐  ┌───────────────┐  ┌────────────────────────────┐   │ │
│  │  │  DoWork()    │  │ PeriodicTimer │  │   TimeProvider             │   │ │
│  │  │  (abstract)  │  │ (agendamento) │  │   (tempo testável)         │   │ │
│  │  └──────────────┘  └───────────────┘  └────────────────────────────┘   │ │
│  └─────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                          │
│                        ┌───────────┴───────────┐                             │
│                        ▼                       ▼                             │
│  ┌─────────────────────────────┐  ┌─────────────────────────────────────┐   │
│  │  ResilientCronJobService<T> │  │     AdvancedCronJobService<T>       │   │
│  │  ┌─────────────────────────┐│  │  ┌─────────────────────────────────┐│   │
│  │  │ • Política de Retry     ││  │  │ • ICronJobContext               ││   │
│  │  │ • Circuit Breaker       ││  │  │ • ICronJobStateStore            ││   │
│  │  │ • Prevenção Sobreposição││  │  │ • ICronJobDependency            ││   │
│  │  │ • Shutdown Gracioso     ││  │  │ • IDistributedCronJobLock       ││   │
│  │  └─────────────────────────┘│  │  │ • Event Hooks (Starting,        ││   │
│  └─────────────────────────────┘  │  │   Completed, Failed, etc.)      ││   │
│                                    │  └─────────────────────────────────┘│   │
│                                    └─────────────────────────────────────┘   │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐   │
│  │                       Camada de Observabilidade                        │   │
│  │  ┌───────────────┐  ┌────────────────┐  ┌────────────────────────┐   │   │
│  │  │ HealthChecks  │  │ Métricas       │  │ OpenTelemetry Tracing  │   │   │
│  │  │ (Healthy/     │  │ (execuções,    │  │ (CronJobActivitySource)│   │   │
│  │  │  Degraded/    │  │  falhas,       │  │                        │   │   │
│  │  │  Unhealthy)   │  │  duração)      │  │                        │   │   │
│  │  └───────────────┘  └────────────────┘  └────────────────────────┘   │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
│                                                                               │
│  ┌───────────────────────────────────────────────────────────────────────┐   │
│  │                        Camada de Configuração                          │   │
│  │  ┌───────────────────┐  ┌────────────────────┐  ┌─────────────────┐   │   │
│  │  │ IScheduleConfig<T>│  │ CronJobOptions<T>  │  │ appsettings.json│   │   │
│  │  │ (agenda básica)   │  │ (opções completas) │  │ (declarativo)   │   │   │
│  │  └───────────────────┘  └────────────────────┘  └─────────────────┘   │   │
│  └───────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Descrição dos Componentes

| Componente | Propósito |
|------------|-----------|
| `CronJobService<T>` | Classe base para todos os CronJobs com agendamento CRON |
| `ResilientCronJobService<T>` | Adiciona retry, circuit breaker e prevenção de sobreposição |
| `AdvancedCronJobService<T>` | Completo com contexto, estado, dependências e eventos |
| `CronJobActivitySource` | Tracing OpenTelemetry para observabilidade distribuída |
| `CronJobMetricsService` | Rastreia métricas de execução (contagem, duração, falhas) |
| `CronJobHealthCheck` | Health checks baseados em métricas e estado do job |

## Troubleshooting

### Problemas Comuns

#### Job Não Está Executando

1. **Verifique a expressão CRON**: Valide com [Crontab Guru](https://crontab.guru/)
2. **Verifique o timezone**: Certifique-se de que o timezone está configurado corretamente
3. **Verifique se está pausado**: Use `ICronJobController.GetStatusAsync()` para verificar o estado
4. **Verifique os logs**: Habilite logging de debug para `Mvp24Hours.Infrastructure.CronJob`

```csharp
// Habilitar logging de debug
builder.Logging.AddFilter("Mvp24Hours.Infrastructure.CronJob", LogLevel.Debug);
```

#### Job Executando Múltiplas Vezes

1. **Prevenção de sobreposição desabilitada**: Habilite `PreventOverlapping = true`
2. **Múltiplas instâncias**: Use distributed locking em ambientes de cluster
3. **Verifique expressão CRON**: Certifique-se de que a expressão corresponde à frequência esperada

```csharp
services.AddResilientCronJob<MeuJob>(config =>
{
    config.CronExpression = "*/5 * * * *";
    config.Resilience.PreventOverlapping = true; // Habilitar prevenção de sobreposição
});
```

#### Circuit Breaker Aberto

1. **Verifique threshold de falhas**: Padrão é 5 falhas consecutivas
2. **Verifique duração do break**: Padrão é 30 segundos
3. **Corrija o problema subjacente**: Resolva a causa raiz das falhas
4. **Reset manual**: Use métricas para monitorar o estado do circuit breaker

```csharp
// Verificar estado do circuit breaker via métricas
var metrics = serviceProvider.GetRequiredService<ICronJobMetrics>();
var state = metrics.GetCircuitBreakerState("MeuJob");
```

#### Vazamentos de Memória

1. **Descarte escopos corretamente**: Sempre use `using` com escopos de DI
2. **Não armazene serviços com escopo**: Evite fazer cache de serviços scoped em campos
3. **Verifique IAsyncDisposable**: Garanta disposal assíncrono apropriado

```csharp
public override async Task DoWork(CancellationToken cancellationToken)
{
    // CORRETO: Escopo é descartado após uso
    using var scope = _serviceProvider!.CreateScope();
    var service = scope.ServiceProvider.GetRequiredService<IMeuServico>();
    await service.ProcessarAsync(cancellationToken);
}
```

### Comandos de Diagnóstico

```bash
# Executar testes para verificar funcionalidade do módulo
dotnet test src/Tests/Mvp24Hours.Infrastructure.CronJob.Test --verbosity normal

# Verificar endpoint de health (se configurado)
curl http://localhost:5000/health/cronjobs

# Visualizar logs com saída estruturada
dotnet run | jq 'select(.Category | startswith("Mvp24Hours.Infrastructure.CronJob"))'
```

### Categorias de Log

| Categoria | Descrição |
|-----------|-----------|
| `Mvp24Hours.Infrastructure.CronJob.Services.CronJobService` | Execução base do job |
| `Mvp24Hours.Infrastructure.CronJob.Services.ResilientCronJobService` | Recursos de resiliência |
| `Mvp24Hours.Infrastructure.CronJob.Resiliency.CronJobCircuitBreaker` | Estado do circuit breaker |
| `Mvp24Hours.Infrastructure.CronJob.Observability.CronJobMetricsService` | Coleta de métricas |

## Veja Também

- [Funcionalidades Avançadas](cronjob-advanced.md) - Contexto, dependências, distributed locking, event hooks
- [Resiliência em CronJob](cronjob-resilience.md) - Retry, circuit breaker, prevenção de sobreposição
- [Observabilidade do CronJob](cronjob-observability.md) - Health checks, métricas, logging estruturado
- [Modernização PeriodicTimer](modernization/periodic-timer.md)
- [Abstração TimeProvider](modernization/time-provider.md)
- [Observabilidade](observability/home.md)
