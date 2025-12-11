# Scheduled Commands

## Overview

Mvp24Hours CQRS provides support for scheduled commands, allowing you to schedule command execution for a future time. The system provides:

1. **Flexible Scheduling** - Execute commands at specific times or after intervals
2. **Automatic Retry** - Automatic re-execution on failures
3. **Persistence** - Command state persists across restarts
4. **Priority** - Control execution order
5. **Cancellation** - Cancel commands before execution

## Architecture

```
┌────────────────────────────────────────────────────────────────────────────┐
│                              Application                                    │
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
│   • Runs in background (configurable polling interval)                      │
│   • Fetches commands ready for execution                                    │
│   • Executes via IMediator.SendAsync()                                      │
│   • Manages retry on failure                                                │
│   • Moves to DLQ if retries exceeded                                        │
└────────────────────────────────────────────────────────────────────────────┘
```

## Main Interfaces

### IScheduledCommand

Marker for commands that can be scheduled:

```csharp
public interface IScheduledCommand : IMediatorCommand
{
}

public interface IScheduledCommand<TResponse> : IMediatorCommand<TResponse>
{
}
```

### ICommandScheduler

Interface for scheduling commands:

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

Scheduling options:

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

## Configuration

### Basic Registration

```csharp
// Using in-memory store (development/testing)
services.AddMvpScheduledCommands();

// With configuration
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

### Custom Store

```csharp
// Implement persistent store
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

    // ... other methods
}

// Registration
services.AddMvpScheduledCommands<SqlScheduledCommandStore>(options =>
{
    options.PollingInterval = TimeSpan.FromSeconds(10);
});
```

## Usage

### Defining a Scheduled Command

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

### Scheduling for Immediate Execution

```csharp
public class OrderService
{
    private readonly ICommandScheduler _scheduler;

    public async Task<string> CreateOrderAsync(Order order)
    {
        // ... create order

        // Schedule email sending (executes as soon as possible)
        var commandId = await _scheduler.ScheduleAsync(new SendEmailCommand
        {
            To = order.CustomerEmail,
            Subject = "Order Confirmed",
            Body = $"Your order #{order.Id} has been confirmed."
        });

        return commandId;
    }
}
```

### Scheduling for Specific Time

```csharp
// Send email at 9am tomorrow
var tomorrow9am = DateTime.Today.AddDays(1).AddHours(9);

var commandId = await _scheduler.ScheduleAsync(
    new SendEmailCommand
    {
        To = "customer@example.com",
        Subject = "Reminder",
        Body = "Don't forget your appointment!"
    },
    new ScheduleOptions
    {
        ScheduledFor = tomorrow9am
    });
```

### Scheduling with Delay

```csharp
// Send email after 1 hour
var commandId = await _scheduler.ScheduleAsync(
    new SendEmailCommand
    {
        To = "customer@example.com",
        Subject = "How was your experience?",
        Body = "Rate your purchase!"
    },
    new ScheduleOptions
    {
        Delay = TimeSpan.FromHours(1)
    });
```

### Configuring Priority

```csharp
// High priority command (executed first)
await _scheduler.ScheduleAsync(
    new ProcessPaymentCommand { OrderId = orderId },
    new ScheduleOptions { Priority = 10 }); // Higher = more priority

// Low priority command
await _scheduler.ScheduleAsync(
    new SendMarketingEmailCommand(),
    new ScheduleOptions { Priority = 1 });
```

### Configuring Retry

```csharp
await _scheduler.ScheduleAsync(
    new CallExternalApiCommand(),
    new ScheduleOptions
    {
        MaxRetries = 5,
        // With exponential backoff: 10s, 20s, 40s, 80s, 160s
    });
```

### Command Expiration

```csharp
// Expires in 24 hours if not executed
await _scheduler.ScheduleAsync(
    new SendPromoCodeCommand { Code = "PROMO50" },
    new ScheduleOptions
    {
        ExpiresAfter = TimeSpan.FromHours(24)
    });
```

### Cancellation

```csharp
// Schedule
var commandId = await _scheduler.ScheduleAsync(
    new SendReminderCommand(),
    new ScheduleOptions { Delay = TimeSpan.FromDays(7) });

// Cancel before execution
var cancelled = await _scheduler.CancelAsync(commandId);
if (cancelled)
{
    Console.WriteLine("Command cancelled successfully!");
}
```

### Checking Status

```csharp
var entry = await _scheduler.GetStatusAsync(commandId);

switch (entry?.Status)
{
    case ScheduledCommandStatus.Pending:
        Console.WriteLine($"Waiting for execution at {entry.ScheduledFor}");
        break;
    case ScheduledCommandStatus.Processing:
        Console.WriteLine("Processing...");
        break;
    case ScheduledCommandStatus.Completed:
        Console.WriteLine($"Completed at {entry.CompletedAt}");
        break;
    case ScheduledCommandStatus.Failed:
        Console.WriteLine($"Failed after {entry.RetryCount} attempts: {entry.ErrorMessage}");
        break;
    case ScheduledCommandStatus.Cancelled:
        Console.WriteLine("Cancelled");
        break;
    case ScheduledCommandStatus.Expired:
        Console.WriteLine("Expired before execution");
        break;
}
```

## Command with Response

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
        // Process heavy report
        var data = await GenerateReportAsync(request);
        return new ReportResult { Data = data, GeneratedAt = DateTime.UtcNow };
    }
}

// Result is stored in the entry
var entry = await _scheduler.GetStatusAsync(commandId);
if (entry?.Status == ScheduledCommandStatus.Completed)
{
    var result = JsonSerializer.Deserialize<ReportResult>(entry.ResultPayload);
}
```

## Lifecycle Events

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

## Best Practices

### 1. Use for Long-Running Operations

```csharp
// ✅ Good use - long-running operation
await _scheduler.ScheduleAsync(new GeneratePdfReportCommand { ... });

// ❌ Avoid - quick synchronous operation
await _scheduler.ScheduleAsync(new UpdateCounterCommand { }); 
```

### 2. Ensure Idempotency

```csharp
public record SendEmailCommand : IScheduledCommand, IIdempotentCommand
{
    public string IdempotencyKey => $"email:{To}:{Subject}:{GetHashCode()}";
    
    public string To { get; init; }
    public string Subject { get; init; }
}
```

### 3. Use Metadata for Tracking

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

### 4. Configure Appropriate Expiration

```csharp
// Promotion that expires
await _scheduler.ScheduleAsync(
    new ApplyDiscountCommand { CustomerId = id },
    new ScheduleOptions
    {
        ScheduledFor = blackFridayStart,
        ExpiresAfter = blackFridayEnd - blackFridayStart
    });
```

### 5. Monitor the Store

```csharp
// Endpoint for monitoring
app.MapGet("/admin/scheduled-commands", async (IScheduledCommandStore store) =>
{
    var pending = await store.GetPendingCountAsync();
    var failed = await store.GetFailedCountAsync();
    var processing = await store.GetProcessingCountAsync();
    
    return new { pending, failed, processing };
});
```

## Complete Example

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
        // Create user immediately
        var user = await _mediator.SendAsync(command);

        // Schedule welcome email for 5 minutes later
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

        // Schedule follow-up email for 7 days later
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


