//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Resiliency
{
    /// <summary>
    /// Interface for managing execution locks to prevent overlapping CronJob executions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a mechanism to ensure only one execution of a CronJob
    /// is running at any given time. This is useful for jobs that should not have
    /// concurrent executions (e.g., database cleanup, report generation).
    /// </para>
    /// <para>
    /// <strong>Usage Pattern:</strong>
    /// <code>
    /// var lockHandle = await lock.TryAcquireAsync("MyJob", timeout, cancellationToken);
    /// if (lockHandle != null)
    /// {
    ///     try
    ///     {
    ///         await DoWork();
    ///     }
    ///     finally
    ///     {
    ///         await lockHandle.ReleaseAsync();
    ///     }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public interface ICronJobExecutionLock
    {
        /// <summary>
        /// Attempts to acquire an execution lock for the specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to lock.</param>
        /// <param name="timeout">
        /// Maximum time to wait for acquiring the lock.
        /// Use <see cref="TimeSpan.Zero"/> for immediate return if lock is not available.
        /// </param>
        /// <param name="cancellationToken">Cancellation token to observe.</param>
        /// <returns>
        /// An <see cref="ICronJobLockHandle"/> if the lock was acquired; otherwise, <c>null</c>.
        /// </returns>
        Task<ICronJobLockHandle?> TryAcquireAsync(
            string jobName,
            TimeSpan timeout,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a lock is currently held for the specified job.
        /// </summary>
        /// <param name="jobName">The name of the job to check.</param>
        /// <returns><c>true</c> if the job is currently locked; otherwise, <c>false</c>.</returns>
        bool IsLocked(string jobName);

        /// <summary>
        /// Gets the time when the current lock was acquired for the specified job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <returns>
        /// The <see cref="DateTimeOffset"/> when the lock was acquired, or <c>null</c> if not locked.
        /// </returns>
        DateTimeOffset? GetLockAcquiredTime(string jobName);
    }

    /// <summary>
    /// Handle for a successfully acquired execution lock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This handle must be disposed or released when the execution completes
    /// to allow subsequent executions to proceed.
    /// </para>
    /// <para>
    /// Implements <see cref="IAsyncDisposable"/> for use with <c>await using</c> syntax.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// await using var lockHandle = await executionLock.TryAcquireAsync("MyJob", timeout);
    /// if (lockHandle != null)
    /// {
    ///     await DoWork();
    /// }
    /// // Lock is automatically released when exiting the using block
    /// </code>
    /// </example>
    public interface ICronJobLockHandle : IAsyncDisposable
    {
        /// <summary>
        /// Gets the name of the job associated with this lock.
        /// </summary>
        string JobName { get; }

        /// <summary>
        /// Gets the time when this lock was acquired.
        /// </summary>
        DateTimeOffset AcquiredAt { get; }

        /// <summary>
        /// Gets whether this lock is still valid (not released).
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Releases the lock, allowing subsequent executions.
        /// </summary>
        /// <returns>A task representing the async operation.</returns>
        Task ReleaseAsync();
    }
}

