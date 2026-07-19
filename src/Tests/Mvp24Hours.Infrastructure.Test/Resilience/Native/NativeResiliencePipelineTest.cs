//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Resilience.Native;
using Polly.Timeout;

namespace Mvp24Hours.Infrastructure.Test.Resilience.Native;

[Trait("Category", "Unit")]
public class NativeResiliencePipelineTest
{
    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new NativeResiliencePipeline<string>(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void VoidConstructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new NativeResiliencePipeline(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("options");
    }

    [Fact]
    public void Name_ShouldReflectOptions()
    {
        var options = new NativeResilienceOptions { Name = "custom-pipeline" };
        DisableAllStrategies(options);

        var pipeline = new NativeResiliencePipeline<string>(options);

        pipeline.Name.Should().Be("custom-pipeline");
        pipeline.UnderlyingPipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteTaskAsync_OnSuccess_ShouldReturnResult()
    {
        NativeResilienceOptions options = CreateFastRetryOptions();
        var pipeline = new NativeResiliencePipeline<string>(options);

        string result = await pipeline.ExecuteTaskAsync(_ => Task.FromResult("ok"));

        result.Should().Be("ok");
    }

    [Fact]
    public async Task ExecuteAsync_ValueTask_ShouldReturnResult()
    {
        NativeResilienceOptions options = CreateFastRetryOptions();
        var pipeline = new NativeResiliencePipeline<int>(options);

        int result = await pipeline.ExecuteAsync(_ => ValueTask.FromResult(42));

        result.Should().Be(42);
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithRetry_ShouldRetryAndSucceed()
    {
        int attempts = 0;
        int retryCallbacks = 0;
        NativeResilienceOptions options = CreateFastRetryOptions();
        options.OnRetry = (_, _, _) => retryCallbacks++;

        var pipeline = new NativeResiliencePipeline<string>(options);

        string result = await pipeline.ExecuteTaskAsync(_ =>
        {
            attempts++;
            if (attempts < 3)
            {
                throw new InvalidOperationException("transient");
            }

            return Task.FromResult("recovered");
        });

        result.Should().Be("recovered");
        attempts.Should().Be(3);
        retryCallbacks.Should().Be(2);
    }

    [Fact]
    public async Task ExecuteTaskAsync_WhenRetryDisabled_ShouldNotRetry()
    {
        int attempts = 0;
        var options = new NativeResilienceOptions
        {
            EnableRetry = false,
            EnableCircuitBreaker = false,
            EnableTimeout = false
        };

        var pipeline = new NativeResiliencePipeline<string>(options);

        Func<Task> act = () => pipeline.ExecuteTaskAsync(_ =>
        {
            attempts++;
            throw new InvalidOperationException("fail");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithTimeout_ShouldThrowTimeoutRejectedException()
    {
        bool timedOut = false;
        var options = new NativeResilienceOptions
        {
            EnableRetry = false,
            EnableCircuitBreaker = false,
            EnableTimeout = true,
            TimeoutDuration = TimeSpan.FromMilliseconds(50),
            OnTimeout = _ => timedOut = true
        };

        var pipeline = new NativeResiliencePipeline<string>(options);

        Func<Task> act = () => pipeline.ExecuteTaskAsync(async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
            return "late";
        });

        await act.Should().ThrowAsync<TimeoutRejectedException>();
        timedOut.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithRetryableExceptionTypes_ShouldOnlyRetryListedTypes()
    {
        int attempts = 0;
        var options = new NativeResilienceOptions
        {
            EnableRetry = true,
            RetryMaxAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(1),
            RetryUseJitter = false,
            EnableCircuitBreaker = false,
            EnableTimeout = false,
            RetryableExceptionTypes = [typeof(TimeoutException)]
        };

        var pipeline = new NativeResiliencePipeline<string>(options);

        Func<Task> act = () => pipeline.ExecuteTaskAsync(_ =>
        {
            attempts++;
            throw new InvalidOperationException("not retryable");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        attempts.Should().Be(1);
    }

    [Fact]
    public async Task VoidPipeline_ExecuteTaskAsync_ShouldRetry()
    {
        int attempts = 0;
        NativeResilienceOptions options = CreateFastRetryOptions();
        var pipeline = new NativeResiliencePipeline(options);

        await pipeline.ExecuteTaskAsync(_ =>
        {
            attempts++;
            if (attempts < 2)
            {
                throw new InvalidOperationException("retry");
            }

            return Task.CompletedTask;
        });

        attempts.Should().Be(2);
        pipeline.Name.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task VoidPipeline_ExecuteAsync_ValueTask_ShouldSucceed()
    {
        bool executed = false;
        NativeResilienceOptions options = CreateFastRetryOptions();
        options.EnableRetry = false;

        var pipeline = new NativeResiliencePipeline(options);

        await pipeline.ExecuteAsync(_ =>
        {
            executed = true;
            return ValueTask.CompletedTask;
        });

        executed.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithCircuitBreaker_ShouldOpenAfterFailures()
    {
        bool opened = false;
        var options = new NativeResilienceOptions
        {
            Name = "cb-test",
            EnableRetry = false,
            EnableTimeout = false,
            EnableCircuitBreaker = true,
            CircuitBreakerFailureRatio = 0.5,
            CircuitBreakerMinimumThroughput = 2,
            CircuitBreakerSamplingDuration = TimeSpan.FromSeconds(30),
            CircuitBreakerBreakDuration = TimeSpan.FromSeconds(30),
            OnCircuitBreakerOpen = _ => opened = true
        };

        var pipeline = new NativeResiliencePipeline<string>(options);

        for (int i = 0; i < 2; i++)
        {
            Func<Task> act = () => pipeline.ExecuteTaskAsync(
                _ => throw new InvalidOperationException("fail"));
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        opened.Should().BeTrue();

        Func<Task> blocked = () => pipeline.ExecuteTaskAsync(_ => Task.FromResult("ok"));
        await blocked.Should().ThrowAsync<Exception>();
    }

    private static NativeResilienceOptions CreateFastRetryOptions()
    {
        return new NativeResilienceOptions
        {
            Name = "fast-retry",
            EnableRetry = true,
            RetryMaxAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(1),
            RetryMaxDelay = TimeSpan.FromMilliseconds(20),
            RetryUseJitter = false,
            RetryBackoffType = ResilienceBackoffType.Constant,
            EnableCircuitBreaker = false,
            EnableTimeout = false,
            ShouldRetryOnException = _ => true
        };
    }

    private static void DisableAllStrategies(NativeResilienceOptions options)
    {
        options.EnableRetry = false;
        options.EnableCircuitBreaker = false;
        options.EnableTimeout = false;
    }
}
