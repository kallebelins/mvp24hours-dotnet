# CronJob - Funcionalidades Avançadas

Este documento descreve as funcionalidades avançadas do módulo CronJob, incluindo configuração avançada, contexto de execução, expressões CRON de 6 campos, dependências entre jobs, distributed locking, persistência de estado, controle de pause/resume e hooks de eventos.

## Funcionalidades

- **CronJobOptions<T>**: Configuração abrangente via código ou appsettings.json
- **CronJobGlobalOptions**: Padrões globais para todos os CronJobs
- **Validação de Configuração**: Validação de expressões CRON no startup
- **Múltiplas Instâncias**: Registrar mesmo tipo de job com configurações diferentes
- **ICronJobContext**: Contexto de execução com metadados (JobId, StartTime, Attempt)
- **Expressões CRON de 6 campos**: Suporte a segundos para agendamento mais preciso
- **Job Dependencies**: Executar jobs após outros completarem
- **Distributed Locking**: Evitar execuções duplicadas em clusters
- **ICronJobStateStore**: Persistência de estado do job
- **Pause/Resume**: Controle de jobs em runtime
- **Event Hooks**: Callbacks para eventos do ciclo de vida do job

## Configuração Avançada

O módulo CronJob oferece opções de configuração abrangentes através de `CronJobOptions<T>` e `CronJobGlobalOptions`.

### CronJobOptions<T> - Configuração Por Job

Configure cada CronJob com controle total sobre agendamento, resiliência e observabilidade:

```csharp
services.AddCronJobWithOptions<MeuJob>(options =>
{
    // Agendamento
    options.CronExpression = "*/5 * * * *";   // A cada 5 minutos
    options.TimeZone = "UTC";                  // ou "America/Sao_Paulo", etc.
    options.Enabled = true;                    // Habilitar/desabilitar job
    options.Description = "Processa pedidos pendentes";
    
    // Resiliência
    options.EnableRetry = true;
    options.MaxRetryAttempts = 3;
    options.RetryDelay = TimeSpan.FromSeconds(5);
    options.UseExponentialBackoff = true;
    
    // Circuit Breaker
    options.EnableCircuitBreaker = true;
    options.CircuitBreakerFailureThreshold = 5;
    options.CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30);
    
    // Sobreposição e Shutdown
    options.PreventOverlapping = true;
    options.GracefulShutdownTimeout = TimeSpan.FromSeconds(30);
    
    // Distributed Locking
    options.EnableDistributedLocking = true;
    options.DistributedLockExpiry = TimeSpan.FromMinutes(5);
    
    // Observabilidade
    options.EnableObservability = true;
    options.EnableHealthCheck = true;
    
    // Dependências
    options.DependsOn = new[] { "JobColetaDados", "JobValidacao" };
});
```

### CronJobGlobalOptions - Padrões Globais

Configure padrões globais que se aplicam a todos os CronJobs:

```csharp
services.AddCronJobGlobalOptions(options =>
{
    // Timezone padrão para todos os jobs
    options.DefaultTimeZone = "UTC";
    options.JobsEnabledByDefault = true;
    
    // Configurações padrão de resiliência
    options.EnableRetryByDefault = true;
    options.DefaultMaxRetryAttempts = 3;
    options.DefaultRetryDelay = TimeSpan.FromSeconds(1);
    options.UseExponentialBackoffByDefault = true;
    
    // Circuit breaker padrão
    options.EnableCircuitBreakerByDefault = false;
    options.DefaultCircuitBreakerFailureThreshold = 5;
    options.DefaultCircuitBreakerBreakDuration = TimeSpan.FromSeconds(30);
    
    // Sobreposição e shutdown
    options.PreventOverlappingByDefault = true;
    options.DefaultGracefulShutdownTimeout = TimeSpan.FromSeconds(30);
    
    // Observabilidade
    options.EnableObservability = true;
    options.EnableHealthChecks = true;
    options.RegisterAggregateHealthCheck = true;
    options.AggregateHealthCheckName = "cronjobs";
    options.HealthCheckTags = new[] { "cronjob", "background" };
    
    // Validação
    options.ValidateCronExpressionsOnStartup = true;
    options.LogConfigurationWarnings = true;
});
```

