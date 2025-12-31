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
    /// In-memory implementation of <see cref="ICronJobExecutionLock"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation uses in-memory semaphores to prevent overlapping executions.
    /// It is suitable for single-instance deployments but not for distributed scenarios.
    /// </para>
    /// <para>
    /// For distributed scenarios (multiple instances of the same application),
    /// use a distributed lock implementation (Redis, SQL, etc.).
    /// </para>
    /// </remarks>
    public class InMemoryCronJobExecutionLock : ICronJobExecutionLock
    {
        private readonly ConcurrentDictionary<string, LockState> _locks = new();
        private readonly TimeProvider _timeProvider;

        /// <summary>
        /// Creates a new instance of <see cref="InMemoryCronJobExecutionLock"/>.
        /// </summary>
        /// <param name="timeProvider">
        /// Optional time provider for testability. Defaults to <see cref="TimeProvider.System"/>.
        /// </param>
        public InMemoryCronJobExecutionLock(TimeProvider? timeProvider = null)
        {
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        /// <inheritdoc />
        public async Task<ICronJobLockHandle?> TryAcquireAsync(
            string jobName,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobName, nameof(jobName));

            var lockState = _locks.GetOrAdd(jobName, _ => new LockState());

            bool acquired;
            if (timeout == TimeSpan.Zero)
            {
                // Immediate check without waiting
                acquired = await lockState.Semaphore.WaitAsync(0, cancellationToken);
            }
            else
            {
                // Wait with timeout
                acquired = await lockState.Semaphore.WaitAsync(timeout, cancellationToken);
            }

            if (!acquired)
            {
                return null;
            }

            var now = _timeProvider.GetUtcNow();
            lockState.AcquiredAt = now;
            lockState.IsLocked = true;

            return new InMemoryLockHandle(this, jobName, now);
        }

        /// <inheritdoc />
        public bool IsLocked(string jobName)
        {
            if (_locks.TryGetValue(jobName, out var state))
            {
                return state.IsLocked;
            }
            return false;
        }

        /// <inheritdoc />
        public DateTimeOffset? GetLockAcquiredTime(string jobName)
        {
            if (_locks.TryGetValue(jobName, out var state) && state.IsLocked)
            {
                return state.AcquiredAt;
            }
            return null;
        }

        /// <summary>
        /// Releases the lock for the specified job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        internal void ReleaseLock(string jobName)
        {
            if (_locks.TryGetValue(jobName, out var state))
            {
                state.IsLocked = false;
                state.AcquiredAt = null;
                state.Semaphore.Release();
            }
        }

        #region Nested Types

        private sealed class LockState
        {
            public SemaphoreSlim Semaphore { get; } = new(1, 1);
            public bool IsLocked { get; set; }
            public DateTimeOffset? AcquiredAt { get; set; }
        }

        private sealed class InMemoryLockHandle : ICronJobLockHandle
        {
            private readonly InMemoryCronJobExecutionLock _parent;
            private int _released;

            public InMemoryLockHandle(
                InMemoryCronJobExecutionLock parent,
                string jobName,
                DateTimeOffset acquiredAt)
            {
                _parent = parent;
                JobName = jobName;
                AcquiredAt = acquiredAt;
            }

            public string JobName { get; }
            public DateTimeOffset AcquiredAt { get; }
            public bool IsValid => Interlocked.CompareExchange(ref _released, 0, 0) == 0;

            public Task ReleaseAsync()
            {
                if (Interlocked.Exchange(ref _released, 1) == 0)
                {
                    _parent.ReleaseLock(JobName);
                }
                return Task.CompletedTask;
            }

            public async ValueTask DisposeAsync()
            {
                await ReleaseAsync();
            }
        }

        #endregion
    }
}

