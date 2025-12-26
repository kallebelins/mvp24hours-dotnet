//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Metrics;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Providers
{
    /// <summary>
    /// Base class for distributed lock providers.
    /// Provides common functionality for lock acquisition, renewal, and release.
    /// </summary>
    /// <remarks>
    /// This base class implements the common retry logic and timeout handling
    /// that all providers share. Subclasses must implement the provider-specific
    /// lock acquisition and release logic.
    /// </remarks>
    public abstract class BaseDistributedLockProvider : IDistributedLock
    {
        private readonly DistributedLockMetrics? _metrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDistributedLockProvider"/> class.
        /// </summary>
        /// <param name="metrics">Optional metrics collector.</param>
        protected BaseDistributedLockProvider(DistributedLockMetrics? metrics = null)
        {
            _metrics = metrics;
        }

        /// <summary>
        /// Gets the provider name for logging and identification.
        /// </summary>
        protected abstract string ProviderName { get; }

        /// <summary>
        /// Attempts to acquire a lock using the provider-specific implementation.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="lockId">A unique identifier for this lock instance.</param>
        /// <param name="duration">The duration the lock should be held.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// A tuple indicating success and the lock metadata (lockId, expiresAt, fencedToken).
        /// </returns>
        protected abstract Task<(bool Success, string LockId, DateTimeOffset ExpiresAt, long? FencedToken)> TryAcquireLockCoreAsync(
            string resource,
            string lockId,
            TimeSpan duration,
            CancellationToken cancellationToken);

        /// <summary>
        /// Releases a lock using the provider-specific implementation.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="lockId">The lock identifier to release.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if the lock was released; otherwise, <c>false</c>.</returns>
        protected abstract Task<bool> ReleaseLockCoreAsync(
            string resource,
            string lockId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Renews a lock using the provider-specific implementation.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="lockId">The lock identifier to renew.</param>
        /// <param name="duration">The new duration for the lock.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if the lock was renewed; otherwise, <c>false</c>.</returns>
        protected abstract Task<bool> RenewLockCoreAsync(
            string resource,
            string lockId,
            TimeSpan duration,
            CancellationToken cancellationToken);

        /// <summary>
        /// Checks if a lock is currently held using the provider-specific implementation.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if the lock is held; otherwise, <c>false</c>.</returns>
        protected abstract Task<bool> IsLockedCoreAsync(
            string resource,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets whether this provider supports fenced tokens.
        /// </summary>
        protected virtual bool SupportsFencing => false;

        /// <inheritdoc />
        public async Task<LockAcquisitionResult> TryAcquireAsync(
            string resource,
            DistributedLockOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resource);

            options ??= DistributedLockOptions.Default;
            var attemptedAt = DateTimeOffset.UtcNow;
            var lockId = Guid.NewGuid().ToString("N");
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(options.AcquisitionTimeout);

                while (!timeoutCts.Token.IsCancellationRequested)
                {
                    var (success, acquiredLockId, expiresAt, fencedToken) = await TryAcquireLockCoreAsync(
                        resource,
                        lockId,
                        options.LockDuration,
                        timeoutCts.Token);

                    if (success)
                    {
                        stopwatch.Stop();
                        var lockHandle = CreateLockHandle(resource, acquiredLockId, expiresAt, fencedToken, options);
                        var completedAt = DateTimeOffset.UtcNow;

                        // Record successful acquisition
                        _metrics?.RecordAcquisition(resource, true, stopwatch.Elapsed);

                        return LockAcquisitionResult.Acquired(
                            lockHandle,
                            fencedToken,
                            attemptedAt,
                            completedAt);
                    }

                    // Wait before retrying
                    await Task.Delay(options.RetryDelay, timeoutCts.Token);
                }

                // Timeout
                stopwatch.Stop();
                var timeoutResult = LockAcquisitionResult.Timeout(
                    $"Lock acquisition timed out after {options.AcquisitionTimeout.TotalSeconds} seconds.",
                    attemptedAt,
                    DateTimeOffset.UtcNow);

                // Record failed acquisition (timeout)
                _metrics?.RecordAcquisition(resource, false, stopwatch.Elapsed);

                if (options.ThrowOnFailure)
                {
                    throw new Exceptions.DistributedLockAcquisitionException(
                        resource,
                        LockAcquisitionStatus.Timeout,
                        timeoutResult.ErrorMessage ?? "Lock acquisition timed out.");
                }

                return timeoutResult;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return LockAcquisitionResult.Timeout(
                    "Lock acquisition was cancelled.",
                    attemptedAt,
                    DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var failedResult = LockAcquisitionResult.Failed(
                    $"Lock acquisition failed: {ex.Message}",
                    ex,
                    attemptedAt,
                    DateTimeOffset.UtcNow);

                // Record failed acquisition
                _metrics?.RecordAcquisition(resource, false, stopwatch.Elapsed);

                if (options.ThrowOnFailure)
                {
                    throw new Exceptions.DistributedLockAcquisitionException(
                        resource,
                        LockAcquisitionStatus.Failed,
                        failedResult.ErrorMessage ?? "Lock acquisition failed.",
                        ex);
                }

                return failedResult;
            }
        }

        /// <inheritdoc />
        public Task<LockAcquisitionResult> TryAcquireWithFenceAsync(
            string resource,
            DistributedLockOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            // If fencing is not supported, fall back to regular acquisition
            if (!SupportsFencing)
            {
                return TryAcquireAsync(resource, options, cancellationToken);
            }

            // Enable fencing in options
            options ??= DistributedLockOptions.Default;
            options = new DistributedLockOptions
            {
                AcquisitionTimeout = options.AcquisitionTimeout,
                LockDuration = options.LockDuration,
                EnableAutoRenewal = options.EnableAutoRenewal,
                RenewalInterval = options.RenewalInterval,
                EnableFencing = true, // Force enable
                RetryDelay = options.RetryDelay,
                ThrowOnFailure = options.ThrowOnFailure
            };

            return TryAcquireAsync(resource, options, cancellationToken);
        }

        /// <inheritdoc />
        public Task<bool> IsLockedAsync(
            string resource,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(resource);
            return IsLockedCoreAsync(resource, cancellationToken);
        }

        /// <summary>
        /// Creates a lock handle for the acquired lock.
        /// </summary>
        protected abstract ILockHandle CreateLockHandle(
            string resource,
            string lockId,
            DateTimeOffset expiresAt,
            long? fencedToken,
            DistributedLockOptions options);

        /// <summary>
        /// Records a lock release in metrics.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        protected void RecordRelease(string resource)
        {
            _metrics?.RecordRelease(resource);
        }
    }
}