### Configuração via appsettings.json

Configure CronJobs declarativamente usando `appsettings.json`:

```json
{
  "CronJobs": {
    "Global": {
      "DefaultTimeZone": "America/Sao_Paulo",
      "EnableObservability": true,
      "ValidateCronExpressionsOnStartup": true,
      "EnableRetryByDefault": true,
      "DefaultMaxRetryAttempts": 3
    },
    "JobProcessamentoPedidos": {
      "CronExpression": "*/5 * * * *",
      "TimeZone": "America/Sao_Paulo",
      "Enabled": true,
      "Description": "Processa pedidos pendentes a cada 5 minutos",
      "EnableRetry": true,
      "MaxRetryAttempts": 3,
      "PreventOverlapping": true
    },
    "JobGeracaoRelatorios": {
      "CronExpression": "0 0 * * *",
      "TimeZone": "America/Sao_Paulo",
      "Enabled": true,
      "Description": "Gera relatórios diários à meia-noite",
      "EnableCircuitBreaker": true,
      "CircuitBreakerFailureThreshold": 3
    }
  }
}
```

Registrar jobs a partir da configuração:

```csharp
// Carregar opções globais da configuração
services.AddCronJobGlobalOptionsFromConfiguration(configuration);

// Registrar jobs a partir da configuração
services.AddCronJobFromConfiguration<JobProcessamentoPedidos>(configuration);
services.AddResilientCronJobFromConfiguration<JobGeracaoRelatorios>(configuration);
services.AddAdvancedCronJobFromConfiguration<JobSincronizacaoDados>(configuration);
```

### Validação no Startup

Expressões CRON e configurações são validadas no startup:

```csharp
// Expressão inválida causará falha no startup da aplicação
services.AddCronJobWithOptions<MeuJob>(options =>
{
    options.CronExpression = "expressao invalida"; // ❌ Falhará no startup
});
```

A validação inclui:
- Sintaxe de expressão CRON (5 campos ou 6 campos)
- Validade do identificador de timezone
- Intervalos de parâmetros de retry e circuit breaker
- Validação de valores de timeout
- Formato do nome da instância (alfanumérico, hífens, underscores)

### Múltiplas Instâncias do Mesmo Tipo de Job

Registre múltiplas instâncias do mesmo tipo de job com configurações diferentes:

```csharp
// Via código
services.AddCronJobInstances<JobSincronizacaoDados>(
    new CronJobOptions<JobSincronizacaoDados>
    {
        InstanceName = "SincDados-US",
        CronExpression = "0 0 * * *",
        TimeZone = "America/New_York",
        Description = "Sincronização de dados US à meia-noite EST"
    },
    new CronJobOptions<JobSincronizacaoDados>
    {
        InstanceName = "SincDados-EU",
        CronExpression = "0 0 * * *",
        TimeZone = "Europe/London",
        Description = "Sincronização de dados EU à meia-noite GMT"
    },
    new CronJobOptions<JobSincronizacaoDados>
    {
        InstanceName = "SincDados-BR",
        CronExpression = "0 0 * * *",
        TimeZone = "America/Sao_Paulo",
        Description = "Sincronização de dados BR à meia-noite BRT"
    }
);
```

Via appsettings.json:

```json
{
  "CronJobs": {
    "JobSincronizacaoDados": {
      "Instances": {
        "SincDados-US": {
          "CronExpression": "0 0 * * *",
          "TimeZone": "America/New_York"
        },
        "SincDados-EU": {
          "CronExpression": "0 0 * * *",
          "TimeZone": "Europe/London"
        },
        "SincDados-BR": {
          "CronExpression": "0 0 * * *",
          "TimeZone": "America/Sao_Paulo"
        }
      }
    }
  }
}
```

