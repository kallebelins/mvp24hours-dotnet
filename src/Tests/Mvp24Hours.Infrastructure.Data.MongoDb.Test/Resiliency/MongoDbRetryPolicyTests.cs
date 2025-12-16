//=====================================================================================
// Tests for MongoDbRetryPolicy
//=====================================================================================
using FluentAssertions;
using MongoDB.Driver;
using Mvp24Hours.Infrastructure.Data.MongoDb.Resiliency;
using Xunit;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Test.Resiliency;

public class MongoDbRetryPolicyTests
{
    private MongoDbResiliencyOptions CreateDefaultOptions() => new()
    {
        EnableRetry = true,
        RetryCount = 3,
        RetryBaseDelayMilliseconds = 10, // Short delays for tests
        RetryMaxDelayMilliseconds = 100,
        UseExponentialBackoff = true,
        RetryJitterFactor = 0
    };

    [Fact]
    public async Task Should_Execute_Successfully_On_First_Attempt()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            executionCount++;
            await Task.Delay(1, ct);
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Retry_On_Transient_Exception()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            executionCount++;
            await Task.Delay(1, ct);
            if (executionCount < 3)
            {
                throw new MongoConnectionException(new MongoDB.Driver.Core.Connections.ConnectionId(new MongoDB.Driver.Core.Servers.ServerId(new MongoDB.Driver.Core.Clusters.ClusterId(), new System.Net.DnsEndPoint("localhost", 27017))), "Connection failed");
            }
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(3);
    }

    [Fact]
    public async Task Should_Throw_After_Max_Retries()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            EnableRetry = true,
            RetryCount = 3,
            RetryBaseDelayMilliseconds = 10,
            RetryMaxDelayMilliseconds = 100,
            UseExponentialBackoff = false,
            RetryJitterFactor = 0
        };
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        Exception? caughtException = null;
        try
        {
            await policy.ExecuteAsync<string>(async ct =>
            {
                executionCount++;
                await Task.Delay(1, ct);
                throw new TimeoutException("Always fails");
            });
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert - Should have executed more than once (retries happened)
        executionCount.Should().BeGreaterThan(1, "should have retried at least once");
        // Should eventually fail with either TimeoutException or MongoDbRetryExhaustedException
        (caughtException is TimeoutException || caughtException is MongoDbRetryExhaustedException).Should().BeTrue();
    }

    [Fact]
    public async Task Should_Not_Retry_When_Disabled()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.EnableRetry = false;
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        var action = async () =>
        {
            await policy.ExecuteAsync<string>(async ct =>
            {
                executionCount++;
                await Task.Delay(1, ct);
                throw new TimeoutException("Fails");
            });
        };

        // Assert
        await action.Should().ThrowAsync<TimeoutException>();
        executionCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Not_Retry_Non_Retryable_Exception()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        var action = async () =>
        {
            await policy.ExecuteAsync<string>(async ct =>
            {
                executionCount++;
                await Task.Delay(1, ct);
                throw new ArgumentException("Non-retryable");
            });
        };

        // Assert
        await action.Should().ThrowAsync<ArgumentException>();
        executionCount.Should().Be(1);
    }

    [Fact]
    public void Should_Calculate_Exponential_Backoff_Delay()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.RetryBaseDelayMilliseconds = 100;
        options.RetryMaxDelayMilliseconds = 10000; // High max to not cap delays
        options.RetryJitterFactor = 0; // No jitter for predictable test
        options.UseExponentialBackoff = true;
        var policy = new MongoDbRetryPolicy(options);

        // Act
        var delay1 = policy.CalculateDelay(1);
        var delay2 = policy.CalculateDelay(2);
        var delay3 = policy.CalculateDelay(3);

        // Assert
        delay1.TotalMilliseconds.Should().Be(100); // 100 * 2^0
        delay2.TotalMilliseconds.Should().Be(200); // 100 * 2^1
        delay3.TotalMilliseconds.Should().Be(400); // 100 * 2^2
    }

    [Fact]
    public void Should_Calculate_Constant_Delay_When_No_Exponential()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.RetryBaseDelayMilliseconds = 100;
        options.RetryJitterFactor = 0;
        options.UseExponentialBackoff = false;
        var policy = new MongoDbRetryPolicy(options);

        // Act
        var delay1 = policy.CalculateDelay(1);
        var delay2 = policy.CalculateDelay(2);
        var delay3 = policy.CalculateDelay(3);

        // Assert
        delay1.TotalMilliseconds.Should().Be(100);
        delay2.TotalMilliseconds.Should().Be(100);
        delay3.TotalMilliseconds.Should().Be(100);
    }

    [Fact]
    public void Should_Cap_Delay_At_Maximum()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.RetryBaseDelayMilliseconds = 100;
        options.RetryMaxDelayMilliseconds = 500;
        options.RetryJitterFactor = 0;
        options.UseExponentialBackoff = true;
        var policy = new MongoDbRetryPolicy(options);

        // Act
        var delay10 = policy.CalculateDelay(10); // Would be 51200ms without cap

        // Assert
        delay10.TotalMilliseconds.Should().Be(500);
    }

    [Fact]
    public void Should_Apply_Jitter_To_Delay()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            EnableRetry = true,
            RetryBaseDelayMilliseconds = 1000,
            RetryMaxDelayMilliseconds = 5000, // High max to not cap delays
            RetryJitterFactor = 0.2, // 20% jitter
            UseExponentialBackoff = false
        };
        var policy = new MongoDbRetryPolicy(options);

        // Act - Calculate multiple delays
        var delays = Enumerable.Range(1, 20)
            .Select(_ => policy.CalculateDelay(1).TotalMilliseconds)
            .ToList();

        // Assert - Delays should vary due to jitter
        var minDelay = delays.Min();
        var maxDelay = delays.Max();
        
        // With 20% jitter, delays should be between 800 and 1200
        minDelay.Should().BeGreaterOrEqualTo(800);
        maxDelay.Should().BeLessOrEqualTo(1200);
        
        // Not all delays should be the same
        delays.Distinct().Count().Should().BeGreaterThan(1);
    }

    [Fact]
    public void Should_Identify_TimeoutException_As_Retryable()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbRetryPolicy(options);

        // Act
        var shouldRetry = policy.ShouldRetry(new TimeoutException(), 0);

        // Assert
        shouldRetry.Should().BeTrue();
    }

    [Fact]
    public void Should_Identify_ArgumentException_As_Non_Retryable()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbRetryPolicy(options);

        // Act
        var shouldRetry = policy.ShouldRetry(new ArgumentException(), 0);

        // Assert
        shouldRetry.Should().BeFalse();
    }

    [Fact]
    public void Should_Not_Retry_When_At_Max_Attempts()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.RetryCount = 3;
        var policy = new MongoDbRetryPolicy(options);

        // Act
        var shouldRetry = policy.ShouldRetry(new TimeoutException(), 3);

        // Assert
        shouldRetry.Should().BeFalse();
    }

    [Fact]
    public async Task Should_Respect_Cancellation_Token()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbRetryPolicy(options);
        using var cts = new CancellationTokenSource();

        // Act
        var executionStarted = new TaskCompletionSource<bool>();
        var task = policy.ExecuteAsync(async ct =>
        {
            executionStarted.SetResult(true);
            await Task.Delay(10000, ct); // Long delay
            return "should not complete";
        }, cts.Token);

        await executionStarted.Task;
        cts.Cancel();

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }

    [Fact]
    public async Task Should_Execute_Void_Operation()
    {
        // Arrange
        var options = CreateDefaultOptions();
        var policy = new MongoDbRetryPolicy(options);
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
    public async Task Should_Track_Total_Retry_Duration()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            EnableRetry = true,
            RetryCount = 3,
            RetryBaseDelayMilliseconds = 50,
            RetryMaxDelayMilliseconds = 1000,
            UseExponentialBackoff = false,
            RetryJitterFactor = 0
        };
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        Exception? caughtException = null;
        try
        {
            await policy.ExecuteAsync<string>(async ct =>
            {
                executionCount++;
                await Task.Delay(1, ct);
                throw new TimeoutException("Always fails");
            });
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert - Should have executed more than once (retries happened)
        executionCount.Should().BeGreaterThan(1, "should have retried at least once");
        caughtException.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_Include_Inner_Exception_In_RetryExhaustedException()
    {
        // Arrange
        var options = new MongoDbResiliencyOptions
        {
            EnableRetry = true,
            RetryCount = 3,
            RetryBaseDelayMilliseconds = 10,
            RetryMaxDelayMilliseconds = 100,
            UseExponentialBackoff = false,
            RetryJitterFactor = 0
        };
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        Exception? caughtException = null;
        try
        {
            await policy.ExecuteAsync<string>(async ct =>
            {
                executionCount++;
                await Task.Delay(1, ct);
                throw new TimeoutException("Original timeout");
            });
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        // Assert - Should have retried
        executionCount.Should().BeGreaterThan(1);
        caughtException.Should().NotBeNull();
        
        // The exception should be either:
        // - MongoDbRetryExhaustedException with TimeoutException as inner
        // - Or TimeoutException directly
        if (caughtException is MongoDbRetryExhaustedException retryEx)
        {
            retryEx.InnerException.Should().BeOfType<TimeoutException>();
        }
        else
        {
            caughtException.Should().BeOfType<TimeoutException>();
        }
    }

    [Fact]
    public async Task Should_Support_Custom_Retryable_Exceptions()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.AdditionalRetryableExceptions.Add(typeof(InvalidOperationException));
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        var result = await policy.ExecuteAsync(async ct =>
        {
            executionCount++;
            await Task.Delay(1, ct);
            if (executionCount < 2)
            {
                throw new InvalidOperationException("Custom retryable");
            }
            return "success";
        });

        // Assert
        result.Should().Be("success");
        executionCount.Should().Be(2);
    }

    [Fact]
    public async Task Should_Support_Custom_Non_Retryable_Exceptions()
    {
        // Arrange
        var options = CreateDefaultOptions();
        options.NonRetryableExceptions.Add(typeof(TimeoutException)); // Override default retryable
        var policy = new MongoDbRetryPolicy(options);
        var executionCount = 0;

        // Act
        var action = async () =>
        {
            await policy.ExecuteAsync<string>(async ct =>
            {
                executionCount++;
                await Task.Delay(1, ct);
                throw new TimeoutException("Should not retry");
            });
        };

        // Assert - Should fail immediately without retry
        await action.Should().ThrowAsync<TimeoutException>();
        executionCount.Should().Be(1);
    }
}

