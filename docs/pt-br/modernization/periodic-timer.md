# PeriodicTimer (Timer Moderno) 🕐

> **Substitui:** `System.Timers.Timer` e `System.Threading.Timer`
> 
> **Disponível desde:** .NET 6
> 
> **Status:** ✅ Implementado no Mvp24Hours

## Visão Geral

`PeriodicTimer` é a substituição moderna do .NET para classes de timer legadas. Fornece uma API limpa async/await com suporte adequado a cancelamento, ideal para serviços em background e tarefas agendadas.

### Benefícios Principais

| Funcionalidade | Timers Legados | PeriodicTimer |
|----------------|----------------|---------------|
| Async/Await | ❌ Baseado em callback | ✅ Async nativo |
| Cancelamento | ⚠️ Stop manual | ✅ CancellationToken |
| Sobreposição | ⚠️ Pode sobrepor | ✅ Sem sobreposição |
| Drift do Timer | ⚠️ Possível drift | ✅ Intervalos consistentes |
| Thread Safety | ⚠️ Complexo | ✅ Embutido |

## Início Rápido

### Uso Básico

```csharp
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

while (await timer.WaitForNextTickAsync(stoppingToken))
{
    await DoWorkAsync();
}
```

### Usando PeriodicTimerHelper

O Mvp24Hours fornece métodos auxiliares para padrões comuns:

```csharp
using Mvp24Hours.Core.Infrastructure.Timers;

// Executar periodicamente com tratamento automático de cancelamento
await PeriodicTimerHelper.RunPeriodicAsync(
    TimeSpan.FromSeconds(5),
    async ct =>
    {
        await ProcessWorkAsync(ct);
    },
    stoppingToken);
```

## Métodos Auxiliares

### RunPeriodicAsync

Executa uma ação periodicamente, aguardando cada tick antes da execução:

```csharp
await PeriodicTimerHelper.RunPeriodicAsync(
    TimeSpan.FromMinutes(1),          // Período
    async ct =>                        // Ação
    {
        await RefreshCacheAsync(ct);
    },
    stoppingToken);                    // Cancelamento
```

### RunPeriodicImmediateAsync

Executa imediatamente na inicialização, depois executa periodicamente:

```csharp
// Executar imediatamente, depois a cada 30 segundos
await PeriodicTimerHelper.RunPeriodicImmediateAsync(
    TimeSpan.FromSeconds(30),
    async ct =>
    {
        await SyncDataAsync(ct);
    },
    stoppingToken);
```

### RunPeriodicWithErrorHandlingAsync

Continua a execução mesmo quando ocorrem erros:

```csharp
await PeriodicTimerHelper.RunPeriodicWithErrorHandlingAsync(
    TimeSpan.FromMinutes(5),
    async ct =>
    {
        await ProcessBatchAsync(ct);
    },
    ex =>
    {
        _logger.LogError(ex, "Processamento em lote falhou");
    },
    stoppingToken);
```

## Padrão de Background Service

### Antes (Timer Legado)

```csharp
// ❌ Padrão antigo com System.Timers.Timer
public class LegacyBackgroundService : IHostedService
{
    private System.Timers.Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new System.Timers.Timer(5000);
        _timer.Elapsed += async (s, e) =>
        {
            await DoWorkAsync(); // ⚠️ Comportamento similar a async void
        };
        _timer.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        return Task.CompletedTask;
    }
}
```

### Depois (PeriodicTimer)

```csharp
// ✅ Padrão moderno com PeriodicTimer
public class ModernBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWorkAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Serviço parando graciosamente");
        }
    }
}
```

## Integração com TimeProvider

Para código testável, use `TimeProvider`:

```csharp
public class TestableService
{
    private readonly TimeProvider _timeProvider;

    public TestableService(TimeProvider? timeProvider = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        // Obtém o tempo atual através da abstração
        var now = _timeProvider.GetUtcNow();
        
        // Cria timer através do TimeProvider
        using var timer = _timeProvider.CreateTimer(
            callback: _ => { },
            state: null,
            dueTime: TimeSpan.FromSeconds(5),
            period: TimeSpan.FromSeconds(5));
    }
}
```