```csharp
services.AddCronJobInstancesFromConfiguration<JobSincronizacaoDados>(configuration);
```

### Desabilitar Jobs Sem Alteração de Código

Desabilite um job via configuração sem modificar o código:

```json
{
  "CronJobs": {
    "JobManutencao": {
      "Enabled": false
    }
  }
}
```

Ou via variáveis de ambiente:

```bash
CronJobs__JobManutencao__Enabled=false
```

## ICronJobContext - Contexto de Execução

O `ICronJobContext` fornece metadados sobre a execução atual do job:

```csharp
public interface ICronJobContext
{
    // Identificadores
    Guid JobId { get; }
    string JobName { get; }
    Guid ExecutionId { get; }
    
    // Tempo
    DateTimeOffset StartTime { get; }
    DateTimeOffset? ScheduledTime { get; }
    TimeSpan Elapsed { get; }
    bool IsTimedOut { get; }
    
    // Tentativas
    int CurrentAttempt { get; }
    int MaxAttempts { get; }
    bool IsRetry { get; }
    
    // Metadados
    long ExecutionCount { get; }
    string? CorrelationId { get; }
    Guid? ParentJobId { get; }
    
    // Propriedades customizadas
    IDictionary<string, object?> Properties { get; }
    CancellationToken CancellationToken { get; }
}
```

### Acessando o Contexto

Use `ICronJobContextAccessor` para acessar o contexto atual:

```csharp
public class MeuJobService
{
    private readonly ICronJobContextAccessor _contextAccessor;
    
    public MeuJobService(ICronJobContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }
    
    public async Task ProcessarAsync()
    {
        var context = _contextAccessor.Context;
        
        if (context != null)
        {
            _logger.LogInformation(
                "Executando job {JobName}, tentativa {Attempt} de {MaxAttempts}",
                context.JobName,
                context.CurrentAttempt,
                context.MaxAttempts);
            
            // Adicionar propriedades customizadas
            context.Properties["ProcessedItems"] = 100;
            context.Properties["Status"] = "Em andamento";
            
            // Verificar timeout
            if (context.IsTimedOut)
            {
                throw new OperationCanceledException("Job timeout excedido");
            }
        }
    }
}
```

### Configuração

```csharp
services.AddCronJobAdvancedInfrastructure(); // Registra ICronJobContextAccessor e outros serviços
```

## Expressões CRON de 6 Campos (Segundos)

O módulo suporta expressões CRON de 6 campos para agendamento preciso em segundos:

### Formato

| Campos | Formato |
|--------|---------|
| 5 campos | `minuto hora dia-mês mês dia-semana` |
| 6 campos | `segundo minuto hora dia-mês mês dia-semana` |

### Uso

```csharp
using Mvp24Hours.Infrastructure.CronJob.Scheduling;

// Expressão padrão de 5 campos
var nextRun5 = CronExpressionParser.GetNextOccurrence("*/5 * * * *");

// Expressão de 6 campos (com segundos)
var nextRun6 = CronExpressionParser.GetNextOccurrence("*/30 * * * * *"); // A cada 30 segundos

// Auto-detecção de formato
var format = CronExpressionParser.DetectFormat("*/30 * * * * *"); 
// Retorna: CronExpressionFormat.WithSeconds

// Obter descrição legível
var description = CronExpressionParser.GetDescription("*/30 * * * * *");
// Retorna: "Every 30 seconds"
```

### Exemplos de Expressões de 6 Campos

| Expressão | Descrição |
|-----------|-----------|
| `*/30 * * * * *` | A cada 30 segundos |
| `0 */5 * * * *` | A cada 5 minutos, no segundo 0 |
| `15 30 * * * *` | No segundo 15 de cada minuto 30 |
| `0 0 9 * * 1-5` | Às 9h em dia útil, no segundo 0 |

