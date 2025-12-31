# Resiliência em CronJob

O módulo CronJob fornece padrões abrangentes de resiliência para execução de jobs em background em nível de produção. Isso inclui políticas de retry, padrão circuit breaker, prevenção de execuções sobrepostas e tratamento de encerramento gracioso.

## Funcionalidades

- **Política de Retry**: Retry configurável com backoff exponencial e jitter
- **Circuit Breaker**: Previne execução repetida de jobs que falham
- **Prevenção de Sobreposição**: Garante que apenas uma execução rode por vez
- **Encerramento Gracioso**: Trata adequadamente o shutdown da aplicação com timeout configurável
- **Timeout de Execução**: Cancela jobs de longa duração após um período configurado
- **Propagação de CancellationToken**: Propaga corretamente o cancelamento para todas as operações aninhadas
- **Integração OpenTelemetry**: Todas as operações de resiliência são instrumentadas para observabilidade

## Instalação

As funcionalidades de resiliência estão incluídas no pacote base:

```bash
dotnet add package Mvp24Hours.Infrastructure.CronJob
```

## Criando um CronJob Resiliente

Herde de `ResilientCronJobService<T>` ao invés de `CronJobService<T>`:

```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Infrastructure.CronJob.Interfaces;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;

public class MeuJobResiliente : ResilientCronJobService<MeuJobResiliente>
{
    public MeuJobResiliente(
        IResilientScheduleConfig<MeuJobResiliente> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider rootServiceProvider,
        ICronJobExecutionLock executionLock,
        CronJobCircuitBreaker circuitBreaker,
        ILogger<ResilientCronJobService<MeuJobResiliente>> logger,
        TimeProvider? timeProvider = null)
        : base(config, hostApplication, rootServiceProvider, executionLock, circuitBreaker, logger, timeProvider)
    {
    }

    public override async Task DoWork(CancellationToken cancellationToken)
    {
        // Sua lógica do job aqui
        // Retries, circuit breaker e prevenção de sobreposição são tratados automaticamente
        
        var service = _serviceProvider!.GetRequiredService<IMeuServico>();
        await service.ProcessarAsync(cancellationToken);
    }
}
```

## Configuração

### Configuração Completa de Resiliência

```csharp
services.AddResilientCronJob<MeuJobResiliente>(config =>
{
    // Configuração de agendamento
    config.CronExpression = "*/5 * * * *"; // A cada 5 minutos
    config.TimeZoneInfo = TimeZoneInfo.Utc;
    
    // Configuração de retry
    config.Resilience.EnableRetry = true;
    config.Resilience.MaxRetryAttempts = 3;
    config.Resilience.RetryDelay = TimeSpan.FromSeconds(1);
    config.Resilience.UseExponentialBackoff = true;
    config.Resilience.MaxRetryDelay = TimeSpan.FromSeconds(30);
    config.Resilience.RetryJitterFactor = 0.2; // 20% de jitter
    
    // Configuração de circuit breaker
    config.Resilience.EnableCircuitBreaker = true;
    config.Resilience.CircuitBreakerFailureThreshold = 5;
    config.Resilience.CircuitBreakerDuration = TimeSpan.FromSeconds(30);
    config.Resilience.CircuitBreakerSuccessThreshold = 1;
    config.Resilience.CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(60);
    
    // Prevenção de sobreposição
    config.Resilience.PreventOverlapping = true;
    config.Resilience.LogOverlappingSkipped = true;
    config.Resilience.OverlappingWaitTimeout = TimeSpan.Zero; // Pular imediatamente
    
    // Encerramento gracioso
    config.Resilience.GracefulShutdownTimeout = TimeSpan.FromSeconds(30);
    config.Resilience.WaitForExecutionOnShutdown = true;
    
    // Timeout de execução
    config.Resilience.ExecutionTimeout = TimeSpan.FromMinutes(5);
    config.Resilience.PropagateCancellation = true;
    
    // Callbacks
    config.Resilience.OnRetry = (ex, tentativa, delay) =>
    {
        Console.WriteLine($"Retry {tentativa}, aguardando {delay.TotalSeconds}s: {ex.Message}");
    };
    
    config.Resilience.OnCircuitBreakerStateChange = (estadoAntigo, estadoNovo) =>
    {
        Console.WriteLine($"Circuit breaker: {estadoAntigo} -> {estadoNovo}");
    };
    
    config.Resilience.OnOverlappingSkipped = () =>
    {
        Console.WriteLine("Execução pulada - anterior ainda em execução");
    };
    
    config.Resilience.OnJobFailed = (ex) =>
    {
        Console.WriteLine($"Job falhou após todos os retries: {ex.Message}");
    };
});
```

### Métodos de Conveniência

