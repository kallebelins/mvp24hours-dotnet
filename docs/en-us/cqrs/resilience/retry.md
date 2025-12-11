# Retry Policies

## Overview

The `RetryBehavior` implements retry policies with exponential backoff for commands that may fail temporarily.

## IRetryable Interface

```csharp
public interface IRetryable
{
    int MaxRetryAttempts { get; }
    int BaseDelayMilliseconds { get; }
}
```

## Retryable Command

```csharp
public record ProcessPaymentCommand 
    : IMediatorCommand<PaymentResult>, IRetryable
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CardToken { get; init; }
    
    // Retry configuration
    public int MaxRetryAttempts => 3;
    public int BaseDelayMilliseconds => 100;
}
```

## RetryBehavior

```csharp
public sealed class RetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRetryable
{
    private readonly ILogger<RetryBehavior<TRequest, TResponse>> _logger;
    private readonly MediatorOptions _options;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var maxAttempts = request.MaxRetryAttempts > 0 
            ? request.MaxRetryAttempts 
            : _options.MaxRetryAttempts;
            
        var baseDelay = request.BaseDelayMilliseconds > 0 
            ? request.BaseDelayMilliseconds 
            : _options.RetryBaseDelayMilliseconds;

        var attempt = 0;
        while (true)
        {
            try
            {
                attempt++;
                return await next();
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                var delay = CalculateDelay(attempt, baseDelay);
                
                _logger.LogWarning(
                    ex,
                    "Attempt {Attempt}/{MaxAttempts} failed for {RequestType}. " +
                    "Retrying in {Delay}ms...",
                    attempt, maxAttempts, typeof(TRequest).Name, delay);

                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is TimeoutException
            or HttpRequestException
            or TaskCanceledException
            or OperationCanceledException;
    }

    private static int CalculateDelay(int attempt, int baseDelay)
    {
        // Exponential backoff with jitter
        var exponential = (int)Math.Pow(2, attempt - 1) * baseDelay;
        var jitter = Random.Shared.Next(0, 100);
        return Math.Min(exponential + jitter, 30000); // Max 30s
    }
}
```

## Configuration

```csharp
services.AddMvpMediator(options =>
{
    options.RegisterHandlersFromAssemblyContaining<Program>();
    options.RegisterRetryBehavior = true;
    
    // Default settings
    options.MaxRetryAttempts = 3;
    options.RetryBaseDelayMilliseconds = 100;
});
```

## Backoff Strategies

### Linear

```
Attempt 1: 100ms
Attempt 2: 200ms
Attempt 3: 300ms
```

### Exponential

```
Attempt 1: 100ms
Attempt 2: 200ms
Attempt 3: 400ms
Attempt 4: 800ms
```

### Exponential with Jitter (recommended)

```
Attempt 1: 100ms + random(0-100)
Attempt 2: 200ms + random(0-100)
Attempt 3: 400ms + random(0-100)
```

## Transient Exceptions

### Default

```csharp
private static bool IsTransient(Exception ex)
{
    return ex is TimeoutException
        or HttpRequestException
        or TaskCanceledException
        or OperationCanceledException;
}
```

### Custom

```csharp
public interface IRetryable
{
    int MaxRetryAttempts { get; }
    int BaseDelayMilliseconds { get; }
    bool ShouldRetry(Exception ex) => IsDefaultTransient(ex);
    
    static bool IsDefaultTransient(Exception ex) => 
        ex is TimeoutException or HttpRequestException;
}

// Usage
public record ProcessPaymentCommand : IMediatorCommand<PaymentResult>, IRetryable
{
    public int MaxRetryAttempts => 3;
    public int BaseDelayMilliseconds => 500;
    
    public bool ShouldRetry(Exception ex) => 
        ex is TimeoutException 
        or PaymentGatewayException { IsRetryable: true };
}
```

## Polly Integration

```csharp
public sealed class PollyRetryBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRetryable
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var policy = Policy
            .Handle<TimeoutException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: request.MaxRetryAttempts,
                sleepDurationProvider: attempt => 
                    TimeSpan.FromMilliseconds(
                        Math.Pow(2, attempt) * request.BaseDelayMilliseconds),
                onRetry: (ex, delay, attempt, _) =>
                {
                    // Log retry
                });

        return await policy.ExecuteAsync(async () => await next());
    }
}
```

## Execution Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      RetryBehavior                               │
├─────────────────────────────────────────────────────────────────┤
│  Attempt 1:                                                     │
│    ├── Execute handler                                          │
│    ├── ❌ TimeoutException                                      │
│    └── Wait 100ms                                               │
│                                                                 │
│  Attempt 2:                                                     │
│    ├── Execute handler                                          │
│    ├── ❌ HttpRequestException                                  │
│    └── Wait 200ms                                               │
│                                                                 │
│  Attempt 3:                                                     │
│    ├── Execute handler                                          │
│    └── ✅ Success → Return result                               │
└─────────────────────────────────────────────────────────────────┘
```

## Best Practices

1. **Only Transient**: Retry only for transient errors
2. **Idempotency**: Commands must be idempotent
3. **Max Attempts**: Reasonable limit (3-5 attempts)
4. **Backoff**: Use exponential with jitter
5. **Total Timeout**: Consider max operation timeout
6. **Logging**: Log all attempts
7. **Metrics**: Monitor retry rate