## Job Dependencies - Dependências Entre Jobs

Execute jobs em ordem, respeitando dependências:

### Configuração

```csharp
// Definir dependências
services.AddCronJobDependency<JobProcessarDados, JobColetarDados>();
services.AddCronJobDependency<JobEnviarRelatorio, JobProcessarDados>();

// Registrar serviços avançados
services.AddCronJobAdvancedInfrastructure();
```

### Fluxo de Execução

```
JobColetarDados → JobProcessarDados → JobEnviarRelatorio
```

### Interface

```csharp
public interface ICronJobDependency
{
    IReadOnlyList<Type> GetDependencies(Type jobType);
    void AddDependency<TJob, TDependsOn>() where TJob : class where TDependsOn : class;
    bool HasPendingDependencies(Type jobType);
    void MarkCompleted(Type jobType);
    void Reset(Type jobType);
}
```

### Uso Programático

```csharp
public class GerenciadorJobs
{
    private readonly ICronJobDependencyTracker _tracker;
    
    public async Task ExecutarPipelineAsync()
    {
        // Verificar se pode executar
        if (_tracker.CanExecute(typeof(JobProcessarDados)))
        {
            await ExecutarJobAsync<JobProcessarDados>();
            _tracker.MarkAsCompleted(typeof(JobProcessarDados));
        }
        
        // Obter próximos jobs prontos
        var ready = _tracker.GetReadyJobs().ToList();
    }
}
```

## Distributed Locking - Evitar Execuções Duplicadas

Evite que o mesmo job execute simultaneamente em múltiplas instâncias (cluster):

### Interface

```csharp
public interface IDistributedCronJobLock
{
    Task<IDistributedCronJobLockHandle?> TryAcquireAsync(
        string jobName, 
        TimeSpan duration, 
        CancellationToken cancellationToken = default);
    
    Task<bool> IsLockedAsync(string jobName, CancellationToken cancellationToken = default);
    
    Task<DistributedLockInfo?> GetLockInfoAsync(
        string jobName, 
        CancellationToken cancellationToken = default);
}
```

### Implementação In-Memory (Single Instance)

```csharp
services.AddSingleton<IDistributedCronJobLock, InMemoryDistributedCronJobLock>();
```

### Implementação Redis (Cluster)

```csharp
// Instale: Mvp24Hours.Infrastructure.CronJob.Redis
services.AddRedisCronJobLock(options =>
{
    options.ConnectionString = "localhost:6379";
    options.KeyPrefix = "cronjob:lock:";
});
```

### Uso no AdvancedCronJobService

O `AdvancedCronJobService` usa automaticamente distributed locking:

```csharp
services.AddAdvancedCronJob<MeuJob>(options =>
{
    options.CronExpression = "*/5 * * * *";
    options.UseDistributedLock = true;
    options.LockTimeout = TimeSpan.FromMinutes(5);
});
```

## ICronJobStateStore - Persistência de Estado

Persista o estado do job entre execuções:

### Interface

```csharp
public interface ICronJobStateStore
{
    Task<CronJobState> GetStateAsync(string jobName, CancellationToken cancellationToken = default);
    Task SaveStateAsync(string jobName, CronJobState state, CancellationToken cancellationToken = default);
    Task<bool> IsPausedAsync(string jobName, CancellationToken cancellationToken = default);
    Task SetPausedAsync(string jobName, bool isPaused, CancellationToken cancellationToken = default);
    Task ResetStateAsync(string jobName, CancellationToken cancellationToken = default);
}
```

### Estado do Job