```csharp
// Job resiliente simples (apenas prevenção de sobreposição)
services.AddResilientCronJob<MeuJob>("*/5 * * * *");

// Resiliência completa (retry + circuit breaker + sobreposição)
services.AddResilientCronJobWithFullResilience<MeuJob>("*/5 * * * *", TimeZoneInfo.Utc);

// Apenas com retry
services.AddResilientCronJobWithRetry<MeuJob>(
    "0 * * * *",
    maxRetryAttempts: 5,
    useExponentialBackoff: true);

// Apenas com circuit breaker
services.AddResilientCronJobWithCircuitBreaker<MeuJob>(
    "* * * * *",
    failureThreshold: 3,
    breakDuration: TimeSpan.FromMinutes(1));
```

## Política de Retry

A política de retry automaticamente retenta execuções de jobs que falharam com comportamento configurável.

### Opções de Configuração

| Opção | Padrão | Descrição |
|-------|--------|-----------|
| `EnableRetry` | `false` | Habilitar política de retry |
| `MaxRetryAttempts` | `3` | Número máximo de tentativas |
| `RetryDelay` | `1 segundo` | Delay inicial entre retries |
| `UseExponentialBackoff` | `true` | Usar backoff exponencial (1s, 2s, 4s, ...) |
| `MaxRetryDelay` | `30 segundos` | Delay máximo com backoff exponencial |
| `RetryJitterFactor` | `0.2` | Fator de jitter (0-1) para evitar thundering herd |
| `ShouldRetryOnException` | `null` | Predicado para filtrar exceções que devem ser retentadas |

### Backoff Exponencial com Jitter

Quando `UseExponentialBackoff` está habilitado, os delays seguem o padrão:

```
delay = min(delayInicial * 2^(tentativa-1), delayMaximo) ± jitter
```

Exemplo com configurações padrão:
- Tentativa 1: ~1s (800ms - 1.2s com jitter)
- Tentativa 2: ~2s (1.6s - 2.4s com jitter)
- Tentativa 3: ~4s (3.2s - 4.8s com jitter)

### Filtrando Exceções Retentáveis

```csharp
config.Resilience.ShouldRetryOnException = ex =>
{
    // Apenas retentar erros transientes
    return ex is HttpRequestException 
        || ex is TimeoutException
        || ex is SqlException { IsTransient: true };
};
```

## Circuit Breaker

O padrão circuit breaker previne execução repetida de um job que está falhando consistentemente, permitindo que o sistema se recupere.

### Estados

| Estado | Descrição |
|--------|-----------|
| **Closed** | Operação normal, execuções permitidas |
| **Open** | Execuções bloqueadas após atingir limite de falhas |
| **Half-Open** | Execuções de teste permitidas após duração do break |

### Transições de Estado

```
Closed ─── falhas ≥ threshold ──→ Open
  ↑                                 │
  │                                 │ duração do break expirou
  │                                 ↓
  └──── sucesso ≥ threshold ──── Half-Open
                                    │
                                    │ falha
                                    ↓
                                  Open
```

### Opções de Configuração

| Opção | Padrão | Descrição |
|-------|--------|-----------|
| `EnableCircuitBreaker` | `false` | Habilitar circuit breaker |
| `CircuitBreakerFailureThreshold` | `5` | Falhas antes de abrir |
| `CircuitBreakerDuration` | `30 segundos` | Tempo que o circuito fica aberto |
| `CircuitBreakerSuccessThreshold` | `1` | Sucessos necessários para fechar de half-open |
| `CircuitBreakerSamplingDuration` | `60 segundos` | Janela para contagem de falhas |

### Monitorando o Estado do Circuit Breaker

```csharp
public class MeuJob : ResilientCronJobService<MeuJob>
{
    public override async Task DoWork(CancellationToken cancellationToken)
    {
        // Acessar estado atual
        var estado = CircuitBreakerState;
        _logger.LogInformation("Estado atual do circuit breaker: {Estado}", estado);
        
        // Contagem de execuções e puladas disponíveis
        _logger.LogInformation("Execuções: {Count}, Puladas: {Puladas}", 
            ExecutionCount, SkippedCount);
    }
}
```

## Prevenção de Sobreposição

Previne execuções concorrentes do mesmo job, útil para jobs que não devem rodar em paralelo.

### Opções de Configuração

| Opção | Padrão | Descrição |
|-------|--------|-----------|
| `PreventOverlapping` | `true` | Habilitar prevenção de sobreposição |
| `LogOverlappingSkipped` | `true` | Logar quando execução é pulada |
| `OverlappingWaitTimeout` | `TimeSpan.Zero` | Tempo para aguardar lock (0 = pular imediatamente) |

### Comportamento

- **Pular Imediatamente**: Com `OverlappingWaitTimeout = TimeSpan.Zero`, se uma execução anterior ainda está rodando, a nova execução é pulada imediatamente.
- **Aguardar com Timeout**: Configure um timeout para aguardar a execução anterior completar antes de pular.

