# PeriodicTimer (Modern Timer) 🕐

> **Replaces:** `System.Timers.Timer` and `System.Threading.Timer`
> 
> **Available since:** .NET 6
> 
> **Status:** ✅ Implemented in Mvp24Hours

## Overview

`PeriodicTimer` is the modern .NET replacement for legacy timer classes. It provides a clean async/await API with proper cancellation support, making it ideal for background services and scheduled tasks.

### Key Benefits

| Feature | Legacy Timers | PeriodicTimer |
|---------|--------------|---------------|
| Async/Await | ❌ Callback-based | ✅ Native async |
| Cancellation | ⚠️ Manual stop | ✅ CancellationToken |
| Overlapping | ⚠️ Can overlap | ✅ No overlap |
| Timer Drift | ⚠️ Possible drift | ✅ Consistent intervals |
| Thread Safety | ⚠️ Complex | ✅ Built-in |

## Quick Start

### Basic Usage

```csharp
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

while (await timer.WaitForNextTickAsync(stoppingToken))
{
    await DoWorkAsync();
}
```

### Using PeriodicTimerHelper

Mvp24Hours provides helper methods for common patterns:

```csharp
using Mvp24Hours.Core.Infrastructure.Timers;

// Run periodically with automatic cancellation handling
await PeriodicTimerHelper.RunPeriodicAsync(
    TimeSpan.FromSeconds(5),
    async ct =>
    {
        await ProcessWorkAsync(ct);
    },
    stoppingToken);
```

## Helper Methods

### RunPeriodicAsync

Runs an action periodically, waiting for each tick before execution:

```csharp
await PeriodicTimerHelper.RunPeriodicAsync(
    TimeSpan.FromMinutes(1),          // Period
    async ct =>                        // Action
    {
        await RefreshCacheAsync(ct);
    },
    stoppingToken);                    // Cancellation
```

### RunPeriodicImmediateAsync

Executes immediately on startup, then runs periodically:

```csharp
// Execute immediately, then every 30 seconds
await PeriodicTimerHelper.RunPeriodicImmediateAsync(
    TimeSpan.FromSeconds(30),
    async ct =>
    {
        await SyncDataAsync(ct);
    },
    stoppingToken);
```

### RunPeriodicWithErrorHandlingAsync

Continues execution even when errors occur:

```csharp
await PeriodicTimerHelper.RunPeriodicWithErrorHandlingAsync(
    TimeSpan.FromMinutes(5),
    async ct =>
    {
        await ProcessBatchAsync(ct);
    },
    ex =>
    {
        _logger.LogError(ex, "Batch processing failed");
    },
    stoppingToken);
```

## Background Service Pattern

### Before (Legacy Timer)

```csharp
// ❌ Old pattern with System.Timers.Timer
public class LegacyBackgroundService : IHostedService
{
    private System.Timers.Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new System.Timers.Timer(5000);
        _timer.Elapsed += async (s, e) =>
        {
            await DoWorkAsync(); // ⚠️ Async void-like behavior
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

### After (PeriodicTimer)

```csharp
// ✅ Modern pattern with PeriodicTimer
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
            _logger.LogInformation("Service stopping gracefully");
        }
    }
}
```

## Integration with TimeProvider

For testable code, use `TimeProvider`:

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
        // Get current time through abstraction
        var now = _timeProvider.GetUtcNow();
        
        // Create timer through TimeProvider
        using var timer = _timeProvider.CreateTimer(
            callback: _ => { },
            state: null,
            dueTime: TimeSpan.FromSeconds(5),
            period: TimeSpan.FromSeconds(5));
    }
}
```

### Testing with FakeTimeProvider

```csharp
using Microsoft.Extensions.Time.Testing;

[Fact]
public async Task Service_ShouldProcessOnSchedule()
{
    // Arrange
    var fakeTime = new FakeTimeProvider();
    var service = new TestableService(fakeTime);

    // Act - Advance time
    fakeTime.Advance(TimeSpan.FromSeconds(5));

    // Assert
    // Verify expected behavior
}
```

## Migrated Services in Mvp24Hours

The following services have been updated to use PeriodicTimer:

| Service | Module | Description |
|---------|--------|-------------|
| `CronJobService<T>` | CronJob | CRON-based scheduled tasks |
| `OutboxProcessor` | CQRS | Integration event publishing |
| `OutboxCleanupService` | CQRS | Outbox message cleanup |
| `InboxCleanupService` | CQRS | Inbox message cleanup |
| `ScheduledCommandHostedService` | CQRS | Scheduled command processing |
| `WriteBehindBackgroundService` | Caching | Write-behind cache flushing |
| `ScheduledMessageBackgroundService` | RabbitMQ | Scheduled message processing |

## Extension Methods

### WaitForNextTickAsync with Timeout

```csharp
using Mvp24Hours.Core.Infrastructure.Timers;

using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

// Wait with timeout
var tickOccurred = await timer.WaitForNextTickAsync(
    timeout: TimeSpan.FromSeconds(5),
    cancellationToken: stoppingToken);

if (!tickOccurred)
{
    // Timeout occurred before tick
}
```

## Best Practices

### 1. Always Use `using` Statement

```csharp
// ✅ Correct - Timer is disposed properly
using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

// ❌ Wrong - Timer leak
var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
```

### 2. Handle Cancellation Properly

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
    // Graceful shutdown - don't throw
    _logger.LogInformation("Shutting down...");
}
```

### 3. Execute Immediately When Needed

```csharp
// Process immediately, then periodically
await ProcessAsync(stoppingToken);

using var timer = new PeriodicTimer(interval);
while (await timer.WaitForNextTickAsync(stoppingToken))
{
    await ProcessAsync(stoppingToken);
}
```

### 4. Use Small Intervals for Better Responsiveness

```csharp
// For long delays, break into smaller intervals
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

## Performance Considerations

- PeriodicTimer is more efficient than Task.Delay for repeated waits
- No thread pool thread is blocked while waiting
- Proper disposal releases internal resources immediately
- Consider batch processing to reduce overhead

## See Also

- [TimeProvider Abstraction](time-provider.md)
- [.NET 9 Features](dotnet9-features.md)
- [Microsoft Documentation](https://learn.microsoft.com/en-us/dotnet/api/system.threading.periodictimer)