```csharp
public class CronJobState
{
    public bool IsPaused { get; set; }
    public DateTimeOffset? LastExecutionTime { get; set; }
    public DateTimeOffset? NextExecutionTime { get; set; }
    public DateTimeOffset? LastSuccessTime { get; set; }
    public DateTimeOffset? LastFailureTime { get; set; }
    public int FailureCount { get; set; }
    public int SuccessCount { get; set; }
    public int ConsecutiveFailures { get; set; }
    public TimeSpan? LastExecutionDuration { get; set; }
    public string? LastError { get; set; }
    public IDictionary<string, object?> CustomData { get; set; }
}
```

### Implementações

```csharp
// In-Memory (padrão)
services.AddSingleton<ICronJobStateStore, InMemoryCronJobStateStore>();

// Redis (persistente)
services.AddRedisCronJobStateStore(options =>
{
    options.ConnectionString = "localhost:6379";
    options.KeyPrefix = "cronjob:state:";
});

// SQL Server
services.AddSqlCronJobStateStore(options =>
{
    options.ConnectionString = "...";
    options.TableName = "CronJobStates";
});
```

## Pause/Resume - Controle em Runtime

Pause e retome jobs programaticamente:

### Interface

```csharp
public interface ICronJobController
{
    Task PauseAsync(string jobName, CancellationToken cancellationToken = default);
    Task ResumeAsync(string jobName, CancellationToken cancellationToken = default);
    Task TriggerAsync(string jobName, CancellationToken cancellationToken = default);
    Task<CronJobStatus> GetStatusAsync(string jobName, CancellationToken cancellationToken = default);
    Task<IEnumerable<CronJobStatus>> GetAllStatusesAsync(CancellationToken cancellationToken = default);
    Task PauseAllAsync(CancellationToken cancellationToken = default);
    Task ResumeAllAsync(CancellationToken cancellationToken = default);
}
```

### Uso

```csharp
[ApiController]
[Route("api/cronjobs")]
public class CronJobApiController : ControllerBase
{
    private readonly ICronJobController _controller;
    
    [HttpPost("{jobName}/pause")]
    public async Task<IActionResult> Pause(string jobName)
    {
        await _controller.PauseAsync(jobName);
        return Ok(new { Message = $"Job '{jobName}' pausado." });
    }
    
    [HttpPost("{jobName}/resume")]
    public async Task<IActionResult> Resume(string jobName)
    {
        await _controller.ResumeAsync(jobName);
        return Ok(new { Message = $"Job '{jobName}' retomado." });
    }
    
    [HttpPost("{jobName}/trigger")]
    public async Task<IActionResult> TriggerNow(string jobName)
    {
        await _controller.TriggerAsync(jobName);
        return Ok(new { Message = $"Job '{jobName}' disparado manualmente." });
    }
    
    [HttpGet("{jobName}/status")]
    public async Task<IActionResult> GetStatus(string jobName)
    {
        var status = await _controller.GetStatusAsync(jobName);
        return Ok(status);
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var statuses = await _controller.GetAllStatusesAsync();
        return Ok(statuses);
    }
}
```

## Event Hooks - Ciclo de Vida do Job

Implemente handlers para eventos do ciclo de vida:

### Interfaces de Handler

```csharp
// Antes de iniciar
public interface ICronJobStartingHandler
{
    Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken);
}

// Após completar com sucesso
public interface ICronJobCompletedHandler
{
    Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken);
}

// Após falhar
public interface ICronJobFailedHandler
{
    Task OnJobFailedAsync(ICronJobContext context, Exception exception, CancellationToken cancellationToken);
}

// Quando cancelado
public interface ICronJobCancelledHandler
{
    Task OnJobCancelledAsync(ICronJobContext context, CancellationToken cancellationToken);
}

// Antes de retry
public interface ICronJobRetryHandler
{
    Task OnJobRetryAsync(ICronJobContext context, Exception exception, int attempt, CancellationToken cancellationToken);
}

// Quando pulado (ex: paused, dependência pendente)
public interface ICronJobSkippedHandler
{
    Task OnJobSkippedAsync(ICronJobContext context, string reason, CancellationToken cancellationToken);
}
```

