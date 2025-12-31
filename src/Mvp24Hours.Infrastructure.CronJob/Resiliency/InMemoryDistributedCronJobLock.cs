//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Resiliency
{
    /// <summary>
    /// In-memory implementation of <see cref="IDistributedCronJobLock"/>.
    /// Only suitable for single-instance deployments or testing.
    /// </summary>
    /// <remarks>
    /// For multi-instance deployments, use a distributed implementation like
    /// Redis-based or SQL Server-based locking.
    /// </remarks>
    public sealed class InMemoryDistributedCronJobLock : IDistributedCronJobLock
    {
        private readonly ConcurrentDictionary<string, LockEntry> _locks = new();
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Creates a new instance of <see cref="InMemoryDistributedCronJobLock"/>.
        /// </summary>
        /// <param name="timeProvider">Optional time provider for testing.</param>
        public InMemoryDistributedCronJobLock(TimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc />
        public Task<IDistributedCronJobLockHandle?> TryAcquireAsync(
            string jobName,
            string instanceId,
            TimeSpan lockDuration,
            CancellationToken cancellationToken = default)
        {
            var now = _timeProvider.GetUtcNow();

            // Try to add or update if expired
            var entry = _locks.AddOrUpdate(
                jobName,
                _ => new LockEntry
                {
                    JobName = jobName,
                    InstanceId = instanceId,
                    AcquiredAt = now,
                    ExpiresAt = now + lockDuration
                },
                (_, existing) =>
                {
                    // If expired or same instance, allow acquisition
                    if (existing.ExpiresAt <= now || existing.InstanceId == instanceId)
                    {
                        return new LockEntry
                        {
                            JobName = jobName,
                            InstanceId = instanceId,
                            AcquiredAt = now,
                            ExpiresAt = now + lockDuration
                        };
                    }
                    // Otherwise, return existing (don't overwrite)
                    return existing;
                });

            // Check if we got the lock
            if (entry.InstanceId == instanceId && entry.AcquiredAt == now)
            {
                var handle = new InMemoryDistributedCronJobLockHandle(
                    this,
                    entry,
                    _timeProvider);

                return Task.FromResult<IDistributedCronJobLockHandle?>(handle);
            }

            return Task.FromResult<IDistributedCronJobLockHandle?>(null);
        }

        /// <inheritdoc />
        public Task<bool> IsLockedAsync(string jobName, CancellationToken cancellationToken = default)
        {
            if (_locks.TryGetValue(jobName, out var entry))
            {
                var now = _timeProvider.GetUtcNow();
                return Task.FromResult(entry.ExpiresAt > now);
            }
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task<DistributedLockInfo?> GetLockInfoAsync(string jobName, CancellationToken cancellationToken = default)
        {
            if (_locks.TryGetValue(jobName, out var entry))
            {
                var now = _timeProvider.GetUtcNow();
                if (entry.ExpiresAt > now)
                {
                    return Task.FromResult<DistributedLockInfo?>(new DistributedLockInfo
                    {
                        JobName = entry.JobName,
                        InstanceId = entry.InstanceId,
                        AcquiredAt = entry.AcquiredAt,
                        ExpiresAt = entry.ExpiresAt,
                        MachineName = Environment.MachineName
                    });
                }
            }
            return Task.FromResult<DistributedLockInfo?>(null);
        }

        internal bool TryExtend(string jobName, string instanceId, TimeSpan extension)
        {
            if (_locks.TryGetValue(jobName, out var entry))
            {
                if (entry.InstanceId == instanceId)
                {
                    var now = _timeProvider.GetUtcNow();
                    if (entry.ExpiresAt > now)
                    {
                        entry.ExpiresAt = now + extension;
                        return true;
                    }
                }
            }
            return false;
        }

        internal bool TryRelease(string jobName, string instanceId)
        {
            if (_locks.TryGetValue(jobName, out var entry))
            {
                if (entry.InstanceId == instanceId)
                {
                    return _locks.TryRemove(jobName, out _);
                }
            }
            return false;
        }

        /// <summary>
        /// Clears all locks. For testing purposes only.
        /// </summary>
        public void Clear()
        {
            _locks.Clear();
        }

        private sealed class LockEntry
        {
            public string JobName { get; init; } = string.Empty;
            public string InstanceId { get; init; } = string.Empty;
            public DateTimeOffset AcquiredAt { get; init; }
            public DateTimeOffset ExpiresAt { get; set; }
        }

        private sealed class InMemoryDistributedCronJobLockHandle : IDistributedCronJobLockHandle
        {
            private readonly InMemoryDistributedCronJobLock _parent;
            private readonly LockEntry _entry;
            private readonly TimeProvider _timeProvider;
            private bool _released;

            public InMemoryDistributedCronJobLockHandle(
                InMemoryDistributedCronJobLock parent,
                LockEntry entry,
                TimeProvider timeProvider)
            {
                _parent = parent;
                _entry = entry;
                _timeProvider = timeProvider;
            }

            public string JobName => _entry.JobName;
            public string InstanceId => _entry.InstanceId;
            public DateTimeOffset AcquiredAt => _entry.AcquiredAt;
            public DateTimeOffset ExpiresAt => _entry.ExpiresAt;

            public bool IsValid => !_released && _entry.ExpiresAt > _timeProvider.GetUtcNow();

            public Task<bool> ExtendAsync(TimeSpan extension, CancellationToken cancellationToken = default)
            {
                if (_released) return Task.FromResult(false);
                return Task.FromResult(_parent.TryExtend(_entry.JobName, _entry.InstanceId, extension));
            }

            public Task ReleaseAsync(CancellationToken cancellationToken = default)
            {
                if (!_released)
                {
                    _parent.TryRelease(_entry.JobName, _entry.InstanceId);
                    _released = true;
                }
                return Task.CompletedTask;
            }

            public async ValueTask DisposeAsync()
            {
                await ReleaseAsync();
            }
        }
    }
}

