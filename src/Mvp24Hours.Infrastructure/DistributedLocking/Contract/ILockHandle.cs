//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Contract
{
    /// <summary>
    /// Represents a handle to an acquired distributed lock.
    /// Provides methods for managing the lock lifecycle, including renewal and release.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The lock handle manages the lifecycle of a distributed lock. When disposed,
    /// the lock is automatically released. The handle also supports automatic renewal
    /// to prevent lock expiration during long-running operations.
    /// </para>
    /// <para>
    /// <strong>Automatic Renewal:</strong>
    /// If auto-renewal is enabled, the lock will be automatically renewed at regular
    /// intervals to prevent expiration. Renewal continues until the handle is disposed
    /// or renewal fails.
    /// </para>
    /// <para>
    /// <strong>Disposal:</strong>
    /// Always dispose the lock handle, preferably using a <c>using</c> statement or
    /// <c>await using</c> for async disposal. This ensures the lock is released even
    /// if an exception occurs.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await distributedLock.TryAcquireAsync("resource", options, cancellationToken);
    /// if (result.IsAcquired)
    /// {
    ///     await using (result.LockHandle)
    ///     {
    ///         // Lock is automatically renewed if enabled
    ///         await LongRunningOperationAsync();
    ///     } // Lock automatically released here
    /// }
    /// </code>
    /// </example>
    public interface ILockHandle : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the resource identifier for this lock.
        /// </summary>
        string Resource { get; }

        /// <summary>
        /// Gets the fenced token for this lock, if fencing is supported.
        /// Returns <c>null</c> if fencing is not supported or not enabled.
        /// </summary>
        /// <remarks>
        /// Fenced tokens are monotonically increasing numbers that help detect stale locks
        /// in split-brain scenarios. Compare this token with previously known tokens to
        /// verify lock validity.
        /// </remarks>
        long? FencedToken { get; }

        /// <summary>
        /// Gets whether this lock handle is still valid (lock is still held).
        /// </summary>
        /// <remarks>
        /// A lock handle becomes invalid if:
        /// <list type="bullet">
        /// <item>The lock expires (if auto-renewal fails or is disabled)</item>
        /// <item>The lock is released (manually or via disposal)</item>
        /// <item>The underlying lock provider encounters an error</item>
        /// </list>
        /// </remarks>
        bool IsValid { get; }

        /// <summary>
        /// Gets when the lock was acquired.
        /// </summary>
        DateTimeOffset AcquiredAt { get; }

        /// <summary>
        /// Gets when the lock expires (if not renewed).
        /// </summary>
        DateTimeOffset ExpiresAt { get; }

        /// <summary>
        /// Manually renews the lock, extending its expiration time.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>
        /// <c>true</c> if the lock was successfully renewed; otherwise, <c>false</c>
        /// (e.g., if the lock was already released or expired).
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method extends the lock's expiration time by the lock duration specified
        /// in the original acquisition options. If auto-renewal is enabled, this method
        /// is called automatically at regular intervals.
        /// </para>
        /// <para>
        /// Manual renewal is useful when you need to extend the lock duration beyond
        /// what auto-renewal provides, or when auto-renewal is disabled.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var handle = result.LockHandle;
        /// 
        /// // Perform initial work
        /// await DoInitialWorkAsync();
        /// 
        /// // Manually renew if more work is needed
        /// if (await handle.RenewAsync(cancellationToken))
        /// {
        ///     await DoMoreWorkAsync();
        /// }
        /// </code>
        /// </example>
        Task<bool> RenewAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Manually releases the lock before disposal.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>
        /// <c>true</c> if the lock was successfully released; otherwise, <c>false</c>
        /// (e.g., if the lock was already released or expired).
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method explicitly releases the lock. The lock will also be automatically
        /// released when the handle is disposed, so calling this method is optional unless
        /// you need to release the lock before the end of the using block.
        /// </para>
        /// <para>
        /// After calling this method, the handle becomes invalid and should not be used
        /// further. Disposal after calling this method is safe but redundant.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var handle = result.LockHandle;
        /// try
        /// {
        ///     await DoWorkAsync();
        ///     
        ///     // Release lock early if work completes successfully
        ///     await handle.ReleaseAsync(cancellationToken);
        /// }
        /// catch (Exception ex)
        /// {
        ///     // Lock will be released on disposal in finally block
        ///     logger.LogError(ex, "Error during work");
        /// }
        /// </code>
        /// </example>
        Task<bool> ReleaseAsync(CancellationToken cancellationToken = default);
    }
}

