using System.Threading.RateLimiting;
using Mvp24Hours.Core.Contract.Infrastructure.Pipe;
using Mvp24Hours.Core.Contract.Infrastructure.RateLimiting;
using Mvp24Hours.Infrastructure.Pipe;
using Mvp24Hours.Infrastructure.Pipe.Operations;

namespace Mvp24Hours.Application.Pipe.Test.Support;

internal static class PipeTestHelpers
{
    public static IPipelineMessage CreateMessage(string? key = null, object? value = null)
    {
        var message = new PipelineMessage();
        if (key != null && value != null)
        {
            message.AddContent(key, value);
        }

        return message;
    }

    public static IPipelineMessage WithCurrentOperation(this IPipelineMessage message, object operation)
    {
        message.AddContent("CurrentOperation", operation);
        return message;
    }
}

internal sealed class TrackingOperation(string name) : OperationBase
{
    public static List<string> ExecutionOrder { get; } = [];

    public override void Execute(IPipelineMessage input)
    {
        ExecutionOrder.Add(name);
        input.AddContent(name, true);
    }
}

internal sealed class FaultyOperation : OperationBase
{
    public override void Execute(IPipelineMessage input)
    {
        input.SetFailure();
    }
}

internal sealed class ThrowingOperation(string message = "boom") : OperationBase
{
    public override void Execute(IPipelineMessage input)
    {
        throw new InvalidOperationException(message);
    }
}

internal sealed class SlowOperation(TimeSpan delay) : OperationBase
{
    public override void Execute(IPipelineMessage input)
    {
        Thread.Sleep(delay);
    }
}

internal sealed class TestBulkheadOperation(string key, int maxConcurrency, int queueLimit = 0) : OperationBase, IBulkheadOperation
{
    public string BulkheadKey { get; } = key;
    public int MaxConcurrency { get; } = maxConcurrency;
    public int QueueLimit { get; } = queueLimit;
    public TimeSpan? QueueTimeout { get; } = TimeSpan.FromMilliseconds(50);

    public override void Execute(IPipelineMessage input)
    {
        Thread.Sleep(100);
    }
}

internal sealed class TestCircuitBreakerOperation(
    string key = "test-circuit",
    int failureThreshold = 2,
    TimeSpan? openDuration = null) : ICircuitBreakerOperation
{
    public string CircuitBreakerKey { get; } = key;
    public int FailureThreshold { get; } = failureThreshold;
    public TimeSpan OpenDuration { get; } = openDuration ?? TimeSpan.FromMinutes(1);
}

internal sealed class TestRetryableOperation(
    int maxRetryAttempts = 2,
    TimeSpan? initialDelay = null) : IRetryableOperation
{
    public int MaxRetryAttempts { get; } = maxRetryAttempts;
    public TimeSpan InitialRetryDelay { get; } = initialDelay ?? TimeSpan.FromMilliseconds(1);
    public double BackoffMultiplier { get; } = 1.0;
    public TimeSpan? MaxRetryDelay { get; } = TimeSpan.FromMilliseconds(10);
    public Type[]? RetryableExceptions { get; } = [typeof(IOException)];
}

internal sealed class TestFallbackOperation : IFallbackOperation
{
    public bool FallbackExecuted { get; private set; }

    public Task ExecuteFallbackAsync(IPipelineMessage input, Exception? exception)
    {
        FallbackExecuted = true;
        input.AddContent("fallback", true);
        return Task.CompletedTask;
    }
}

internal sealed class TestRateLimitedOperation : IRateLimitedOperation
{
    public string RateLimiterKey { get; init; } = "pipe-test";
    public RateLimitingAlgorithm Algorithm { get; init; } = RateLimitingAlgorithm.FixedWindow;
    public int PermitLimit { get; init; } = 1;
    public TimeSpan Window { get; init; } = TimeSpan.FromMinutes(1);
    public int SegmentsPerWindow { get; init; } = 1;
    public TimeSpan ReplenishmentPeriod { get; init; } = TimeSpan.FromMinutes(1);
    public int TokensPerPeriod { get; init; } = 1;
    public bool AutoReplenishment { get; init; } = true;
    public int QueueLimit { get; init; } = 0;
    public QueueProcessingOrder QueueProcessingOrder { get; init; } = QueueProcessingOrder.OldestFirst;
    public TimeSpan? QueueTimeout { get; init; }

    public void OnRateLimited(TimeSpan? retryAfter) { }
}

internal sealed class TestSagaContext
{
    public int Value { get; set; }
    public List<string> Log { get; } = [];
}
