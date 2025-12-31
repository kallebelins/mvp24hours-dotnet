//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
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
/// Tests for CronJob overlapping execution prevention.
/// </summary>
public class OverlappingExecutionTest
{
    #region InMemoryCronJobExecutionLock Tests

    [Fact]
    public async Task ExecutionLock_ShouldAcquireLock_WhenNotLocked()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();

        // Act
        var handle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);

        // Assert
        handle.Should().NotBeNull();
        handle!.JobName.Should().Be("TestJob");
        handle.IsValid.Should().BeTrue();
        executionLock.IsLocked("TestJob").Should().BeTrue();
    }

    [Fact]
    public async Task ExecutionLock_ShouldReturnNull_WhenAlreadyLocked()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();
        var firstHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);

        // Act
        var secondHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);

        // Assert
        firstHandle.Should().NotBeNull();
        secondHandle.Should().BeNull();
    }

    [Fact]
    public async Task ExecutionLock_ShouldReleaseLock_OnDispose()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();
        var handle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);

        // Act
        await handle!.DisposeAsync();

        // Assert
        executionLock.IsLocked("TestJob").Should().BeFalse();
    }

    [Fact]
    public async Task ExecutionLock_ShouldAcquireLock_AfterPreviousRelease()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();

        // First acquisition and release
        var firstHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);
        await firstHandle!.DisposeAsync();

        // Act
        var secondHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);

        // Assert
        secondHandle.Should().NotBeNull();
        executionLock.IsLocked("TestJob").Should().BeTrue();
    }

    [Fact]
    public async Task ExecutionLock_ShouldWaitForLock_WithTimeout()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();
        var firstHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);

        // Start a task to release the lock after a delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(100);
            await firstHandle!.DisposeAsync();
        });

        // Act
        var secondHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.FromSeconds(1));

        // Assert
        secondHandle.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecutionLock_ShouldReturnNull_WhenTimeoutExpires()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();
        var firstHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);

        // Act
        var secondHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.FromMilliseconds(50));

        // Assert
        secondHandle.Should().BeNull();
    }

    [Fact]
    public async Task ExecutionLock_ShouldTrackLockAcquiredTime()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();
        var beforeAcquire = DateTimeOffset.UtcNow;

        // Act
        var handle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);
        var lockTime = executionLock.GetLockAcquiredTime("TestJob");

        // Assert
        lockTime.Should().NotBeNull();
        lockTime!.Value.Should().BeOnOrAfter(beforeAcquire);
        handle!.AcquiredAt.Should().BeOnOrAfter(beforeAcquire);
    }

    [Fact]
    public async Task ExecutionLock_ShouldHandleMultipleJobs_Independently()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();

        // Act
        var handle1 = await executionLock.TryAcquireAsync("Job1", TimeSpan.Zero);
        var handle2 = await executionLock.TryAcquireAsync("Job2", TimeSpan.Zero);

        // Assert
        handle1.Should().NotBeNull();
        handle2.Should().NotBeNull();
        executionLock.IsLocked("Job1").Should().BeTrue();
        executionLock.IsLocked("Job2").Should().BeTrue();
    }

    [Fact]
    public async Task ExecutionLock_ShouldReturnNull_WhenCancelled()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();
        var firstHandle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.Zero);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await executionLock.TryAcquireAsync("TestJob", TimeSpan.FromSeconds(10), cts.Token));
    }

    #endregion

    #region Overlapping Prevention in ResilientCronJob Tests

    [Fact]
    public async Task ResilientCronJob_ShouldSkipExecution_WhenOverlapping()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        int executionCount = 0;
        int skippedCount = 0;

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                PreventOverlapping = true,
                OverlappingWaitTimeout = TimeSpan.Zero,
                LogOverlappingSkipped = true,
                OnOverlappingSkipped = () => skippedCount++
            });

        // Acquire lock externally to simulate overlapping
        var externalLock = await executionLock.TryAcquireAsync(nameof(TestResilientCronJob), TimeSpan.Zero);

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
        skippedCount.Should().Be(1);
        job.SkippedCount.Should().Be(1);

        // Cleanup
        await externalLock!.DisposeAsync();
    }

    [Fact]
    public async Task ResilientCronJob_ShouldExecute_WhenNotOverlapping()
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
                PreventOverlapping = true,
                OverlappingWaitTimeout = TimeSpan.Zero
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
                executionCount++;
                return Task.CompletedTask;
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(cts.Token);

        // Assert
        executionCount.Should().Be(1);
        job.SkippedCount.Should().Be(0);
    }

    [Fact]
    public async Task ResilientCronJob_ShouldReleaseLock_AfterExecution()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                PreventOverlapping = true,
                OverlappingWaitTimeout = TimeSpan.Zero
            });

        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            ct => Task.CompletedTask);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(cts.Token);

        // Assert
        executionLock.IsLocked(nameof(TestResilientCronJob)).Should().BeFalse();
    }

    [Fact]
    public async Task ResilientCronJob_ShouldReleaseLock_OnException()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                PreventOverlapping = true,
                OverlappingWaitTimeout = TimeSpan.Zero,
                EnableRetry = false
            });

        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            ct => throw new InvalidOperationException("Test exception"));

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(cts.Token);

        // Assert
        executionLock.IsLocked(nameof(TestResilientCronJob)).Should().BeFalse();
    }

    [Fact]
    public async Task ResilientCronJob_ShouldRecordSkippedMetric_WhenOverlapping()
    {
        // Arrange
        var services = new ServiceCollection();
        var metricsService = new CronJobMetricsService();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();

        services.AddSingleton<ICronJobMetrics>(metricsService);
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                PreventOverlapping = true,
                OverlappingWaitTimeout = TimeSpan.Zero
            });

        // Acquire lock externally
        var externalLock = await executionLock.TryAcquireAsync(nameof(TestResilientCronJob), TimeSpan.Zero);

        var job = new TestResilientCronJob(
            config,
            hostLifetimeMock.Object,
            serviceProvider,
            executionLock,
            circuitBreaker,
            NullLogger<ResilientCronJobService<TestResilientCronJob>>.Instance,
            TimeProvider.System,
            ct => Task.CompletedTask);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(cts.Token);

        // Assert
        var jobState = metricsService.GetJobState(nameof(TestResilientCronJob));
        jobState.Should().NotBeNull();
        jobState!.TotalSkipped.Should().BeGreaterThan(0);
        jobState.LastSkipReason.Should().Be("overlapping");

        // Cleanup
        await externalLock!.DisposeAsync();
    }

    #endregion

    #region Concurrent Execution Tests

    [Fact]
    public async Task ExecutionLock_ShouldPreventConcurrentAccess()
    {
        // Arrange
        var executionLock = new InMemoryCronJobExecutionLock();
        int concurrentExecutions = 0;
        int maxConcurrent = 0;
        var executionTasks = new System.Collections.Generic.List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            executionTasks.Add(Task.Run(async () =>
            {
                var handle = await executionLock.TryAcquireAsync("TestJob", TimeSpan.FromSeconds(5));
                if (handle != null)
                {
                    var current = Interlocked.Increment(ref concurrentExecutions);
                    maxConcurrent = Math.Max(maxConcurrent, current);
                    
                    await Task.Delay(50); // Simulate work
                    
                    Interlocked.Decrement(ref concurrentExecutions);
                    await handle.DisposeAsync();
                }
            }));
        }

        await Task.WhenAll(executionTasks);

        // Assert
        maxConcurrent.Should().Be(1);
    }

    #endregion
}

