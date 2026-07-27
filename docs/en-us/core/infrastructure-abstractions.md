# Infrastructure Abstractions

Mvp24Hours.Core provides small infrastructure contracts and adapters so domain and application code can remain testable. Prefer the native .NET abstraction where one exists and use an adapter for legacy Mvp24Hours contracts.

## TimeProvider and IClock

`TimeProvider` is the recommended .NET 10 abstraction. `AddTimeProvider()` registers `TimeProvider.System` and a `TimeProviderAdapter` as `IClock`, so old and new consumers share the same source of time.

```csharp
using Mvp24Hours.Extensions;

builder.Services.AddTimeProvider();

public sealed class ExpirationService(TimeProvider timeProvider)
{
    public bool IsExpired(DateTimeOffset expiresAt) =>
        timeProvider.GetUtcNow() >= expiresAt;
}
```

The legacy `IClock` exposes:

| Member | Type |
|---|---|
| `UtcNow` | `DateTime` |
| `Now` | `DateTime` |
| `UtcToday` | `DateTime` |
| `Today` | `DateTime` |
| `UtcNowOffset` | `DateTimeOffset` |
| `NowOffset` | `DateTimeOffset` |

### Adapter direction

| Existing dependency | Registration | Result |
|---|---|---|
| Neither | `services.AddTimeProvider()` | `TimeProvider.System` plus `IClock` via `TimeProviderAdapter` |
| Custom `TimeProvider` | `services.AddTimeProvider(provider)` | The supplied provider plus `IClock` |
| Existing `IClock` | `services.AddClock(clock)` | The supplied clock plus `TimeProvider` via `ClockAdapter` |
| `SystemClock` | `services.AddSystemClock()` | `SystemClock.Instance` plus a `ClockAdapter` |

Overloads also accept a `TimeZoneInfo`. `ReplaceTimeProvider` and `ReplaceClock` remove both registrations before installing the replacement, which is useful in tests.

```csharp
using Microsoft.Extensions.Time.Testing;

var fakeTime = new FakeTimeProvider(
    new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

services.AddTimeProvider(fakeTime);
fakeTime.Advance(TimeSpan.FromHours(1));
```

See [TimeProvider](../modernization/time-provider.md) for timer and migration patterns.

## GUID generation

Use `IGuidGenerator.NewGuid()` when generated IDs must be substitutable in tests. `StandardGuidGenerator` delegates to `Guid.NewGuid()`. Register the implementation with the lifetime appropriate for your application:

```csharp
using Mvp24Hours.Core.Infrastructure.GuidGenerators;

services.AddSingleton<IGuidGenerator>(StandardGuidGenerator.Instance);
```

`SequentialGuidGenerator` supports SQL Server, PostgreSQL/string, MySQL/string, and binary layouts. Depending on its constructor, `DeterministicGuidGenerator` returns either a supplied GUID queue or sequential deterministic values, which makes assertions repeatable.

## Other Core infrastructure surfaces

| Concern | Core surface | Guide |
|---|---|---|
| In-memory queues | `IChannel<T>`, `IChannelReader<T>`, `IChannelWriter<T>`, `IChannelFactory` | [Channels](../modernization/channels.md) |
| Keyed dependency injection | keyed registration/resolution extensions and `ServiceKeys` | [Keyed services](../modernization/keyed-services.md) |
| Rate limiting | `IRateLimiterProvider` and related hooks | [Rate limiting](../modernization/rate-limiting.md) |
| Configuration | `IOptionsValidator<TOptions>` and validation extensions | [Options validation](options-validation.md) |
| Cloud-native defaults | `AspireOptions`, health endpoints, correlation accessor | [.NET Aspire](../modernization/aspire.md) |

## Testing guidance

- Inject `TimeProvider` or `IClock`; do not read wall-clock time directly in test-sensitive code.
- Use `FakeTimeProvider` to advance time without delays.
- Keep infrastructure contracts narrow and register fakes through DI.
- Test adapters once; test business behavior against the abstraction consumed by that behavior.
