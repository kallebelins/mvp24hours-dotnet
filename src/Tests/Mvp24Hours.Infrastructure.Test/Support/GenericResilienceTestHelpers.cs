//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Options;

namespace Mvp24Hours.Infrastructure.Test.Support;

internal static class GenericResilienceTestHelpers
{
    public static RetryOptions CreateRetryOptions(
        int maxRetries = 2,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        RetryBackoffType backoffType = RetryBackoffType.Constant,
        double jitterFactor = 0,
        bool useExponentialBackoff = false,
        Func<Exception, bool>? shouldRetry = null)
    {
        return new RetryOptions
        {
            MaxRetries = maxRetries,
            InitialDelay = initialDelay ?? TimeSpan.FromMilliseconds(1),
            MaxDelay = maxDelay ?? TimeSpan.FromMilliseconds(50),
            BackoffType = backoffType,
            JitterFactor = jitterFactor,
            UseExponentialBackoff = useExponentialBackoff,
            ShouldRetryOnException = shouldRetry ?? (_ => true)
        };
    }

    public static CircuitBreakerOptions CreateCircuitBreakerOptions(
        double failureRatio = 0.5,
        int minimumThroughput = 2,
        TimeSpan? samplingDuration = null,
        TimeSpan? breakDuration = null,
        Func<Exception, bool>? shouldCountAsFailure = null)
    {
        return new CircuitBreakerOptions
        {
            FailureRatio = failureRatio,
            MinimumThroughput = minimumThroughput,
            SamplingDuration = samplingDuration ?? TimeSpan.FromSeconds(30),
            BreakDuration = breakDuration ?? TimeSpan.FromSeconds(30),
            ShouldCountAsFailure = shouldCountAsFailure
        };
    }

    public static Func<CancellationToken, Task<T>> Succeed<T>(T value)
    {
        return _ => Task.FromResult(value);
    }

    public static Func<CancellationToken, Task<T>> FailThenSucceed<T>(
        int failures,
        T successValue,
        Exception? exception = null)
    {
        int attempts = 0;
        return _ =>
        {
            attempts++;
            if (attempts <= failures)
            {
                throw exception ?? new InvalidOperationException("transient failure");
            }

            return Task.FromResult(successValue);
        };
    }

    public static Func<CancellationToken, Task<T>> AlwaysFail<T>(Exception? exception = null)
    {
        return _ => throw exception ?? new InvalidOperationException("always fail");
    }

    public static Func<CancellationToken, Task> SucceedVoid()
    {
        return _ => Task.CompletedTask;
    }

    public static Func<CancellationToken, Task> FailThenSucceedVoid(int failures, Exception? exception = null)
    {
        int attempts = 0;
        return _ =>
        {
            attempts++;
            if (attempts <= failures)
            {
                throw exception ?? new InvalidOperationException("transient failure");
            }

            return Task.CompletedTask;
        };
    }
}
