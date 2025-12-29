//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Infrastructure.Timers
{
    /// <summary>
    /// Helper methods for working with PeriodicTimer (.NET 6+).
    /// </summary>
    /// <remarks>
    /// <para>
    /// PeriodicTimer is the modern replacement for System.Timers.Timer and System.Threading.Timer.
    /// Key benefits:
    /// </para>
    /// <list type="bullet">
    /// <item>Native async/await support with WaitForNextTickAsync()</item>
    /// <item>Proper CancellationToken integration for graceful shutdown</item>
    /// <item>No callback-based API that can lead to overlapping executions</item>
    /// <item>Prevents timer drift by maintaining consistent intervals</item>
    /// <item>Thread-safe and designed for modern async patterns</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Simple periodic execution
    /// await PeriodicTimerHelper.RunPeriodicAsync(
    ///     TimeSpan.FromSeconds(5),
    ///     async ct =>
    ///     {
    ///         await ProcessWorkAsync(ct);
    ///     },
    ///     stoppingToken);
    /// </code>
    /// </example>
    public static class PeriodicTimerHelper
    {
        /// <summary>
        /// Runs an action periodically using PeriodicTimer.
        /// </summary>
        /// <param name="period">The time interval between ticks.</param>
        /// <param name="action">The action to execute on each tick.</param>
        /// <param name="cancellationToken">Token to signal cancellation.</param>
        /// <returns>A task that completes when the timer is cancelled.</returns>
        /// <example>
        /// <code>
        /// await PeriodicTimerHelper.RunPeriodicAsync(
        ///     TimeSpan.FromSeconds(10),
        ///     async ct =>
        ///     {
        ///         await DoWorkAsync(ct);
        ///     },
        ///     stoppingToken);
        /// </code>
        /// </example>
        public static async Task RunPeriodicAsync(
            TimeSpan period,
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
            }

            using var timer = new PeriodicTimer(period);
            
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal cancellation, exit gracefully
            }
        }

        /// <summary>
        /// Runs an action periodically with immediate first execution.
        /// </summary>
        /// <param name="period">The time interval between ticks.</param>
        /// <param name="action">The action to execute on each tick.</param>
        /// <param name="cancellationToken">Token to signal cancellation.</param>
        /// <returns>A task that completes when the timer is cancelled.</returns>
        /// <remarks>
        /// Unlike <see cref="RunPeriodicAsync"/>, this method executes the action immediately
        /// before waiting for the first tick.
        /// </remarks>
        /// <example>
        /// <code>
        /// // Execute immediately, then every 30 seconds
        /// await PeriodicTimerHelper.RunPeriodicImmediateAsync(
        ///     TimeSpan.FromSeconds(30),
        ///     async ct =>
        ///     {
        ///         await RefreshCacheAsync(ct);
        ///     },
        ///     stoppingToken);
        /// </code>
        /// </example>
        public static async Task RunPeriodicImmediateAsync(
            TimeSpan period,
            Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);

            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
            }

            try
            {
                // Execute immediately
                await action(cancellationToken).ConfigureAwait(false);

                // Then run periodically
                using var timer = new PeriodicTimer(period);
                
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    await action(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal cancellation, exit gracefully
            }
        }

        /// <summary>
        /// Runs an action periodically with error handling.
        /// </summary>
        /// <param name="period">The time interval between ticks.</param>
        /// <param name="action">The action to execute on each tick.</param>
        /// <param name="onError">Error handler called when action throws.</param>
        /// <param name="cancellationToken">Token to signal cancellation.</param>
        /// <returns>A task that completes when the timer is cancelled.</returns>
        /// <remarks>
        /// This method catches exceptions from the action and calls the error handler,
        /// then continues with the next tick. Only OperationCanceledException when
        /// cancellation is requested will stop the loop.
        /// </remarks>
        /// <example>
        /// <code>
        /// await PeriodicTimerHelper.RunPeriodicWithErrorHandlingAsync(
        ///     TimeSpan.FromMinutes(1),
        ///     async ct =>
        ///     {
        ///         await ProcessBatchAsync(ct);
        ///     },
        ///     ex =>
        ///     {
        ///         _logger.LogError(ex, "Batch processing failed");
        ///     },
        ///     stoppingToken);
        /// </code>
        /// </example>
        public static async Task RunPeriodicWithErrorHandlingAsync(
            TimeSpan period,
            Func<CancellationToken, Task> action,
            Action<Exception> onError,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(action);
            ArgumentNullException.ThrowIfNull(onError);

            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
            }

            using var timer = new PeriodicTimer(period);
            
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
                {
                    try
                    {
                        await action(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw; // Re-throw to exit the loop
                    }
                    catch (Exception ex)
                    {
                        onError(ex);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal cancellation, exit gracefully
            }
        }

        /// <summary>
        /// Creates a PeriodicTimer with dynamic period support via TimeProvider.
        /// </summary>
        /// <param name="timeProvider">The time provider for testability.</param>
        /// <param name="period">The initial period.</param>
        /// <returns>A PeriodicTimer instance.</returns>
        /// <remarks>
        /// <para>
        /// TimeProvider.CreatePeriodicTimer() creates a timer that respects the time provider's
        /// time progression, enabling testability with FakeTimeProvider.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // In production
        /// using var timer = PeriodicTimerHelper.CreateTimer(TimeProvider.System, TimeSpan.FromSeconds(5));
        /// 
        /// // In tests
        /// var fakeTime = new FakeTimeProvider();
        /// using var timer = PeriodicTimerHelper.CreateTimer(fakeTime, TimeSpan.FromSeconds(5));
        /// fakeTime.Advance(TimeSpan.FromSeconds(5));
        /// </code>
        /// </example>
        public static ITimer CreateTimer(TimeProvider timeProvider, TimeSpan period)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);

            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Period must be greater than zero.");
            }

            return timeProvider.CreateTimer(
                callback: _ => { }, // No-op callback, we use WaitForNextTickAsync pattern
                state: null,
                dueTime: period,
                period: period);
        }
    }

    /// <summary>
    /// Extensions for working with PeriodicTimer in background services.
    /// </summary>
    public static class PeriodicTimerExtensions
    {
        /// <summary>
        /// Waits for the next tick with timeout support.
        /// </summary>
        /// <param name="timer">The timer.</param>
        /// <param name="timeout">The maximum time to wait.</param>
        /// <param name="cancellationToken">Token to signal cancellation.</param>
        /// <returns>True if a tick occurred, false if timeout or cancelled.</returns>
        public static async Task<bool> WaitForNextTickAsync(
            this PeriodicTimer timer,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(timer);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                return await timer.WaitForNextTickAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred (not user cancellation)
                return false;
            }
        }
    }
}