```csharp
// Aguardar até 10 segundos pelo lock antes de pular
config.Resilience.OverlappingWaitTimeout = TimeSpan.FromSeconds(10);
```

## Encerramento Gracioso

Trata adequadamente o shutdown da aplicação, dando tempo para jobs em execução completarem.

### Opções de Configuração

| Opção | Padrão | Descrição |
|-------|--------|-----------|
| `GracefulShutdownTimeout` | `30 segundos` | Tempo máximo para aguardar conclusão do job |
| `WaitForExecutionOnShutdown` | `true` | Se deve aguardar a execução atual |

### Comportamento

1. Quando o shutdown é solicitado, o job recebe cancelamento
2. O framework aguarda até `GracefulShutdownTimeout` pela conclusão
3. Se o timeout for excedido, o job é cancelado forçadamente
4. Os recursos são descartados apropriadamente

```csharp
public override async Task DoWork(CancellationToken cancellationToken)
{
    // Verificar cancelamento periodicamente para shutdown responsivo
    foreach (var item in items)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ProcessarItemAsync(item, cancellationToken);
    }
}
```

## Timeout de Execução

Cancela automaticamente execuções de jobs que demoram muito.

### Opções de Configuração

| Opção | Padrão | Descrição |
|-------|--------|-----------|
| `ExecutionTimeout` | `null` | Tempo máximo de execução (null = sem timeout) |
| `PropagateCancellation` | `true` | Propagar cancelamento para operações aninhadas |

```csharp
// Cancelar execução após 5 minutos
config.Resilience.ExecutionTimeout = TimeSpan.FromMinutes(5);
```

## Tracing OpenTelemetry

Operações de resiliência são totalmente instrumentadas:

### Tags Adicionais

| Tag | Descrição |
|-----|-----------|
| `cronjob.resilience.retry_enabled` | Se retry está habilitado |
| `cronjob.resilience.retry_attempt` | Tentativa atual de retry |
| `cronjob.resilience.retry_count` | Total de retries em todas as execuções |
| `cronjob.resilience.circuit_breaker_enabled` | Se circuit breaker está habilitado |
| `cronjob.resilience.circuit_breaker_state` | Estado atual do circuit breaker |
| `cronjob.resilience.prevent_overlapping` | Se prevenção de sobreposição está habilitada |
| `cronjob.resilience.execution_skipped` | Se a execução foi pulada |
| `cronjob.resilience.skip_reason` | Motivo de ter pulado |
| `cronjob.resilience.timed_out` | Se a execução teve timeout |

## Implementação de Lock Distribuído

Para deploys multi-instância, implemente `ICronJobExecutionLock`:

```csharp
public class RedisDistributedCronJobLock : ICronJobExecutionLock
{
    private readonly IDistributedLockFactory _lockFactory;
    
    public async Task<ICronJobLockHandle?> TryAcquireAsync(
        string jobName,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var lockKey = $"cronjob:lock:{jobName}";
        var handle = await _lockFactory.TryAcquireAsync(lockKey, timeout);
        
        return handle != null ? new RedisLockHandle(handle, jobName) : null;
    }
    
    // ... implementar outros métodos
}

// Registrar lock customizado
services.AddCronJobResilienceInfrastructure<RedisDistributedCronJobLock>();
```

## Propriedades

| Propriedade | Tipo | Descrição |
|-------------|------|-----------|
| `ExecutionCount` | `long` | Número total de execuções |
| `RetryCount` | `long` | Total de retries em todas as execuções |
| `SkippedCount` | `long` | Execuções puladas (sobreposição/circuit breaker) |
| `CircuitBreakerState` | `CircuitBreakerState` | Estado atual do circuit breaker |

## Boas Práticas

1. **Comece Conservador**: Inicie com poucos retries e aumente baseado no comportamento observado
2. **Use Jitter**: Sempre habilite jitter para evitar problemas de thundering herd
3. **Monitore o Circuit Breaker**: Configure alertas para mudanças de estado do circuit breaker
4. **Respeite o Cancelamento**: Sempre verifique `cancellationToken` em operações longas
5. **Logue Apropriadamente**: Use os callback hooks para implementar logging/alertas customizados
6. **Teste Cenários de Falha**: Escreva testes que simulem falhas para verificar comportamento de resiliência

## Veja Também

- [CronJob Básico](cronjob.md)
- [Observabilidade do CronJob](cronjob-observability.md) - Health checks, métricas, logging estruturado
- [Modernização PeriodicTimer](modernization/periodic-timer.md)
- [Abstração TimeProvider](modernization/time-provider.md)
- [Resiliência Genérica](modernization/generic-resilience.md)
- [Observabilidade](observability/home.md)

