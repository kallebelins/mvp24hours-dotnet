//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Contract
{
    /// <summary>
    /// Interface for distributed lock operations.
    /// Provides atomic lock acquisition and release across distributed systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Distributed locks are essential for coordinating operations across multiple
    /// application instances, preventing race conditions and ensuring exclusive access
    /// to shared resources.
    /// </para>
    /// <para>
    /// <strong>Key Features:</strong>
    /// <list type="bullet">
    /// <item>Atomic lock acquisition with timeout</item>
    /// <item>Automatic lock renewal to prevent expiration during long operations</item>
    /// <item>Fenced tokens for split-brain prevention (when supported by provider)</item>
    /// <item>Graceful timeout handling</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Usage Pattern:</strong>
    /// <code>
    /// var lockResult = await distributedLock.TryAcquireAsync("resource-key", options, cancellationToken);
    /// if (lockResult.IsAcquired)
    /// {
    ///     using (lockResult.LockHandle)
    ///     {
    ///         // Critical section - exclusive access guaranteed
    ///         await DoWorkAsync();
    ///     } // Lock automatically released on dispose
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public interface IDistributedLock
    {
        /// <summary>
        /// Attempts to acquire a distributed lock for the specified resource.
        /// </summary>
        /// <param name="resource">The resource identifier to lock. Must be unique and consistent across instances.</param>
        /// <param name="options">Lock acquisition options (timeout, duration, renewal, fencing).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="LockAcquisitionResult"/> indicating whether the lock was acquired,
        /// timed out, or failed. If acquired, contains a <see cref="ILockHandle"/> for managing the lock.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This operation is atomic - only one instance can acquire the lock at a time.
        /// If the lock is already held by another instance, this method will wait up to
        /// <see cref="DistributedLockOptions.AcquisitionTimeout"/> before timing out.
        /// </para>
        /// <para>
        /// The lock handle implements <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/>,
        /// ensuring automatic release even if an exception occurs.
        /// </para>
        /// <para>
        /// <strong>Thread Safety:</strong> This method is thread-safe and can be called
        /// concurrently from multiple threads.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var options = new DistributedLockOptions
        /// {
        ///     AcquisitionTimeout = TimeSpan.FromSeconds(10),
        ///     LockDuration = TimeSpan.FromMinutes(5),
        ///     EnableAutoRenewal = true,
        ///     RenewalInterval = TimeSpan.FromMinutes(2)
        /// };
        /// 
        /// var result = await distributedLock.TryAcquireAsync("my-resource", options, cancellationToken);
        /// if (result.IsAcquired)
        /// {
        ///     try
        ///     {
        ///         // Critical section
        ///         await ProcessResourceAsync();
        ///     }
        ///     finally
        ///     {
        ///         await result.LockHandle.DisposeAsync();
        ///     }
        /// }
        /// else if (result.IsTimeout)
        /// {
        ///     // Handle timeout - another instance may be holding the lock
        ///     logger.LogWarning("Failed to acquire lock: timeout");
        /// }
        /// </code>
        /// </example>
        Task<LockAcquisitionResult> TryAcquireAsync(
            string resource,
            DistributedLockOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Attempts to acquire a distributed lock with a fenced token.
        /// Fenced tokens provide protection against split-brain scenarios where
        /// multiple instances believe they hold the lock.
        /// </summary>
        /// <param name="resource">The resource identifier to lock.</param>
        /// <param name="options">Lock acquisition options.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>
        /// A <see cref="LockAcquisitionResult"/> with a fenced token if the lock was acquired.
        /// The fenced token can be used to verify lock ownership before performing operations.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Fenced tokens are monotonically increasing numbers that can be used to detect
        /// stale locks. When performing operations, compare the fenced token with the
        /// last known token - if the current token is lower, the lock may be stale.
        /// </para>
        /// <para>
        /// Not all providers support fencing. If fencing is not supported, this method
        /// behaves the same as <see cref="TryAcquireAsync(string, DistributedLockOptions?, CancellationToken)"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = await distributedLock.TryAcquireWithFenceAsync("resource", options, cancellationToken);
        /// if (result.IsAcquired && result.FencedToken.HasValue)
        /// {
        ///     var token = result.FencedToken.Value;
        ///     // Store token for later comparison
        ///     lastKnownToken = token;
        ///     
        ///     // Before performing operation, verify token is still valid
        ///     if (token >= lastKnownToken)
        ///     {
        ///         await PerformOperationAsync();
        ///     }
        /// }
        /// </code>
        /// </example>
        Task<LockAcquisitionResult> TryAcquireWithFenceAsync(
            string resource,
            DistributedLockOptions? options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a lock is currently held for the specified resource.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>
        /// <c>true</c> if the lock is currently held; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method provides a non-blocking check. Note that the lock state may
        /// change immediately after this check, so it should not be used for
        /// synchronization purposes. Use <see cref="TryAcquireAsync"/> for actual locking.
        /// </remarks>
        Task<bool> IsLockedAsync(
            string resource,
            CancellationToken cancellationToken = default);
    }
}

