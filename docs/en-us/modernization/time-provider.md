# TimeProvider Abstraction (.NET 8+)

The `TimeProvider` is the .NET 8+ standard abstraction for time operations, replacing direct usage of `DateTime.Now` and `DateTime.UtcNow`. This guide explains how to use TimeProvider in Mvp24Hours and migrate from the legacy `IClock` interface.

## Why Use TimeProvider?

```csharp
// ❌ Hard to test - depends on real time
public bool IsExpired(DateTime expirationDate)
{
    return DateTime.UtcNow > expirationDate;
}

// ✅ Testable - uses TimeProvider abstraction
public bool IsExpired(DateTime expirationDate, TimeProvider timeProvider)
{
    return timeProvider.GetUtcNow() > expirationDate;
}
```

### Benefits

- **Standard .NET API**: Works with all .NET 8+ libraries
- **Built-in testing support**: `FakeTimeProvider` from Microsoft.Extensions.TimeProvider.Testing
- **Future-proof**: Official Microsoft standard for time abstraction
- **No custom code to maintain**: Uses native .NET implementation

## Installation

```bash
# For production (already included in .NET 8+)
# No additional package needed

# For testing
dotnet add package Microsoft.Extensions.TimeProvider.Testing
```

## Quick Start

### Register TimeProvider in DI

```csharp
// In Program.cs - registers TimeProvider.System for production
builder.Services.AddTimeProvider();

// Both TimeProvider and IClock are now available
public class OrderService
{
    private readonly TimeProvider _timeProvider;
    private readonly IClock _clock; // Legacy support
    
    public OrderService(TimeProvider timeProvider, IClock clock)
    {
        _timeProvider = timeProvider;
        _clock = clock;
    }
    
    public Order CreateOrder(Cart cart)
    {
        return new Order
        {
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime,
            ExpiresAt = _timeProvider.GetUtcNow().AddDays(30).UtcDateTime
        };
    }
}
```

### Testing with FakeTimeProvider

```csharp
using Microsoft.Extensions.Time.Testing;

[Fact]
public async Task Order_Expires_After_30_Days()
{
    // Arrange
    var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    var services = new ServiceCollection();
    services.ReplaceTimeProvider(fakeTime);
    
    var provider = services.BuildServiceProvider();
    var orderService = provider.GetRequiredService<OrderService>();
    
    // Act
    var order = orderService.CreateOrder(cart);
    
    // Assert - order should not be expired initially
    Assert.False(orderService.IsExpired(order));
    
    // Advance time by 31 days
    fakeTime.Advance(TimeSpan.FromDays(31));
    
    // Assert - order should now be expired
    Assert.True(orderService.IsExpired(order));
}
```

## Adapters: TimeProvider ↔ IClock

Mvp24Hours provides bidirectional adapters for gradual migration:

### TimeProviderAdapter (TimeProvider → IClock)

```csharp
// Wrap TimeProvider as IClock for legacy code
var timeProvider = TimeProvider.System;
IClock clock = new TimeProviderAdapter(timeProvider);

// Or with custom timezone
var easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
IClock clock = new TimeProviderAdapter(timeProvider, easternZone);
```

### ClockAdapter (IClock → TimeProvider)

```csharp
// Wrap existing IClock as TimeProvider for new code
IClock testClock = new TestClock(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
TimeProvider timeProvider = new ClockAdapter(testClock);
```

## DI Registration Methods

### AddTimeProvider()

Registers `TimeProvider.System` and bridges to `IClock`:

```csharp
services.AddTimeProvider();
// Equivalent to:
// services.AddSingleton(TimeProvider.System);
// services.AddSingleton<IClock, TimeProviderAdapter>();
```

### AddTimeProvider(TimeProvider)

Registers a custom TimeProvider (e.g., for testing):

```csharp
var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
services.AddTimeProvider(fakeTime);
```

### AddClock(IClock)

Registers an existing IClock and bridges to TimeProvider:

```csharp
var testClock = new TestClock(DateTime.UtcNow);
services.AddClock(testClock);
```

### AddSystemClock()

Convenience method for registering SystemClock:

