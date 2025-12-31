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
    /// Interface for distributed locking of CronJob executions.
    /// Prevents duplicate executions across multiple instances in a cluster.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implement this interface for distributed environments where multiple
    /// application instances may run the same CronJob. Implementations should use
    /// distributed locking mechanisms such as:
    /// </para>
    /// <list type="bullet">
    /// <item><b>Redis:</b> Using RedLock algorithm</item>
    /// <item><b>SQL Server:</b> Using sp_getapplock</item>
    /// <item><b>PostgreSQL:</b> Using pg_advisory_lock</item>
    /// <item><b>Azure:</b> Using Azure Blob Lease</item>
    /// </list>
    /// </remarks>
    public interface IDistributedCronJobLock
    {
        /// <summary>
        /// Attempts to acquire a distributed lock for a CronJob.
        /// </summary>
        /// <param name="jobName">The name of the job to lock.</param>
        /// <param name="instanceId">Unique identifier for this instance.</param>
        /// <param name="lockDuration">How long the lock should be held.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A lock handle if acquired, null otherwise.</returns>
        Task<IDistributedCronJobLockHandle?> TryAcquireAsync(
            string jobName,
            string instanceId,
            TimeSpan lockDuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a lock is currently held for a job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the lock is held, false otherwise.</returns>
        Task<bool> IsLockedAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets information about the current lock holder.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Lock holder information, or null if not locked.</returns>
        Task<DistributedLockInfo?> GetLockInfoAsync(string jobName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Handle for a distributed lock. Disposing releases the lock.
    /// </summary>
    public interface IDistributedCronJobLockHandle : IAsyncDisposable
    {
        /// <summary>
        /// Gets the name of the locked job.
        /// </summary>
        string JobName { get; }

        /// <summary>
        /// Gets the instance ID that holds the lock.
        /// </summary>
        string InstanceId { get; }

        /// <summary>
        /// Gets when the lock was acquired.
        /// </summary>
        DateTimeOffset AcquiredAt { get; }

        /// <summary>
        /// Gets when the lock expires.
        /// </summary>
        DateTimeOffset ExpiresAt { get; }

        /// <summary>
        /// Gets whether the lock is still valid.
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Extends the lock duration.
        /// </summary>
        /// <param name="extension">Additional time to hold the lock.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if extension succeeded, false otherwise.</returns>
        Task<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases the lock immediately.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ReleaseAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Information about a distributed lock.
    /// </summary>
    public sealed class DistributedLockInfo
    {
        /// <summary>
        /// Gets the name of the locked job.
        /// </summary>
        public string JobName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the instance ID that holds the lock.
        /// </summary>
        public string InstanceId { get; init; } = string.Empty;

        /// <summary>
        /// Gets when the lock was acquired.
        /// </summary>
        public DateTimeOffset AcquiredAt { get; init; }

        /// <summary>
        /// Gets when the lock expires.
        /// </summary>
        public DateTimeOffset ExpiresAt { get; init; }

        /// <summary>
        /// Gets the machine name of the lock holder.
        /// </summary>
        public string? MachineName { get; init; }
    }
}

