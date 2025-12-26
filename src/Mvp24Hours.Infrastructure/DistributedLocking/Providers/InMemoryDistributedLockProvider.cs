//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.DistributedLocking.Contract;
using Mvp24Hours.Infrastructure.DistributedLocking.Metrics;
using Mvp24Hours.Infrastructure.DistributedLocking.Options;
using Mvp24Hours.Infrastructure.DistributedLocking.Results;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.DistributedLocking.Providers
{
    /// <summary>
    /// In-memory distributed lock provider for testing and single-instance scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This provider uses an in-memory dictionary to manage locks. It is suitable for:
    /// <list type="bullet">
    /// <item>Unit testing and integration testing</item>
    /// <item>Single-instance applications</item>
    /// <item>Development environments</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Limitations:</strong>
    /// <list type="bullet">
    /// <item>Not suitable for distributed scenarios (multiple application instances)</item>
    /// <item>Locks are lost on application restart</item>
    /// <item>No persistence across process boundaries</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class InMemoryDistributedLockProvider : BaseDistributedLockProvider
    {
        private static readonly ConcurrentDictionary<string, LockEntry> _locks = new();
        private readonly ILogger<InMemoryDistributedLockProvider>? _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDistributedLockProvider"/> class.
        /// </summary>
        /// <param name="logger">Optional logger.</param>
        /// <param name="metrics">Optional metrics collector.</param>
        public InMemoryDistributedLockProvider(
            ILogger<InMemoryDistributedLockProvider>? logger = null,
            DistributedLockMetrics? metrics = null)
            : base(metrics)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        protected override string ProviderName => "InMemory";

        /// <inheritdoc />
        protected override Task<(bool Success, string LockId, DateTimeOffset ExpiresAt, long? FencedToken)> TryAcquireLockCoreAsync(
            string resource,
            string lockId,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            var expiresAt = DateTimeOffset.UtcNow.Add(duration);
            var key = GetLockKey(resource);

            // Try to acquire lock atomically
            var entry = _locks.AddOrUpdate(
                key,
                _ => new LockEntry
                {
                    LockId = lockId,
                    ExpiresAt = expiresAt,
                    AcquiredAt = DateTimeOffset.UtcNow
                },
                (_, existing) =>
                {
                    // Check if existing lock has expired
                    if (DateTimeOffset.UtcNow >= existing.ExpiresAt)
                    {
                        // Lock expired, acquire it
                        return new LockEntry
                        {
                            LockId = lockId,
                            ExpiresAt = expiresAt,
                            AcquiredAt = DateTimeOffset.UtcNow
                        };
                    }

                    // Lock is still held
                    return existing;
                });

            var success = entry.LockId == lockId && DateTimeOffset.UtcNow < entry.ExpiresAt;

            if (success)
            {
                _logger?.LogDebug(
                    "Lock acquired: Resource={Resource}, LockId={LockId}, ExpiresAt={ExpiresAt}",
                    resource, lockId, expiresAt);
            }
            else
            {
                _logger?.LogDebug(
                    "Lock acquisition failed: Resource={Resource}, LockId={LockId}, ExistingLockId={ExistingLockId}",
                    resource, lockId, entry.LockId);
            }

            return Task.FromResult((success, lockId, expiresAt, (long?)null));
        }

        /// <inheritdoc />
        protected override Task<bool> ReleaseLockCoreAsync(
            string resource,
            string lockId,
            CancellationToken cancellationToken)
        {
            var key = GetLockKey(resource);

            if (_locks.TryGetValue(key, out var entry) && entry.LockId == lockId)
            {
                var removed = _locks.TryRemove(key, out _);
                if (removed)
                {
                    RecordRelease(resource);
                    _logger?.LogDebug(
                        "Lock released: Resource={Resource}, LockId={LockId}",
                        resource, lockId);
                }
                return Task.FromResult(removed);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        protected override Task<bool> RenewLockCoreAsync(
            string resource,
            string lockId,
            TimeSpan duration,
            CancellationToken cancellationToken)
        {
            var key = GetLockKey(resource);

            if (_locks.TryGetValue(key, out var entry) && entry.LockId == lockId)
            {
                var newExpiresAt = DateTimeOffset.UtcNow.Add(duration);
                entry.ExpiresAt = newExpiresAt;

                _logger?.LogDebug(
                    "Lock renewed: Resource={Resource}, LockId={LockId}, NewExpiresAt={NewExpiresAt}",
                    resource, lockId, newExpiresAt);

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        protected override Task<bool> IsLockedCoreAsync(
            string resource,
            CancellationToken cancellationToken)
        {
            var key = GetLockKey(resource);

            if (_locks.TryGetValue(key, out var entry))
            {
                // Check if lock has expired
                if (DateTimeOffset.UtcNow >= entry.ExpiresAt)
                {
                    // Lock expired, remove it
                    _locks.TryRemove(key, out _);
                    return Task.FromResult(false);
                }

                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        /// <inheritdoc />
        protected override ILockHandle CreateLockHandle(
            string resource,
            string lockId,
            DateTimeOffset expiresAt,
            long? fencedToken,
            DistributedLockOptions options)
        {
            return new InMemoryLockHandle(this, resource, lockId, expiresAt, fencedToken, options);
        }

        private static string GetLockKey(string resource)
        {
            return $"lock:{resource}";
        }

        private class LockEntry
        {
            public string LockId { get; set; } = string.Empty;
            public DateTimeOffset ExpiresAt { get; set; }
            public DateTimeOffset AcquiredAt { get; set; }
        }

        private class InMemoryLockHandle : LockHandleBase
        {
            private readonly InMemoryDistributedLockProvider _provider;

            public InMemoryLockHandle(
                InMemoryDistributedLockProvider provider,
                string resource,
                string lockId,
                DateTimeOffset expiresAt,
                long? fencedToken,
                DistributedLockOptions options)
                : base(resource, lockId, expiresAt, fencedToken, options)
            {
                _provider = provider;
            }

            protected override Task<bool> ReleaseLockAsync(CancellationToken cancellationToken)
            {
                return _provider.ReleaseLockCoreAsync(Resource, LockId, cancellationToken);
            }

            protected override Task<bool> RenewLockAsync(CancellationToken cancellationToken)
            {
                return _provider.RenewLockCoreAsync(Resource, LockId, _options.LockDuration, cancellationToken);
            }
        }
    }
}