```csharp
services.AddSystemClock();
// Equivalent to:
// services.AddClock(SystemClock.Instance);
```

### ReplaceTimeProvider() / ReplaceClock()

Replace existing registrations (useful in tests):

```csharp
// In test setup
services.AddTimeProvider(); // Normal registration
services.ReplaceTimeProvider(fakeTimeProvider); // Replace for testing
```

## CronJobService Integration

`CronJobService` now accepts an optional `TimeProvider` parameter:

```csharp
public class MyCronJob : CronJobService<MyCronJob>
{
    public MyCronJob(
        IScheduleConfig<MyCronJob> config,
        IHostApplicationLifetime hostApplication,
        IServiceProvider serviceProvider,
        ILogger<MyCronJob> logger,
        TimeProvider? timeProvider = null) // Optional - defaults to TimeProvider.System
        : base(config, hostApplication, serviceProvider, logger, timeProvider)
    {
    }
    
    public override async Task DoWork(CancellationToken cancellationToken)
    {
        // Your scheduled work here
    }
}
```

### Testing CronJobs

```csharp
[Fact]
public async Task CronJob_Executes_At_Scheduled_Time()
{
    var fakeTime = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
    
    // Create CronJob with fake time
    var cronJob = new MyCronJob(config, hostApp, serviceProvider, logger, fakeTime);
    
    // Advance time to trigger execution
    fakeTime.SetUtcNow(new DateTimeOffset(2024, 1, 1, 1, 0, 0, TimeSpan.Zero));
    
    // Verify execution
}
```

## ScheduledCommandHostedService Integration

The scheduled command processor also supports `TimeProvider`:

```csharp
public class ScheduledCommandHostedService : BackgroundService
{
    public ScheduledCommandHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledCommandHostedService> logger,
        ScheduledCommandOptions? options = null,
        TimeProvider? timeProvider = null) // Optional
    { }
}
```

## Migration Guide

### From IClock to TimeProvider

**Step 1: Update DI Registration**
```csharp
// Before
services.AddSingleton<IClock, SystemClock>();

// After
services.AddTimeProvider(); // Registers both TimeProvider and IClock
```

**Step 2: Update Constructor Injection**
```csharp
// Before
public MyService(IClock clock)
{
    _utcNow = clock.UtcNow;
}

// After (recommended)
public MyService(TimeProvider timeProvider)
{
    _utcNow = timeProvider.GetUtcNow().UtcDateTime;
}

// Or keep using IClock (still works)
public MyService(IClock clock)
{
    _utcNow = clock.UtcNow;
}
```

**Step 3: Update Tests**
```csharp
// Before
var testClock = new TestClock(DateTime.UtcNow);
services.AddSingleton<IClock>(testClock);

// After (recommended)
var fakeTime = new FakeTimeProvider(DateTimeOffset.UtcNow);
services.ReplaceTimeProvider(fakeTime);
fakeTime.Advance(TimeSpan.FromHours(1)); // Built-in time manipulation
```

## Best Practices

1. **Prefer TimeProvider for new code** - It's the .NET standard
2. **Use FakeTimeProvider for testing** - More features than TestClock
3. **Keep IClock for backward compatibility** - Gradual migration is fine
4. **Inject via DI** - Don't use TimeProvider.System directly in business logic
5. **Use DateTimeOffset** - TimeProvider returns DateTimeOffset, not DateTime

## Comparison: IClock vs TimeProvider

| Feature | IClock (Legacy) | TimeProvider (.NET 8+) |
|---------|-----------------|------------------------|
| Source | Mvp24Hours custom | Microsoft.Extensions.* |
| Return type | DateTime | DateTimeOffset |
| Test provider | TestClock/MockClock | FakeTimeProvider |
| Timer creation | Not supported | CreateTimer() |
| Stopwatch | Not supported | GetTimestamp() |
| Maintainer | Community | Microsoft |

## Related Documentation

- [IClock Documentation](../core/infrastructure-abstractions.md)
- [CronJob Documentation](../cronjob.md)
- [Scheduled Commands](../cqrs/scheduled-commands.md)
- [Microsoft TimeProvider Docs](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider)

