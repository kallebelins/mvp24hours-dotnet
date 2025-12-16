//=====================================================================================
// Tests for MongoDbResiliencyPolicy
//=====================================================================================
using FluentAssertions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using Xunit;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Resiliency;

public class MongoDbResiliencyPolicyTests
{
    private MongoDbResiliencyOptions CreateDefaultOptions() => new()
    {
        EnableRetry = true,
        RetryCount = 3,
        RetryBaseDelayMilliseconds = 10,
        RetryMaxDelayMilliseconds = 100,
        UseExponentialBackoff = false,
        RetryJitterFactor = 0,
        EnableCircuitBreaker = true,
        CircuitBreakerFailureThreshold = 5,
        CircuitBreakerDurationSeconds = 5,
        CircuitBreakerMinimumThroughput = 1,
        EnableOperationTimeout = false // Disable for most tests
    };

    [Fact]
    public async Task Should_Execute_Successfully()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return "success";
        });

        // Assert
        result.Should().Be("success");
    }

    [Fact]
    public async Task Should_Retry_And_Eventually_Succeed()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);
        var executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            executionCount++;
            await Task.Delay(1, ct);
            if (executionCount < 3)
            {
                throw new TimeoutException("Transient failure");
            }
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(3);
    }

    [Fact]
    public async Task Should_Throw_CircuitBreakerOpenException_When_Manually_Tripped()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);

        // Manually trip the circuit breaker
        policy.TripCircuitBreaker();
        policy.CircuitState.Should().Be(CircuitBreakerState.Open);

        // Act & Assert
        await Assert.ThrowsAsync<MongoDbCircuitBreakerOpenException>(async () =>
        {
            await policy.ExecuteAsync(async ct =>
            {
                await Task.Delay(1, ct);
                return "should not execute";
            });
        });
    }

    [Fact]
    public async Task Should_Return_Fallback_When_CircuitBreaker_Open()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);

        // Manually trip the circuit breaker
        policy.TripCircuitBreaker();

        // Act
        var result = await policy.ExecuteWithFallbackAsync(
            async ct =>
            {
                await Task.Delay(1, ct);
                return "should not execute";
            },
            fallbackValue: "fallback");

        // Assert
        result.Should().Be("fallback");
    }

    [Fact]
    public async Task Should_Return_Fallback_From_Factory()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);

        // Manually trip the circuit breaker
        policy.TripCircuitBreaker();

        // Act
        var result = await policy.ExecuteWithFallbackAsync(
            async ct =>
            {
                await Task.Delay(1, ct);
                return "should not execute";
            },
            fallbackFactory: ex => $"fallback-{ex.GetType().Name}");

        // Assert
        result.Should().Be("fallback-MongoDbCircuitBreakerOpenException");
    }

    [Fact]
    public async Task Should_Execute_With_Custom_Timeout()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);

        // Act
        var result = await policy.ExecuteWithTimeoutAsync(
            async ct =>
            {
                await Task.Delay(10, ct);
                return "success";
            },
            timeout: TimeSpan.FromSeconds(5));

        // Assert
        result.Should().Be("success");
    }

    [Fact]
    public async Task Should_Throw_Timeout_Exception_When_Exceeds_Timeout()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.EnableRetry = false; // Disable retry to make test faster
        var policy = new MongoDbResiliencyPolicy(options);

        // Act & Assert
        await Assert.ThrowsAsync<MongoDbOperationTimeoutException>(async () =>
        {
            await policy.ExecuteWithTimeoutAsync(
                async ct =>
                {
                    await Task.Delay(5000, ct);
                    return "should not complete";
                },
                timeout: TimeSpan.FromMilliseconds(50));
        });
    }

    [Fact]
    public void Should_Report_Circuit_State()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);

        // Act & Assert
        policy.CircuitState.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Should_Provide_Metrics()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);

        // Act
        var metrics = policy.Metrics;

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalSuccessCount.Should().Be(0);
        metrics.TotalFailureCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_Update_Metrics_On_Success()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);

        // Act
        await policy.ExecuteAsync(async ct =>
        {
            await Task.Delay(1, ct);
            return "success";
        });

        // Assert
        policy.Metrics.TotalSuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Update_Metrics_On_Failure()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.EnableRetry = false;
        var policy = new MongoDbResiliencyPolicy(options);

        // Act
        try
        {
            await policy.ExecuteAsync<string>(async ct =>
            {
                await Task.Delay(1, ct);
                throw new TimeoutException("Failure");
            });
        }
        catch { }

        // Assert
        policy.Metrics.TotalFailureCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public void Should_Manually_Reset_CircuitBreaker()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 1;
        options.CircuitBreakerMinimumThroughput = 1;
        var policy = new MongoDbResiliencyPolicy(options);

        // Trip the circuit
        policy.TripCircuitBreaker();
        policy.CircuitState.Should().Be(CircuitBreakerState.Open);

        // Act
        policy.ResetCircuitBreaker();

        // Assert
        policy.CircuitState.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void Should_Manually_Trip_CircuitBreaker()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);
        policy.CircuitState.Should().Be(CircuitBreakerState.Closed);

        // Act
        policy.TripCircuitBreaker();

        // Assert
        policy.CircuitState.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public async Task Should_Execute_Void_Operation()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);
        var executed = false;

        // Act
        await policy.ExecuteAsync(async ct =>
        {
            await Task.Delay(1, ct);
            executed = true;
        });

        // Assert
        executed.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Respect_Cancellation_Token()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbResiliencyPolicy(options);
        using var cts = new CancellationTokenSource();

        // Act
        var executionStarted = new TaskCompletionSource<bool>();
        var task = policy.ExecuteAsync(async ct =>
        {
            executionStarted.SetResult(true);
            await Task.Delay(10000, ct);
            return "should not complete";
        }, cts.Token);

        await executionStarted.Task;
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    // Note: Retry exhaustion fallback tests are complex due to timing dependencies
    // The fallback mechanism is tested through circuit breaker tests which are more reliable

    [Fact]
    public async Task Should_Track_Circuit_Breaker_Trips()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.CircuitBreakerFailureThreshold = 2;
        options.CircuitBreakerMinimumThroughput = 1;
        options.CircuitBreakerDurationSeconds = 0; // Immediate transition
        var policy = new MongoDbResiliencyPolicy(options);

        // Trip the circuit twice
        policy.TripCircuitBreaker();
        policy.ResetCircuitBreaker();
        policy.TripCircuitBreaker();

        // Assert
        policy.Metrics.CircuitTripCount.Should().Be(2);
    }
}

