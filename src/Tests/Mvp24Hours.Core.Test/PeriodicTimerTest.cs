//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Infrastructure.Timers;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Mvp24Hours.Core.Test
{
    /// <summary>
    /// Tests for PeriodicTimerHelper and PeriodicTimer extensions.
    /// </summary>
    public class PeriodicTimerTest
    {
        #region PeriodicTimerHelper.RunPeriodicAsync Tests

        [Fact]
        public async Task RunPeriodicAsync_ShouldExecuteActionMultipleTimes()
        {
            // Arrange
            var executionCount = 0;
            using var cts = new CancellationTokenSource();
            
            // Act
            var task = PeriodicTimerHelper.RunPeriodicAsync(
                TimeSpan.FromMilliseconds(50),
                async ct =>
                {
                    executionCount++;
                    if (executionCount >= 3)
                    {
                        cts.Cancel();
                    }
                    await Task.CompletedTask;
                },
                cts.Token);

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            Assert.Equal(3, executionCount);
        }

        [Fact]
        public async Task RunPeriodicAsync_ShouldStopOnCancellation()
        {
            // Arrange
            var executionCount = 0;
            using var cts = new CancellationTokenSource();
            
            // Cancel after 150ms (should allow ~2 executions with 50ms period)
            cts.CancelAfter(150);

            // Act
            await PeriodicTimerHelper.RunPeriodicAsync(
                TimeSpan.FromMilliseconds(50),
                async ct =>
                {
                    executionCount++;
                    await Task.CompletedTask;
                },
                cts.Token);

            // Assert
            Assert.True(executionCount >= 1 && executionCount <= 3);
        }

        [Fact]
        public async Task RunPeriodicAsync_ShouldThrowOnNullAction()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                PeriodicTimerHelper.RunPeriodicAsync(
                    TimeSpan.FromSeconds(1),
                    null!,
                    CancellationToken.None));
        }

        [Fact]
        public async Task RunPeriodicAsync_ShouldThrowOnZeroPeriod()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                PeriodicTimerHelper.RunPeriodicAsync(
                    TimeSpan.Zero,
                    async ct => await Task.CompletedTask,
                    CancellationToken.None));
        }

        [Fact]
        public async Task RunPeriodicAsync_ShouldThrowOnNegativePeriod()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                PeriodicTimerHelper.RunPeriodicAsync(
                    TimeSpan.FromMilliseconds(-100),
                    async ct => await Task.CompletedTask,
                    CancellationToken.None));
        }

        #endregion

        #region PeriodicTimerHelper.RunPeriodicImmediateAsync Tests

        [Fact]
        public async Task RunPeriodicImmediateAsync_ShouldExecuteImmediately()
        {
            // Arrange
            var firstExecutionTime = DateTimeOffset.MinValue;
            var startTime = DateTimeOffset.UtcNow;
            using var cts = new CancellationTokenSource();

            // Act
            var task = PeriodicTimerHelper.RunPeriodicImmediateAsync(
                TimeSpan.FromMilliseconds(500),
                async ct =>
                {
                    if (firstExecutionTime == DateTimeOffset.MinValue)
                    {
                        firstExecutionTime = DateTimeOffset.UtcNow;
                    }
                    cts.Cancel();
                    await Task.CompletedTask;
                },
                cts.Token);

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - First execution should happen within 50ms of start (not waiting for period)
            var delay = firstExecutionTime - startTime;
            Assert.True(delay.TotalMilliseconds < 50, $"First execution took {delay.TotalMilliseconds}ms");
        }

        [Fact]
        public async Task RunPeriodicImmediateAsync_ShouldContinuePeriodically()
        {
            // Arrange
            var executionCount = 0;
            using var cts = new CancellationTokenSource();
            
            // Act
            var task = PeriodicTimerHelper.RunPeriodicImmediateAsync(
                TimeSpan.FromMilliseconds(50),
                async ct =>
                {
                    executionCount++;
                    if (executionCount >= 3)
                    {
                        cts.Cancel();
                    }
                    await Task.CompletedTask;
                },
                cts.Token);

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            Assert.Equal(3, executionCount);
        }

        #endregion

        #region PeriodicTimerHelper.RunPeriodicWithErrorHandlingAsync Tests

        [Fact]
        public async Task RunPeriodicWithErrorHandling_ShouldCallErrorHandler()
        {
            // Arrange
            var errorCount = 0;
            var executionCount = 0;
            using var cts = new CancellationTokenSource();

            // Act
            var task = PeriodicTimerHelper.RunPeriodicWithErrorHandlingAsync(
                TimeSpan.FromMilliseconds(50),
                async ct =>
                {
                    executionCount++;
                    if (executionCount == 2)
                    {
                        throw new InvalidOperationException("Test error");
                    }
                    if (executionCount >= 4)
                    {
                        cts.Cancel();
                    }
                    await Task.CompletedTask;
                },
                ex =>
                {
                    errorCount++;
                },
                cts.Token);

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            Assert.Equal(1, errorCount);
            Assert.Equal(4, executionCount);
        }

        [Fact]
        public async Task RunPeriodicWithErrorHandling_ShouldContinueAfterError()
        {
            // Arrange
            var executionCount = 0;
            using var cts = new CancellationTokenSource();

            // Act
            var task = PeriodicTimerHelper.RunPeriodicWithErrorHandlingAsync(
                TimeSpan.FromMilliseconds(50),
                async ct =>
                {
                    executionCount++;
                    if (executionCount == 1)
                    {
                        throw new Exception("First error");
                    }
                    if (executionCount >= 3)
                    {
                        cts.Cancel();
                    }
                    await Task.CompletedTask;
                },
                ex => { }, // Ignore errors
                cts.Token);

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - Should continue after error
            Assert.Equal(3, executionCount);
        }

        [Fact]
        public async Task RunPeriodicWithErrorHandling_ShouldThrowOnNullErrorHandler()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                PeriodicTimerHelper.RunPeriodicWithErrorHandlingAsync(
                    TimeSpan.FromSeconds(1),
                    async ct => await Task.CompletedTask,
                    null!,
                    CancellationToken.None));
        }

        #endregion

        #region PeriodicTimerHelper.CreateTimer Tests

        [Fact]
        public void CreateTimer_ShouldThrowOnNullTimeProvider()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                PeriodicTimerHelper.CreateTimer(null!, TimeSpan.FromSeconds(1)));
        }

        [Fact]
        public void CreateTimer_ShouldThrowOnZeroPeriod()
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PeriodicTimerHelper.CreateTimer(TimeProvider.System, TimeSpan.Zero));
        }

        [Fact]
        public void CreateTimer_ShouldCreateValidTimer()
        {
            // Act
            using var timer = PeriodicTimerHelper.CreateTimer(TimeProvider.System, TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.NotNull(timer);
        }

        #endregion

        #region PeriodicTimerExtensions.WaitForNextTickAsync Tests

        [Fact]
        public async Task WaitForNextTickAsync_WithTimeout_ShouldReturnTrueOnTick()
        {
            // Arrange
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));

            // Act
            var result = await timer.WaitForNextTickAsync(
                TimeSpan.FromSeconds(1),
                CancellationToken.None);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task WaitForNextTickAsync_WithTimeout_ShouldReturnFalseOnTimeout()
        {
            // Arrange
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

            // Act
            var result = await timer.WaitForNextTickAsync(
                TimeSpan.FromMilliseconds(50),
                CancellationToken.None);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task WaitForNextTickAsync_WithTimeout_ShouldThrowOnCancellation()
        {
            // Arrange
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            // TaskCanceledException is a subclass of OperationCanceledException
            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                timer.WaitForNextTickAsync(TimeSpan.FromSeconds(1), cts.Token));
            Assert.True(exception is OperationCanceledException || exception is TaskCanceledException);
        }

        #endregion

        #region PeriodicTimer Behavior Tests

        [Fact]
        public async Task PeriodicTimer_ShouldMaintainConsistentInterval()
        {
            // Arrange
            var intervals = new System.Collections.Generic.List<double>();
            var lastTick = Stopwatch.GetTimestamp();
            var tickCount = 0;
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
            using var cts = new CancellationTokenSource();

            // Act
            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    var now = Stopwatch.GetTimestamp();
                    var elapsed = (now - lastTick) * 1000.0 / Stopwatch.Frequency;
                    intervals.Add(elapsed);
                    lastTick = now;
                    tickCount++;

                    if (tickCount >= 5)
                    {
                        cts.Cancel();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - Average interval should be close to 50ms
            var avgInterval = 0.0;
            foreach (var interval in intervals)
            {
                avgInterval += interval;
            }
            avgInterval /= intervals.Count;
            
            Assert.True(avgInterval >= 40 && avgInterval <= 100,
                $"Average interval was {avgInterval}ms, expected ~50ms");
        }

        [Fact]
        public async Task PeriodicTimer_DisposeShouldStopWaiting()
        {
            // Arrange
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
            var waited = false;
            var completed = false;

            // Act
            var task = Task.Run(async () =>
            {
                waited = true;
                var result = await timer.WaitForNextTickAsync();
                completed = !result; // Should return false when disposed
            });

            // Give time for the wait to start
            await Task.Delay(50);
            Assert.True(waited);
            Assert.False(completed);

            timer.Dispose();
            await task;

            // Assert
            Assert.True(completed);
        }

        [Fact]
        public async Task PeriodicTimer_ShouldNotOverlapExecutions()
        {
            // Arrange
            var concurrentExecutions = 0;
            var maxConcurrent = 0;
            var lockObj = new object();
            using var cts = new CancellationTokenSource();

            // Act
            var task = PeriodicTimerHelper.RunPeriodicAsync(
                TimeSpan.FromMilliseconds(50),
                async ct =>
                {
                    lock (lockObj)
                    {
                        concurrentExecutions++;
                        if (concurrentExecutions > maxConcurrent)
                        {
                            maxConcurrent = concurrentExecutions;
                        }
                    }

                    await Task.Delay(100, ct); // Simulate work longer than period

                    lock (lockObj)
                    {
                        concurrentExecutions--;
                    }
                },
                cts.Token);

            // Wait for a few potential iterations
            await Task.Delay(300);
            cts.Cancel();

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert - PeriodicTimer waits for each tick, so no overlap
            Assert.Equal(1, maxConcurrent);
        }

        #endregion

        #region Integration Tests

        [Fact]
        public async Task PeriodicTimer_ShouldWorkWithTimeProvider()
        {
            // Arrange
            var timeProvider = TimeProvider.System;
            var executionCount = 0;
            using var cts = new CancellationTokenSource();

            // Act - Using TimeProvider.CreatePeriodicTimer indirectly through CreateTimer
            using var timerWrapper = PeriodicTimerHelper.CreateTimer(timeProvider, TimeSpan.FromMilliseconds(50));
            
            // Note: ITimer from TimeProvider doesn't have WaitForNextTickAsync,
            // so we test that CreateTimer works
            Assert.NotNull(timerWrapper);
            
            // For actual periodic execution, use PeriodicTimer directly
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
            
            try
            {
                while (await timer.WaitForNextTickAsync(cts.Token))
                {
                    executionCount++;
                    if (executionCount >= 3)
                    {
                        cts.Cancel();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected
            }

            // Assert
            Assert.Equal(3, executionCount);
        }

        [Fact]
        public async Task PeriodicTimer_GracefulShutdownScenario()
        {
            // Arrange
            var startedWork = false;
            var completedWork = false;
            using var cts = new CancellationTokenSource();

            // Act
            var task = PeriodicTimerHelper.RunPeriodicImmediateAsync(
                TimeSpan.FromMilliseconds(100),
                async ct =>
                {
                    startedWork = true;
                    
                    try
                    {
                        await Task.Delay(500, ct); // Long running work
                        completedWork = true;
                    }
                    catch (OperationCanceledException)
                    {
                        // Graceful cancellation during work
                    }
                },
                cts.Token);

            // Wait for work to start
            await Task.Delay(50);
            Assert.True(startedWork);

            // Cancel gracefully
            cts.Cancel();
            await task;

            // Assert - Work was cancelled but no exception bubbled up
            Assert.True(startedWork);
            Assert.False(completedWork); // Work was interrupted
        }

        #endregion
    }
}

