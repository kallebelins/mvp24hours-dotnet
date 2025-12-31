//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
/// Tests for CronJob circuit breaker functionality.
/// </summary>
public class CircuitBreakerTest
{
    #region State Transition Tests

    [Fact]
    public void CircuitBreaker_ShouldStartInClosedState()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();

        // Act
        var state = circuitBreaker.GetState("TestJob");

        // Assert
        state.Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void CircuitBreaker_ShouldOpenAfterFailureThreshold()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();
        var stateChanges = new System.Collections.Generic.List<(CircuitBreakerState From, CircuitBreakerState To)>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30),
                (from, to) => stateChanges.Add((from, to)));
        }

        // Assert
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Open);
        stateChanges.Should().ContainSingle()
            .Which.Should().Be((CircuitBreakerState.Closed, CircuitBreakerState.Open));
    }

    [Fact]
    public void CircuitBreaker_ShouldNotOpenBeforeThreshold()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();

        // Act - Record 4 failures (threshold is 5)
        for (int i = 0; i < 4; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30));
        }

        // Assert
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void CircuitBreaker_ShouldTransitionToHalfOpen_AfterDuration()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var circuitBreaker = new CronJobCircuitBreaker(fakeTimeProvider);
        var duration = TimeSpan.FromSeconds(30);

        // Open the circuit
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, duration);
        }

        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Open);

        // Act - Advance time past the duration
        fakeTimeProvider.Advance(duration.Add(TimeSpan.FromSeconds(1)));
        
        var canExecute = circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));

        // Assert
        canExecute.Should().BeTrue();
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.HalfOpen);
    }

    [Fact]
    public void CircuitBreaker_ShouldCloseAfterSuccessInHalfOpen()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var circuitBreaker = new CronJobCircuitBreaker(fakeTimeProvider);
        var duration = TimeSpan.FromSeconds(30);
        var stateChanges = new System.Collections.Generic.List<(CircuitBreakerState From, CircuitBreakerState To)>();

        // Open the circuit
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, duration);
        }

        // Transition to half-open
        fakeTimeProvider.Advance(duration.Add(TimeSpan.FromSeconds(1)));
        circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));

        // Act - Record success
        circuitBreaker.RecordSuccess("TestJob", successThreshold: 1, 
            (from, to) => stateChanges.Add((from, to)));

        // Assert
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Closed);
        stateChanges.Should().Contain((CircuitBreakerState.HalfOpen, CircuitBreakerState.Closed));
    }

    [Fact]
    public void CircuitBreaker_ShouldReopenOnFailureInHalfOpen()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var circuitBreaker = new CronJobCircuitBreaker(fakeTimeProvider);
        var duration = TimeSpan.FromSeconds(30);

        // Open the circuit
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, duration);
        }

        // Transition to half-open
        fakeTimeProvider.Advance(duration.Add(TimeSpan.FromSeconds(1)));
        circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));

        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.HalfOpen);

        // Act - Record failure in half-open
        circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, duration);

        // Assert
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Open);
    }

    #endregion

    #region Execution Blocking Tests

    [Fact]
    public void CircuitBreaker_ShouldBlockExecution_WhenOpen()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();

        // Open the circuit
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30));
        }

        // Act
        var canExecute = circuitBreaker.AllowExecution("TestJob", 
            failureThreshold: 5, 
            TimeSpan.FromSeconds(30), 
            TimeSpan.FromMinutes(1));

        // Assert
        canExecute.Should().BeFalse();
    }

    [Fact]
    public void CircuitBreaker_ShouldAllowExecution_WhenClosed()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();

        // Act
        var canExecute = circuitBreaker.AllowExecution("TestJob", 
            failureThreshold: 5, 
            TimeSpan.FromSeconds(30), 
            TimeSpan.FromMinutes(1));

        // Assert
        canExecute.Should().BeTrue();
    }

    [Fact]
    public void CircuitBreaker_ShouldAllowSingleExecution_InHalfOpen()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var circuitBreaker = new CronJobCircuitBreaker(fakeTimeProvider);
        var duration = TimeSpan.FromSeconds(30);

        // Open the circuit
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, duration);
        }

        // Transition to half-open
        fakeTimeProvider.Advance(duration.Add(TimeSpan.FromSeconds(1)));
        var firstExecutionAllowed = circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));

        // Act - Try second execution while first is pending
        var secondExecutionAllowed = circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));

        // Assert
        firstExecutionAllowed.Should().BeTrue();
        secondExecutionAllowed.Should().BeFalse();
    }

    #endregion

    #region Metrics Tests

    [Fact]
    public void CircuitBreaker_ShouldTrackMetrics()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();

        // Act - Record some failures
        for (int i = 0; i < 3; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30));
        }

        var metrics = circuitBreaker.GetMetrics("TestJob");

        // Assert
        metrics.Should().NotBeNull();
        metrics!.State.Should().Be(CircuitBreakerState.Closed);
        metrics.FailureCount.Should().Be(3);
        metrics.LastFailureTime.Should().NotBeNull();
    }

    [Fact]
    public void CircuitBreaker_ShouldReturnNullMetrics_ForUnknownJob()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();

        // Act
        var metrics = circuitBreaker.GetMetrics("UnknownJob");

        // Assert
        metrics.Should().BeNull();
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void CircuitBreaker_ShouldResetToClosedState()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();

        // Open the circuit
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30));
        }

        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Open);

        // Act
        circuitBreaker.Reset("TestJob");

        // Assert
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Closed);
    }

    [Fact]
    public void CircuitBreaker_ShouldResetSuccessCount_OnSuccess()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();

        // Record some failures
        for (int i = 0; i < 3; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30));
        }

        // Act - Record success
        circuitBreaker.RecordSuccess("TestJob", successThreshold: 1);

        var metrics = circuitBreaker.GetMetrics("TestJob");

        // Assert
        metrics.Should().NotBeNull();
        metrics!.FailureCount.Should().Be(0);
    }

    #endregion

    #region Sampling Window Tests

    [Fact]
    public void CircuitBreaker_ShouldCleanOldFailures_OutsideSamplingWindow()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var circuitBreaker = new CronJobCircuitBreaker(fakeTimeProvider);
        var samplingDuration = TimeSpan.FromMinutes(1);

        // Record failures
        for (int i = 0; i < 4; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30), samplingDuration);
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30));
        }

        // Act - Advance time past the sampling window
        fakeTimeProvider.Advance(samplingDuration.Add(TimeSpan.FromSeconds(1)));
        
        // This should clean old failures and not open the circuit
        circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30), samplingDuration);
        circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, TimeSpan.FromSeconds(30));

        // Assert
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Closed);
        circuitBreaker.GetMetrics("TestJob")!.FailureCount.Should().Be(1);
    }

    #endregion

    #region Integration with ResilientCronJob Tests

    [Fact]
    public async Task ResilientCronJob_ShouldSkipExecution_WhenCircuitBreakerOpen()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        int executionCount = 0;

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                EnableCircuitBreaker = true,
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerDuration = TimeSpan.FromSeconds(30),
                PreventOverlapping = false,
                EnableRetry = false
            });

        // Open the circuit by recording failures
        for (int i = 0; i < 3; i++)
        {
            circuitBreaker.AllowExecution(nameof(TestResilientCronJob), 3, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure(nameof(TestResilientCronJob), 3, TimeSpan.FromSeconds(30));
        }

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
                executionCount++;
                return Task.CompletedTask;
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(cts.Token);

        // Assert
        executionCount.Should().Be(0);
        job.SkippedCount.Should().BeGreaterThan(0);
        job.CircuitBreakerState.Should().Be(CircuitBreakerState.Open);
    }

    [Fact]
    public void CircuitBreaker_ShouldInvokeCallback_OnStateChange()
    {
        // Arrange
        var circuitBreaker = new CronJobCircuitBreaker();
        var stateChanges = new System.Collections.Generic.List<(CircuitBreakerState From, CircuitBreakerState To)>();
        var failureThreshold = 3;
        var duration = TimeSpan.FromSeconds(30);

        // Act - Record failures to trigger state change
        for (int i = 0; i < failureThreshold; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold, duration, TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold, duration, 
                onStateChange: (from, to) => stateChanges.Add((from, to)));
        }

        // Assert
        stateChanges.Should().ContainSingle();
        stateChanges.Should().Contain((CircuitBreakerState.Closed, CircuitBreakerState.Open));
    }

    #endregion

    #region Success Threshold Tests

    [Fact]
    public void CircuitBreaker_ShouldRequireMultipleSuccesses_WhenThresholdHigher()
    {
        // Arrange
        var fakeTimeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var circuitBreaker = new CronJobCircuitBreaker(fakeTimeProvider);
        var duration = TimeSpan.FromSeconds(30);
        var successThreshold = 3;

        // Open the circuit
        for (int i = 0; i < 5; i++)
        {
            circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));
            circuitBreaker.RecordFailure("TestJob", failureThreshold: 5, duration);
        }

        // Transition to half-open
        fakeTimeProvider.Advance(duration.Add(TimeSpan.FromSeconds(1)));
        circuitBreaker.AllowExecution("TestJob", failureThreshold: 5, duration, TimeSpan.FromMinutes(1));

        // Act - Record only 2 successes (threshold is 3)
        circuitBreaker.RecordSuccess("TestJob", successThreshold);
        circuitBreaker.RecordSuccess("TestJob", successThreshold);

        // Assert - Still in half-open
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.HalfOpen);

        // Act - Record third success
        circuitBreaker.RecordSuccess("TestJob", successThreshold);

        // Assert - Now closed
        circuitBreaker.GetState("TestJob").Should().Be(CircuitBreakerState.Closed);
    }

    #endregion
}

