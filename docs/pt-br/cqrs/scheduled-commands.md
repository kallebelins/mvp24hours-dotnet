# Scheduled Commands (Comandos Agendados)

## Visão Geral

O Mvp24Hours CQRS oferece suporte a comandos agendados, permitindo que você agende a execução de commands para um momento futuro. O sistema fornece:

1. **Agendamento Flexível** - Execute comandos em horários específicos ou após intervalos
2. **Retry Automático** - Reexecução automática em caso de falhas
3. **Persistência** - Estado dos comandos persiste entre reinicializações
4. **Prioridade** - Controle a ordem de execução
5. **Cancelamento** - Cancele comandos antes da execução

## Arquitetura

```
┌────────────────────────────────────────────────────────────────────────────┐
│                              Aplicação                                      │
│                                                                             │
│   ICommandScheduler.ScheduleAsync(command, options)                         │
└────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                        IScheduledCommandStore                               │
│   ┌────────────────────────────────────────────────────────────────────┐   │
│   │  ScheduledCommandEntry                                              │   │
│   │  - Id, CommandType, Payload, ScheduledFor                           │   │
│   │  - Status (Pending, Processing, Completed, Failed)                  │   │
│   │  - RetryCount, Priority, ExpiresAt                                  │   │
│   └────────────────────────────────────────────────────────────────────┘   │
└────────────────────────────────────────────────────────────────────────────┘
                                   │
                                   ▼
┌────────────────────────────────────────────────────────────────────────────┐
│                    ScheduledCommandHostedService                            │
│                                                                             │
│   • Executa em background (polling interval configurável)                   │
│   • Busca comandos prontos para execução                                    │
│   • Executa via IMediator.SendAsync()                                       │
│   • Gerencia retry em caso de falha                                         │
│   • Move para DLQ se exceder tentativas                                     │
└────────────────────────────────────────────────────────────────────────────┘
```

## Interfaces Principais

### IScheduledCommand

Marcador para comandos que podem ser agendados:

```csharp
public interface IScheduledCommand : IMediatorCommand
{
}

public interface IScheduledCommand<TResponse> : IMediatorCommand<TResponse>
{
}
```

### ICommandScheduler

Interface para agendar comandos:

```csharp
public interface ICommandScheduler
{
    Task<string> ScheduleAsync<TCommand>(
        TCommand command,
        ScheduleOptions options,
        CancellationToken cancellationToken = default)
        where TCommand : class;

    Task<string> ScheduleAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class;

    Task<bool> CancelAsync(string commandId, CancellationToken cancellationToken = default);

    Task<ScheduledCommandEntry?> GetStatusAsync(string commandId, CancellationToken cancellationToken = default);
}
```

### ScheduleOptions

Opções de agendamento:

```csharp
public class ScheduleOptions
{
    public DateTime? ScheduledFor { get; set; }
    public TimeSpan? Delay { get; set; }
    public int Priority { get; set; } = 0;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan? ExpiresAfter { get; set; }
    public string? CorrelationId { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
```

## Configuração

### Registro Básico

```csharp
// Usando store em memória (desenvolvimento/testes)
services.AddMvpScheduledCommands();

// Com configuração
services.AddMvpScheduledCommands(options =>
{
    options.Enabled = true;
    options.PollingInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 50;
    options.MaxRetries = 3;
    options.RetryDelayBase = TimeSpan.FromSeconds(10);
    options.EnableExponentialBackoff = true;
});
```

### Store Customizado

```csharp
// Implementar store persistente
public class SqlScheduledCommandStore : IScheduledCommandStore
{
    private readonly AppDbContext _dbContext;

    public async Task SaveAsync(ScheduledCommandEntry entry, CancellationToken cancellationToken)
    {
        _dbContext.ScheduledCommands.Add(entry);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduledCommandEntry>> GetReadyForExecutionAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        return await _dbContext.ScheduledCommands
            .Where(e => e.Status == ScheduledCommandStatus.Pending)
            .Where(e => e.ScheduledFor <= DateTime.UtcNow)
            .Where(e => e.ExpiresAt == null || e.ExpiresAt > DateTime.UtcNow)
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.ScheduledFor)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    // ... outros métodos
}

// Registro
services.AddMvpScheduledCommands<SqlScheduledCommandStore>(options =>
{
    options.PollingInterval = TimeSpan.FromSeconds(10);
});
```

## Uso

### Definindo um Scheduled Command