### Implementação

```csharp
public class NotificacaoJobHandler : 
    ICronJobStartingHandler, 
    ICronJobCompletedHandler, 
    ICronJobFailedHandler
{
    private readonly ILogger<NotificacaoJobHandler> _logger;
    private readonly INotificationService _notifications;
    
    public async Task OnJobStartingAsync(ICronJobContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job {JobName} iniciando, execução #{Count}", 
            context.JobName, context.ExecutionCount);
    }
    
    public async Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Job {JobName} concluído em {Duration:c}", 
            context.JobName, duration);
        
        // Notificar se demorou muito
        if (duration > TimeSpan.FromMinutes(5))
        {
            await _notifications.SendAsync(
                $"Job {context.JobName} demorou {duration.TotalMinutes:F1} minutos");
        }
    }
    
    public async Task OnJobFailedAsync(ICronJobContext context, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Job {JobName} falhou na tentativa {Attempt}", 
            context.JobName, context.CurrentAttempt);
        
        // Alerta crítico
        await _notifications.SendAlertAsync(
            $"FALHA: Job {context.JobName} - {exception.Message}");
    }
}
```

### Registro

```csharp
// Registrar handlers individuais
services.AddCronJobEventHandler<ICronJobStartingHandler, NotificacaoJobHandler>();
services.AddCronJobEventHandler<ICronJobCompletedHandler, NotificacaoJobHandler>();
services.AddCronJobEventHandler<ICronJobFailedHandler, NotificacaoJobHandler>();

// Ou usar a extensão que registra todos
services.AddCronJobEventHandlers<NotificacaoJobHandler>();
```

### Classe Base para Handlers

Use `CronJobEventHandlerBase` para implementar apenas os eventos desejados:

```csharp
public class MeuHandler : CronJobEventHandlerBase
{
    public override Task OnJobCompletedAsync(ICronJobContext context, TimeSpan duration, CancellationToken ct)
    {
        // Só precisa implementar este
        return base.OnJobCompletedAsync(context, duration, ct);
    }
}
```

## AdvancedCronJobService - Serviço Completo

O `AdvancedCronJobService<T>` integra todas as funcionalidades avançadas:

```csharp
public class MeuJobAvancado : AdvancedCronJobService<MeuJobAvancado>
{
    public MeuJobAvancado(
        IScheduleConfig<MeuJobAvancado> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ILogger<AdvancedCronJobService<MeuJobAvancado>> logger,
        TimeProvider? timeProvider = null,
        ICronJobStateStore? stateStore = null,
        ICronJobDependency? dependencyTracker = null,
        IDistributedCronJobLock? distributedLock = null,
        CronJobEventDispatcher? eventDispatcher = null,
        ICronJobContextAccessor? contextAccessor = null)
        : base(config, hostApplication, rootServiceProvider, logger, 
               timeProvider, stateStore, dependencyTracker, distributedLock, 
               eventDispatcher, contextAccessor)
    {
    }
    
    public override async Task DoWork(CancellationToken cancellationToken)
    {
        // Seu código aqui - contexto e eventos são gerenciados automaticamente
    }
}
```

### Configuração Completa

```csharp
services
    .AddCronJobAdvancedInfrastructure()    // Context, State, Dependencies, Events
    .AddAdvancedCronJob<MeuJobAvancado>(config =>
    {
        config.CronExpression = "*/5 * * * *";
        config.TimeZoneInfo = TimeZoneInfo.Utc;
    })
    .AddCronJobEventHandlers<NotificacaoJobHandler>();
```

## Veja Também

- [CronJob Básico](cronjob.md) - Documentação principal do módulo
- [CronJob Resiliência](cronjob-resilience.md) - Retry, circuit breaker, prevenção de sobreposição
- [CronJob Observabilidade](cronjob-observability.md) - Health checks, métricas, logging estruturado

