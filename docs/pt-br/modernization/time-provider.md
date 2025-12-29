# Abstração TimeProvider (.NET 8+)

O `TimeProvider` é a abstração padrão do .NET 8+ para operações de tempo, substituindo o uso direto de `DateTime.Now` e `DateTime.UtcNow`. Este guia explica como usar TimeProvider no Mvp24Hours e migrar da interface legada `IClock`.

## Por Que Usar TimeProvider?

```csharp
// ❌ Difícil de testar - depende do tempo real
public bool Expirou(DateTime dataExpiracao)
{
    return DateTime.UtcNow > dataExpiracao;
}

// ✅ Testável - usa abstração TimeProvider
public bool Expirou(DateTime dataExpiracao, TimeProvider timeProvider)
{
    return timeProvider.GetUtcNow() > dataExpiracao;
}
```

### Benefícios

- **API padrão do .NET**: Funciona com todas as bibliotecas .NET 8+
- **Suporte a testes nativo**: `FakeTimeProvider` do Microsoft.Extensions.TimeProvider.Testing
- **À prova de futuro**: Padrão oficial da Microsoft para abstração de tempo
- **Sem código customizado para manter**: Usa implementação nativa do .NET

## Instalação

```bash
# Para produção (já incluído no .NET 8+)
# Nenhum pacote adicional necessário

# Para testes
dotnet add package Microsoft.Extensions.TimeProvider.Testing
```

## Início Rápido

### Registrar TimeProvider no DI

```csharp
// Em Program.cs - registra TimeProvider.System para produção
builder.Services.AddTimeProvider();

// Tanto TimeProvider quanto IClock agora estão disponíveis
public class PedidoService
{
    private readonly TimeProvider _timeProvider;
    private readonly IClock _clock; // Suporte legado
    
    public PedidoService(TimeProvider timeProvider, IClock clock)
    {
        _timeProvider = timeProvider;
        _clock = clock;
    }
    
    public Pedido CriarPedido(Carrinho carrinho)
    {
        return new Pedido
        {
            CriadoEm = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiraEm = _timeProvider.GetUtcNow().AddDays(30).UtcDateTime
        };
    }
}
```

### Testando com FakeTimeProvider

```csharp
using Microsoft.Extensions.Time.Testing;

[Fact]
public async Task Pedido_Expira_Apos_30_Dias()
{
    // Arrange
    var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    var services = new ServiceCollection();
    services.ReplaceTimeProvider(fakeTime);
    
    var provider = services.BuildServiceProvider();
    var pedidoService = provider.GetRequiredService<PedidoService>();
    
    // Act
    var pedido = pedidoService.CriarPedido(carrinho);
    
    // Assert - pedido não deve estar expirado inicialmente
    Assert.False(pedidoService.EstaExpirado(pedido));
    
    // Avança o tempo em 31 dias
    fakeTime.Advance(TimeSpan.FromDays(31));
    
    // Assert - pedido agora deve estar expirado
    Assert.True(pedidoService.EstaExpirado(pedido));
}
```

## Adaptadores: TimeProvider ↔ IClock

Mvp24Hours fornece adaptadores bidirecionais para migração gradual:

### TimeProviderAdapter (TimeProvider → IClock)

```csharp
// Envolve TimeProvider como IClock para código legado
var timeProvider = TimeProvider.System;
IClock clock = new TimeProviderAdapter(timeProvider);

// Ou com timezone customizado
var brasiliaZone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
IClock clock = new TimeProviderAdapter(timeProvider, brasiliaZone);
```

### ClockAdapter (IClock → TimeProvider)

```csharp
// Envolve IClock existente como TimeProvider para código novo
IClock testClock = new TestClock(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
TimeProvider timeProvider = new ClockAdapter(testClock);
```

## Métodos de Registro no DI

### AddTimeProvider()

Registra `TimeProvider.System` e faz ponte para `IClock`:

```csharp
services.AddTimeProvider();
// Equivalente a:
// services.AddSingleton(TimeProvider.System);
// services.AddSingleton<IClock, TimeProviderAdapter>();
```

### AddTimeProvider(TimeProvider)

Registra um TimeProvider customizado (ex: para testes):

```csharp
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
services.AddTimeProvider(fakeTime);
```

### AddClock(IClock)

Registra um IClock existente e faz ponte para TimeProvider:

```csharp
var testClock = new TestClock(DateTime.UtcNow);
services.AddClock(testClock);
```