### Testando com FakeTimeProvider

```csharp
using Microsoft.Extensions.Time.Testing;

[Fact]
public async Task Service_DeveProcessarNoAgendamento()
{
    // Arrange
    var fakeTime = new FakeTimeProvider();
    var service = new TestableService(fakeTime);

    // Act - Avança o tempo
    fakeTime.Advance(TimeSpan.FromSeconds(5));

    // Assert
    // Verificar comportamento esperado
}
```

## Serviços Migrados no Mvp24Hours

Os seguintes serviços foram atualizados para usar PeriodicTimer:

| Serviço | Módulo | Descrição |
|---------|--------|-----------|
| `CronJobService<T>` | CronJob | Tarefas agendadas baseadas em CRON |
| `OutboxProcessor` | CQRS | Publicação de eventos de integração |
| `OutboxCleanupService` | CQRS | Limpeza de mensagens do outbox |
| `InboxCleanupService` | CQRS | Limpeza de mensagens do inbox |
| `ScheduledCommandHostedService` | CQRS | Processamento de comandos agendados |
| `WriteBehindBackgroundService` | Caching | Flush de cache write-behind |
| `ScheduledMessageBackgroundService` | RabbitMQ | Processamento de mensagens agendadas |

## Métodos de Extensão

### WaitForNextTickAsync com Timeout

```csharp
using Mvp24Hours.Core.Infrastructure.Timers;

using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

// Aguardar com timeout
var tickOcorreu = await timer.WaitForNextTickAsync(
    timeout: TimeSpan.FromSeconds(5),
    cancellationToken: stoppingToken);

if (!tickOcorreu)
{
    // Timeout ocorreu antes do tick
}
```

## Boas Práticas

### 1. Sempre Use a Instrução `using`

```csharp
// ✅ Correto - Timer é descartado adequadamente
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

// ❌ Errado - Vazamento de timer
var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
```

### 2. Trate o Cancelamento Adequadamente

```csharp
try
{
    while (await timer.WaitForNextTickAsync(stoppingToken))
    {
        await DoWorkAsync(stoppingToken);
    }
}
catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
{
    // Shutdown gracioso - não lance exceção
    _logger.LogInformation("Desligando...");
}
```

### 3. Execute Imediatamente Quando Necessário

```csharp
// Processa imediatamente, depois periodicamente
await ProcessAsync(stoppingToken);

using var timer = new PeriodicTimer(interval);
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    await ProcessAsync(stoppingToken);
}
```

### 4. Use Intervalos Menores para Melhor Responsividade

```csharp
// Para delays longos, divida em intervalos menores
const int MaxIntervalMs = 60_000;

while (!cancellationToken.IsCancellationRequested)
{
    var remaining = targetTime - DateTimeOffset.UtcNow;
    
    if (remaining <= TimeSpan.Zero)
        break;

    var waitTime = remaining > TimeSpan.FromMilliseconds(MaxIntervalMs)
        ? TimeSpan.FromMilliseconds(MaxIntervalMs)
        : remaining;

    using var timer = new PeriodicTimer(waitTime);
    await timer.WaitForNextTickAsync(cancellationToken);
}
```

## Considerações de Performance

- PeriodicTimer é mais eficiente que Task.Delay para esperas repetidas
- Nenhuma thread do thread pool é bloqueada enquanto aguarda
- Descarte adequado libera recursos internos imediatamente
- Considere processamento em lote para reduzir overhead

## Veja Também

- [Abstração TimeProvider](time-provider.md)
- [Funcionalidades do .NET 9](dotnet9-features.md)
- [Documentação Microsoft](https://learn.microsoft.com/pt-br/dotnet/api/system.threading.periodictimer)

