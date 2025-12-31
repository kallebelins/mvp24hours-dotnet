//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Mvp24Hours.Infrastructure.CronJob.Observability;
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.Services;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.CronJobs;
using Mvp24Hours.Infrastructure.CronJob.Test.Support.Testing;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Test;

/// <summary>
/// Tests for CronJob retry and resilience features.
/// </summary>
public class RetryResilienceTest
{
    #region Retry Tests

    [Fact]
    public async Task Retry_ShouldRetryOnFailure_UntilMaxAttempts()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        int attemptCount = 0;

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                EnableRetry = true,
                MaxRetryAttempts = 3,
                RetryDelay = TimeSpan.FromMilliseconds(10),
                UseExponentialBackoff = false,
                PreventOverlapping = false
            });

        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            async ct =>
            {
                attemptCount++;
                if (attemptCount <= 3)
                {
                    throw new InvalidOperationException($"Failure #{attemptCount}");
                }
                await Task.CompletedTask;
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        
        // Wait for execution
        await Task.Delay(500);
        await job.StopAsync(cts.Token);

        // Assert
        // Initial attempt + 3 retries = 4 total attempts
        attemptCount.Should().Be(4);
        job.RetryCount.Should().Be(3);
    }

    [Fact]
    public async Task Retry_ShouldSucceedOnFirstAttempt_WhenNoFailure()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        int attemptCount = 0;

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: TestCronJobFactory.CreateRetryConfig<TestResilientCronJob>(maxAttempts: 3));

        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            ct =>
            {
                attemptCount++;
                return Task.CompletedTask;
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(cts.Token);

        // Assert
        attemptCount.Should().Be(1);
        job.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task Retry_ShouldUseExponentialBackoff_WhenEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        var retryDelays = new System.Collections.Generic.List<TimeSpan>();

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                EnableRetry = true,
                MaxRetryAttempts = 3,
                RetryDelay = TimeSpan.FromMilliseconds(100),
                UseExponentialBackoff = true,
                RetryJitterFactor = 0, // Disable jitter for predictable testing
                MaxRetryDelay = TimeSpan.FromSeconds(10),
                PreventOverlapping = false,
                OnRetry = (ex, attempt, delay) => retryDelays.Add(delay)
            });

        int attemptCount = 0;
        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            ct =>
            {
                attemptCount++;
                if (attemptCount <= 3)
                {
                    throw new InvalidOperationException($"Failure #{attemptCount}");
                }
                return Task.CompletedTask;
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await job.StartAsync(cts.Token);
        await Task.Delay(2000);
        await job.StopAsync(cts.Token);

        // Assert
        retryDelays.Should().HaveCount(3);
        // Exponential: 100ms, 200ms, 400ms
        retryDelays[0].TotalMilliseconds.Should().BeApproximately(100, 10);
        retryDelays[1].TotalMilliseconds.Should().BeApproximately(200, 20);
        retryDelays[2].TotalMilliseconds.Should().BeApproximately(400, 40);
    }

    [Fact]
    public async Task Retry_ShouldInvokeOnRetryCallback_OnEachRetry()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        var retryInfo = new System.Collections.Generic.List<(Exception Ex, int Attempt)>();

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                EnableRetry = true,
                MaxRetryAttempts = 2,
                RetryDelay = TimeSpan.FromMilliseconds(10),
                UseExponentialBackoff = false,
                PreventOverlapping = false,
                OnRetry = (ex, attempt, delay) => retryInfo.Add((ex, attempt))
            });

        int attemptCount = 0;
        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            ct =>
            {
                attemptCount++;
                throw new InvalidOperationException($"Error {attemptCount}");
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(500);
        await job.StopAsync(cts.Token);

        // Assert
        retryInfo.Should().HaveCount(2);
        retryInfo[0].Attempt.Should().Be(1);
        retryInfo[1].Attempt.Should().Be(2);
        retryInfo[0].Ex.Message.Should().Contain("Error 1");
        retryInfo[1].Ex.Message.Should().Contain("Error 2");
    }

    [Fact]
    public async Task Retry_ShouldRespectShouldRetryOnException_Filter()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        int attemptCount = 0;

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                EnableRetry = true,
                MaxRetryAttempts = 5,
                RetryDelay = TimeSpan.FromMilliseconds(10),
                UseExponentialBackoff = false,
                PreventOverlapping = false,
                // Only retry on InvalidOperationException
                ShouldRetryOnException = ex => ex is InvalidOperationException
            });

        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            ct =>
            {
                attemptCount++;
                if (attemptCount == 1)
                {
                    throw new InvalidOperationException("Retryable");
                }
                // ArgumentException should NOT be retried
                throw new ArgumentException("Non-retryable");
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(500);
        await job.StopAsync(cts.Token);

        // Assert
        // 1 initial + 1 retry (which throws ArgumentException and stops)
        attemptCount.Should().Be(2);
        job.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task Retry_ShouldSucceedAfterTransientFailures()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        int attemptCount = 0;
        bool succeeded = false;

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                EnableRetry = true,
                MaxRetryAttempts = 5,
                RetryDelay = TimeSpan.FromMilliseconds(10),
                UseExponentialBackoff = false,
                PreventOverlapping = false
            });

        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            ct =>
            {
                attemptCount++;
                if (attemptCount < 3)
                {
                    throw new InvalidOperationException("Transient failure");
                }
                succeeded = true;
                return Task.CompletedTask;
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(500);
        await job.StopAsync(cts.Token);

        // Assert
        attemptCount.Should().Be(3);
        succeeded.Should().BeTrue();
        job.RetryCount.Should().Be(2);
        job.ExecutionCount.Should().Be(1);
    }

    #endregion

    #region Resilience Configuration Tests

    [Fact]
    public void ResilienceConfig_Default_ShouldHaveNoFeaturesEnabled()
    {
        // Arrange & Act
        var config = new CronJobResilienceConfig<TestResilientCronJob>();

        // Assert
        config.EnableRetry.Should().BeFalse();
        config.EnableCircuitBreaker.Should().BeFalse();
        config.PreventOverlapping.Should().BeTrue(); // Default is true
    }

    [Fact]
    public void ResilienceConfig_WithRetry_ShouldEnableRetry()
    {
        // Arrange & Act
        var config = CronJobResilienceConfig<TestResilientCronJob>.WithRetry(maxAttempts: 5);

        // Assert
        config.EnableRetry.Should().BeTrue();
        config.MaxRetryAttempts.Should().Be(5);
        config.EnableCircuitBreaker.Should().BeFalse();
    }

    [Fact]
    public void ResilienceConfig_WithCircuitBreaker_ShouldEnableCircuitBreaker()
    {
        // Arrange & Act
        var config = CronJobResilienceConfig<TestResilientCronJob>.WithCircuitBreaker(
            failureThreshold: 10, 
            duration: TimeSpan.FromMinutes(1));

        // Assert
        config.EnableCircuitBreaker.Should().BeTrue();
        config.CircuitBreakerFailureThreshold.Should().Be(10);
        config.CircuitBreakerDuration.Should().Be(TimeSpan.FromMinutes(1));
        config.EnableRetry.Should().BeFalse();
    }

    [Fact]
    public void ResilienceConfig_FullResilience_ShouldEnableAllFeatures()
    {
        // Arrange & Act
        var config = CronJobResilienceConfig<TestResilientCronJob>.FullResilience();

        // Assert
        config.EnableRetry.Should().BeTrue();
        config.EnableCircuitBreaker.Should().BeTrue();
        config.PreventOverlapping.Should().BeTrue();
        config.PropagateCancellation.Should().BeTrue();
    }

    [Fact]
    public void ResilienceConfig_ToString_ShouldShowEnabledFeatures()
    {
        // Arrange
        var config = CronJobResilienceConfig<TestResilientCronJob>.FullResilience();

        // Act
        var result = config.ToString();

        // Assert
        result.Should().Contain("Retry");
        result.Should().Contain("CircuitBreaker");
        result.Should().Contain("PreventOverlapping");
    }

    #endregion
}