```csharp
public record SendEmailCommand : IScheduledCommand
{
    public string To { get; init; }
    public string Subject { get; init; }
    public string Body { get; init; }
}

public class SendEmailHandler : IMediatorCommandHandler<SendEmailCommand>
{
    private readonly IEmailService _emailService;

    public SendEmailHandler(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task<Unit> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        await _emailService.SendAsync(request.To, request.Subject, request.Body);
        return Unit.Value;
    }
}
```

### Agendando para Execução Imediata

```csharp
public class OrderService
{
    private readonly ICommandScheduler _scheduler;

    public async Task<string> CreateOrderAsync(Order order)
    {
        // ... criar pedido

        // Agendar envio de email (executa assim que possível)
        var commandId = await _scheduler.ScheduleAsync(new SendEmailCommand
        {
            To = order.CustomerEmail,
            Subject = "Pedido Confirmado",
            Body = $"Seu pedido #{order.Id} foi confirmado."
        });

        return commandId;
    }
}
```

### Agendando para Horário Específico

```csharp
// Enviar email às 9h da manhã
var tomorrow9am = DateTime.Today.AddDays(1).AddHours(9);

var commandId = await _scheduler.ScheduleAsync(
    new SendEmailCommand
    {
        To = "customer@example.com",
        Subject = "Lembrete",
        Body = "Não esqueça do seu compromisso!"
    },
    new ScheduleOptions
    {
        ScheduledFor = tomorrow9am
    });
```

### Agendando com Delay

```csharp
// Enviar email após 1 hora
var commandId = await _scheduler.ScheduleAsync(
    new SendEmailCommand
    {
        To = "customer@example.com",
        Subject = "Como foi sua experiência?",
        Body = "Avalie sua compra!"
    },
    new ScheduleOptions
    {
        Delay = TimeSpan.FromHours(1)
    });
```

### Configurando Prioridade

```csharp
// Comando de alta prioridade (executado primeiro)
await _scheduler.ScheduleAsync(
    new ProcessPaymentCommand { OrderId = orderId },
    new ScheduleOptions { Priority = 10 }); // Maior = mais prioritário

// Comando de baixa prioridade
await _scheduler.ScheduleAsync(
    new SendMarketingEmailCommand(),
    new ScheduleOptions { Priority = 1 });
```

### Configurando Retry

```csharp
await _scheduler.ScheduleAsync(
    new CallExternalApiCommand(),
    new ScheduleOptions
    {
        MaxRetries = 5,
        // Com exponential backoff: 10s, 20s, 40s, 80s, 160s
    });
```

### Expiração de Comandos

```csharp
// Expira em 24 horas se não executado
await _scheduler.ScheduleAsync(
    new SendPromoCodeCommand { Code = "PROMO50" },
    new ScheduleOptions
    {
        ExpiresAfter = TimeSpan.FromHours(24)
    });
```

### Cancelamento

```csharp
// Agendar
var commandId = await _scheduler.ScheduleAsync(
    new SendReminderCommand(),
    new ScheduleOptions { Delay = TimeSpan.FromDays(7) });

// Cancelar antes da execução
var cancelled = await _scheduler.CancelAsync(commandId);
if (cancelled)
{
    Console.WriteLine("Comando cancelado com sucesso!");
}
```

### Verificando Status

```csharp
var entry = await _scheduler.GetStatusAsync(commandId);

switch (entry?.Status)
{
    case ScheduledCommandStatus.Pending:
        Console.WriteLine($"Aguardando execução em {entry.ScheduledFor}");
        break;
    case ScheduledCommandStatus.Processing:
        Console.WriteLine("Em processamento...");
        break;
    case ScheduledCommandStatus.Completed:
        Console.WriteLine($"Concluído em {entry.CompletedAt}");
        break;
    case ScheduledCommandStatus.Failed:
        Console.WriteLine($"Falhou após {entry.RetryCount} tentativas: {entry.ErrorMessage}");
        break;
    case ScheduledCommandStatus.Cancelled:
        Console.WriteLine("Cancelado");
        break;
    case ScheduledCommandStatus.Expired:
        Console.WriteLine("Expirou antes de ser executado");
        break;
}
```

## Command com Resposta

```csharp
public record ProcessReportCommand : IScheduledCommand<ReportResult>
{
    public string ReportType { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}

public class ProcessReportHandler : IMediatorCommandHandler<ProcessReportCommand, ReportResult>
{
    public async Task<ReportResult> Handle(
        ProcessReportCommand request, 
        CancellationToken cancellationToken)
    {
        // Processar relatório pesado
        var data = await GenerateReportAsync(request);
        return new ReportResult { Data = data, GeneratedAt = DateTime.UtcNow };
    }
}

// O resultado é armazenado no entry
var entry = await _scheduler.GetStatusAsync(commandId);
if (entry?.Status == ScheduledCommandStatus.Completed)
{
    var result = JsonSerializer.Deserialize<ReportResult>(entry.ResultPayload);
}
```

