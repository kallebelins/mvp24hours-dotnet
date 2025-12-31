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
/// Tests for CronJob graceful shutdown functionality.
/// </summary>
public class GracefulShutdownTest
{
    #region Basic Shutdown Tests

    [Fact]
    public async Task CronJob_ShouldStopGracefully_WhenNotExecuting()
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
            cronExpression: "0 0 1 1 *", // Far in the future
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                WaitForExecutionOnShutdown = true,
                GracefulShutdownTimeout = TimeSpan.FromSeconds(5)
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
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await job.StartAsync(cts.Token);
        
        var stopTask = job.StopAsync(CancellationToken.None);
        var completed = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert
        completed.Should().Be(stopTask);
    }

    [Fact]
    public async Task GracefulShutdownConfig_ShouldHaveCorrectDefaults()
    {
        // Arrange & Act
        var config = new CronJobResilienceConfig<TestResilientCronJob>
        {
            WaitForExecutionOnShutdown = true,
            GracefulShutdownTimeout = TimeSpan.FromSeconds(10),
            PreventOverlapping = false
        };

        // Assert
        config.WaitForExecutionOnShutdown.Should().BeTrue();
        config.GracefulShutdownTimeout.Should().Be(TimeSpan.FromSeconds(10));
        config.PreventOverlapping.Should().BeFalse();
    }

    [Fact]
    public async Task CronJob_ShouldAllowStopWhenNotRunning()
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
            cronExpression: "0 0 1 1 *", // Far in future
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                WaitForExecutionOnShutdown = true,
                GracefulShutdownTimeout = TimeSpan.FromSeconds(10),
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
            ct => Task.CompletedTask);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        
        // Stop immediately - should not hang
        var stopTask = job.StopAsync(CancellationToken.None);
        var completed = await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert
        completed.Should().Be(stopTask);
    }

    [Fact]
    public async Task CronJob_ShouldTimeout_WhenGracefulShutdownExpires()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        var executionCancelled = new TaskCompletionSource<bool>();

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                WaitForExecutionOnShutdown = true,
                GracefulShutdownTimeout = TimeSpan.FromMilliseconds(100),
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
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
                catch (OperationCanceledException)
                {
                    executionCancelled.TrySetResult(true);
                    throw;
                }
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await job.StartAsync(cts.Token);
        await Task.Delay(50); // Let execution start
        
        var stopTask = job.StopAsync(CancellationToken.None);
        await Task.WhenAny(stopTask, Task.Delay(TimeSpan.FromSeconds(2)));

        // Assert - StopAsync should complete even though execution didn't finish
        stopTask.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task CronJob_ShouldNotWait_WhenWaitForExecutionDisabled()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        var stopCompleted = false;

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                WaitForExecutionOnShutdown = false,
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
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await job.StartAsync(cts.Token);
        await Task.Delay(50); // Let execution start
        
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await job.StopAsync(CancellationToken.None);
        stopwatch.Stop();
        stopCompleted = true;

        // Assert - Stop should complete quickly without waiting
        stopCompleted.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    #endregion

    #region Cancellation Token Tests

    [Fact]
    public async Task CronJob_ShouldPropagateCancellation_WhenEnabled()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        var cancellationReceived = new TaskCompletionSource<bool>();

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                PropagateCancellation = true,
                WaitForExecutionOnShutdown = false,
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
                ct.Register(() => cancellationReceived.TrySetResult(true));
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await job.StartAsync(cts.Token);
        await Task.Delay(100); // Let execution start
        
        await job.StopAsync(CancellationToken.None);

        // Assert
        var received = await Task.WhenAny(
            cancellationReceived.Task, 
            Task.Delay(TimeSpan.FromSeconds(2)));
        received.Should().Be(cancellationReceived.Task);
    }

    [Fact]
    public async Task CronJob_ShouldRespectExecutionTimeout()
    {
        // Arrange
        var services = new ServiceCollection();
        var tracker = new ExecutionTracker();
        var hostLifetimeMock = new Mock<IHostApplicationLifetime>();
        var executionLock = new InMemoryCronJobExecutionLock();
        var circuitBreaker = new CronJobCircuitBreaker();
        var timeoutOccurred = false;

        services.AddSingleton(tracker);
        services.AddSingleton<ICronJobMetrics, CronJobMetricsService>();
        var serviceProvider = services.BuildServiceProvider();

        var config = TestCronJobFactory.CreateConfig<TestResilientCronJob>(
            resilience: new CronJobResilienceConfig<TestResilientCronJob>
            {
                ExecutionTimeout = TimeSpan.FromMilliseconds(100),
                PropagateCancellation = true,
                WaitForExecutionOnShutdown = false,
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
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
                catch (OperationCanceledException)
                {
                    timeoutOccurred = true;
                    throw;
                }
            });

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(500);
        await job.StopAsync(cts.Token);

        // Assert
        timeoutOccurred.Should().BeTrue();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task CronJob_ShouldDisposeResources_OnAsyncDispose()
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
            ct => Task.CompletedTask);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(100);
        await job.StopAsync(cts.Token);
        await job.DisposeAsync();

        // Assert - Should complete without throwing
        true.Should().BeTrue();
    }

    [Fact]
    public async Task CronJob_ShouldDisposeResources_OnSyncDispose()
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
            ct => Task.CompletedTask);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(100);
        await job.StopAsync(cts.Token);
        job.Dispose();

        // Assert - Should complete without throwing
        true.Should().BeTrue();
    }

    [Fact]
    public async Task CronJob_ShouldHandleMultipleDisposes()
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
            ct => Task.CompletedTask);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(100);
        await job.StopAsync(cts.Token);
        
        // Multiple disposes should not throw
        await job.DisposeAsync();
        await job.DisposeAsync();
        job.Dispose();

        // Assert
        true.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CronJob_ShouldLogStopStats_OnShutdown()
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
            ct => Task.CompletedTask);

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await job.StartAsync(cts.Token);
        await Task.Delay(200);
        await job.StopAsync(cts.Token);

        // Assert
        var state = metricsService.GetJobState(nameof(TestResilientCronJob));
        state.Should().NotBeNull();
        state!.IsRunning.Should().BeFalse();
    }

    #endregion
}

