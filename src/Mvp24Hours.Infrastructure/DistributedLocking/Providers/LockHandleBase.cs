//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Providers
{
    /// <summary>
    /// Base implementation of <see cref="ILockHandle"/> with automatic renewal support.
    /// </summary>
    /// <remarks>
    /// This base class provides common functionality for lock handles, including
    /// automatic renewal, expiration tracking, and disposal. Subclasses must
    /// implement the provider-specific release logic.
    /// </remarks>
    public abstract class LockHandleBase : ILockHandle
    {
        protected readonly DistributedLockOptions _options;
        private readonly CancellationTokenSource _renewalCts;
        private readonly Task? _renewalTask;
        private bool _disposed;
        private bool _released;
        private DateTimeOffset _expiresAt;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockHandleBase"/> class.
        /// </summary>
        /// <param name="resource">The resource identifier.</param>
        /// <param name="lockId">The lock identifier.</param>
        /// <param name="expiresAt">When the lock expires.</param>
        /// <param name="fencedToken">Optional fenced token.</param>
        /// <param name="options">Lock options.</param>
        protected LockHandleBase(
            string resource,
            string lockId,
            DateTimeOffset expiresAt,
            long? fencedToken,
            DistributedLockOptions options)
        {
            Resource = resource ?? throw new ArgumentNullException(nameof(resource));
            LockId = lockId ?? throw new ArgumentNullException(nameof(lockId));
            _expiresAt = expiresAt;
            FencedToken = fencedToken;
            _options = options ?? throw new ArgumentNullException(nameof(options));
            AcquiredAt = DateTimeOffset.UtcNow;
            _renewalCts = new CancellationTokenSource();

            // Start auto-renewal if enabled
            if (_options.EnableAutoRenewal)
            {
                _renewalTask = StartAutoRenewalAsync(_renewalCts.Token);
            }
        }

        /// <summary>
        /// Gets the lock identifier.
        /// </summary>
        protected string LockId { get; }

        /// <inheritdoc />
        public string Resource { get; }

        /// <inheritdoc />
        public long? FencedToken { get; }

        /// <inheritdoc />
        public bool IsValid => !_disposed && !_released && DateTimeOffset.UtcNow < _expiresAt;

        /// <inheritdoc />
        public DateTimeOffset AcquiredAt { get; }

        /// <inheritdoc />
        public DateTimeOffset ExpiresAt => _expiresAt;

        /// <summary>
        /// Releases the lock using the provider-specific implementation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if the lock was released; otherwise, <c>false</c>.</returns>
        protected abstract Task<bool> ReleaseLockAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Renews the lock using the provider-specific implementation.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns><c>true</c> if the lock was renewed; otherwise, <c>false</c>.</returns>
        protected abstract Task<bool> RenewLockAsync(CancellationToken cancellationToken);

        /// <inheritdoc />
        public async Task<bool> RenewAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed || _released)
                return false;

            try
            {
                var renewed = await RenewLockAsync(cancellationToken);
                if (renewed)
                {
                    _expiresAt = DateTimeOffset.UtcNow.Add(_options.LockDuration);
                    return true;
                }
            }
            catch
            {
                // Renewal failed, lock may be invalid
            }

            return false;
        }

        /// <inheritdoc />
        public async Task<bool> ReleaseAsync(CancellationToken cancellationToken = default)
        {
            if (_released || _disposed)
                return false;

            _released = true;
            _renewalCts.Cancel();

            try
            {
                return await ReleaseLockAsync(cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Starts the automatic renewal task.
        /// </summary>
        private async Task StartAutoRenewalAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && !_disposed && !_released)
                {
                    await Task.Delay(_options.RenewalInterval, cancellationToken);

                    if (cancellationToken.IsCancellationRequested || _disposed || _released)
                        break;

                    // Renew if lock is still valid and not close to expiration
                    var timeUntilExpiry = _expiresAt - DateTimeOffset.UtcNow;
                    if (timeUntilExpiry <= _options.RenewalInterval)
                    {
                        var renewed = await RenewAsync(cancellationToken);
                        if (!renewed)
                        {
                            // Renewal failed, stop auto-renewal
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch
            {
                // Renewal failed, stop auto-renewal
            }
        }

        /// <summary>
        /// Disposes the lock handle, releasing the lock if still held.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _renewalCts.Cancel();
            _renewalCts.Dispose();

            // Release synchronously if possible
            try
            {
                ReleaseAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // Ignore errors during disposal
            }
        }

        /// <summary>
        /// Disposes the lock handle asynchronously, releasing the lock if still held.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            _renewalCts.Cancel();
            _renewalCts.Dispose();

            try
            {
                await ReleaseAsync();
            }
            catch
            {
                // Ignore errors during disposal
            }
        }
    }
}