### AddSystemClock()

Método de conveniência para registrar SystemClock:

```csharp
services.AddSystemClock();
// Equivalente a:
// services.AddClock(SystemClock.Instance);
```

### ReplaceTimeProvider() / ReplaceClock()

Substitui registros existentes (útil em testes):

```csharp
// No setup do teste
services.AddTimeProvider(); // Registro normal
services.ReplaceTimeProvider(fakeTimeProvider); // Substitui para testes
```

## Integração com CronJobService

`CronJobService` agora aceita um parâmetro opcional `TimeProvider`:

```csharp
public class MeuCronJob : CronJobService<MeuCronJob>
{
    public MeuCronJob(
        IScheduleConfig<MeuCronJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider serviceProvider,
        ILogger<MeuCronJob> logger,
        TimeProvider? timeProvider = null) // Opcional - padrão TimeProvider.System
        : base(config, hostApplication, serviceProvider, logger, timeProvider)
    {
    }
    
    public override async Task DoWork(CancellationToken cancellationToken)
    {
        // Seu trabalho agendado aqui
    }
}
```

### Testando CronJobs

```csharp
[Fact]
public async Task CronJob_Executa_No_Horario_Agendado()
{
    var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    
    // Cria CronJob com tempo falso
    var cronJob = new MeuCronJob(config, hostApp, serviceProvider, logger, fakeTime);
    
    // Avança tempo para disparar execução
    fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 1, 1, 0, 0, TimeSpan.Zero));
    
    // Verifica execução
}
```

## Integração com ScheduledCommandHostedService

O processador de comandos agendados também suporta `TimeProvider`:

```csharp
public class ScheduledCommandHostedService : BackgroundService
{
    public ScheduledCommandHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledCommandHostedService> logger,
        ScheduledCommandOptions? options = null,
        TimeProvider? timeProvider = null) // Opcional
    { }
}
```

## Guia de Migração

### De IClock para TimeProvider

**Passo 1: Atualizar Registro no DI**
```csharp
// Antes
services.AddSingleton<IClock, SystemClock>();

// Depois
services.AddTimeProvider(); // Registra tanto TimeProvider quanto IClock
```

**Passo 2: Atualizar Injeção no Construtor**
```csharp
// Antes
public MeuServico(IClock clock)
{
    _utcNow = clock.UtcNow;
}

// Depois (recomendado)
public MeuServico(TimeProvider timeProvider)
{
    _utcNow = timeProvider.GetUtcNow().UtcDateTime;
}

// Ou continuar usando IClock (ainda funciona)
public MeuServico(IClock clock)
{
    _utcNow = clock.UtcNow;
}
```

**Passo 3: Atualizar Testes**
```csharp
// Antes
var testClock = new TestClock(DateTime.UtcNow);
services.AddSingleton<IClock>(testClock);

// Depois (recomendado)
var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
services.ReplaceTimeProvider(fakeTime);
fakeTime.Advance(TimeSpan.FromHours(1)); // Manipulação de tempo nativa
```

## Boas Práticas

1. **Prefira TimeProvider para código novo** - É o padrão do .NET
2. **Use FakeTimeProvider para testes** - Mais funcionalidades que TestClock
3. **Mantenha IClock para compatibilidade** - Migração gradual é ok
4. **Injete via DI** - Não use TimeProvider.System diretamente na lógica de negócio
5. **Use DateTimeOffset** - TimeProvider retorna DateTimeOffset, não DateTime

## Comparação: IClock vs TimeProvider

| Recurso | IClock (Legado) | TimeProvider (.NET 8+) |
|---------|-----------------|------------------------|
| Origem | Mvp24Hours customizado | Microsoft.Extensions.* |
| Tipo de retorno | DateTime | DateTimeOffset |
| Provider de teste | TestClock/MockClock | FakeTimeProvider |
| Criação de timer | Não suportado | CreateTimer() |
| Stopwatch | Não suportado | GetTimestamp() |
| Mantenedor | Comunidade | Microsoft |

## Documentação Relacionada

- [Documentação IClock](../core/infrastructure-abstractions.md)
- [Documentação CronJob](../cronjob.md)
- [Comandos Agendados](../cqrs/scheduled-commands.md)
- [Documentação Microsoft TimeProvider](https://learn.microsoft.com/pt-br/dotnet/api/system.timeprovider)