## Eventos de Ciclo de Vida

```csharp
public class ScheduledCommandNotificationHandler : 
    IMediatorNotificationHandler<ScheduledCommandCompletedNotification>,
    IMediatorNotificationHandler<ScheduledCommandFailedNotification>
{
    private readonly ILogger<ScheduledCommandNotificationHandler> _logger;

    public Task Handle(
        ScheduledCommandCompletedNotification notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Command {CommandId} ({CommandType}) completed successfully",
            notification.CommandId,
            notification.CommandType);
        return Task.CompletedTask;
    }

    public Task Handle(
        ScheduledCommandFailedNotification notification, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(
            "Command {CommandId} ({CommandType}) failed: {Error}",
            notification.CommandId,
            notification.CommandType,
            notification.Error);
        return Task.CompletedTask;
    }
}
```

## Boas Práticas

### 1. Use para Operações Demoradas

```csharp
// ✅ Bom uso - operação demorada
await _scheduler.ScheduleAsync(new GeneratePdfReportCommand { ... });

// ❌ Evite - operação rápida e síncrona
await _scheduler.ScheduleAsync(new UpdateCounterCommand { }); 
```

### 2. Garanta Idempotência

```csharp
public record SendEmailCommand : IScheduledCommand, IIdempotentCommand
{
    public string IdempotencyKey => $"email:{To}:{Subject}:{GetHashCode()}";
    
    public string To { get; init; }
    public string Subject { get; init; }
}
```

### 3. Use Metadata para Rastreamento

```csharp
await _scheduler.ScheduleAsync(
    new ProcessOrderCommand { OrderId = orderId },
    new ScheduleOptions
    {
        CorrelationId = currentCorrelationId,
        Metadata = new Dictionary<string, string>
        {
            ["UserId"] = userId,
            ["Source"] = "WebHook"
        }
    });
```

### 4. Configure Expiração Apropriada

```csharp
// Promoção que expira
await _scheduler.ScheduleAsync(
    new ApplyDiscountCommand { CustomerId = id },
    new ScheduleOptions
    {
        ScheduledFor = blackFridayStart,
        ExpiresAfter = blackFridayEnd - blackFridayStart
    });
```

### 5. Monitore o Store

```csharp
// Endpoint para monitoramento
app.MapGet("/admin/scheduled-commands", async (IScheduledCommandStore store) =>
{
    var pending = await store.GetPendingCountAsync();
    var failed = await store.GetFailedCountAsync();
    var processing = await store.GetProcessingCountAsync();
    
    return new { pending, failed, processing };
});
```

## Exemplo Completo

```csharp
// Program.cs
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
});

services.AddMvpScheduledCommands(options =>
{
    options.PollingInterval = TimeSpan.FromSeconds(10);
    options.MaxRetries = 3;
    options.EnableExponentialBackoff = true;
});

// Command
public record SendWelcomeEmailCommand : IScheduledCommand
{
    public string UserId { get; init; }
    public string Email { get; init; }
}

// Handler
public class SendWelcomeEmailHandler : IMediatorCommandHandler<SendWelcomeEmailCommand>
{
    private readonly IEmailService _email;
    private readonly IUserRepository _users;

    public async Task<Unit> Handle(
        SendWelcomeEmailCommand request, 
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId);
        await _email.SendWelcomeAsync(user);
        return Unit.Value;
    }
}

// Service
public class UserRegistrationService
{
    private readonly ICommandScheduler _scheduler;
    private readonly IMediator _mediator;

    public async Task RegisterUserAsync(RegisterUserCommand command)
    {
        // Criar usuário imediatamente
        var user = await _mediator.SendAsync(command);

        // Agendar email de boas-vindas para 5 minutos depois
        await _scheduler.ScheduleAsync(
            new SendWelcomeEmailCommand
            {
                UserId = user.Id,
                Email = user.Email
            },
            new ScheduleOptions
            {
                Delay = TimeSpan.FromMinutes(5),
                MaxRetries = 3
            });

        // Agendar email de follow-up para 7 dias depois
        await _scheduler.ScheduleAsync(
            new SendFollowUpEmailCommand { UserId = user.Id },
            new ScheduleOptions
            {
                Delay = TimeSpan.FromDays(7),
                ExpiresAfter = TimeSpan.FromDays(14)
            });
    }
}
```


