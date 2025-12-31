//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.State
{
    /// <summary>
    /// In-memory implementation of <see cref="ICronJobStateStore"/>.
    /// State is lost on application restart.
    /// </summary>
    /// <remarks>
    /// Use this implementation for single-instance deployments or development.
    /// For production distributed deployments, use a persistent implementation
    /// (e.g., Redis, SQL Server, MongoDB).
    /// </remarks>
    public sealed class InMemoryCronJobStateStore : ICronJobStateStore
    {
        private readonly ConcurrentDictionary<string, CronJobState> _states = new();

        /// <inheritdoc />
        public Task<CronJobState?> GetStateAsync(string jobName, CancellationToken cancellationToken = default)
        {
            _states.TryGetValue(jobName, out var state);
            return Task.FromResult(state);
        }

        /// <inheritdoc />
        public Task SaveStateAsync(CronJobState state, CancellationToken cancellationToken = default)
        {
            _states[state.JobName] = state;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeleteStateAsync(string jobName, CancellationToken cancellationToken = default)
        {
            _states.TryRemove(jobName, out _);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IReadOnlyList<CronJobState>> GetAllStatesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<CronJobState>>(_states.Values.ToList());
        }

        /// <inheritdoc />
        public Task<bool> IsPausedAsync(string jobName, CancellationToken cancellationToken = default)
        {
            if (_states.TryGetValue(jobName, out var state))
            {
                return Task.FromResult(state.IsPaused);
            }
            return Task.FromResult(false);
        }

        /// <inheritdoc />
        public Task SetPausedAsync(string jobName, bool isPaused, CancellationToken cancellationToken = default)
        {
            _states.AddOrUpdate(
                jobName,
                _ =>
                {
                    var newState = new CronJobState(jobName) { IsPaused = isPaused };
                    if (isPaused)
                    {
                        newState.PausedAt = System.DateTimeOffset.UtcNow;
                    }
                    return newState;
                },
                (_, existing) =>
                {
                    existing.IsPaused = isPaused;
                    if (isPaused)
                    {
                        existing.PausedAt = System.DateTimeOffset.UtcNow;
                    }
                    else
                    {
                        existing.PausedAt = null;
                        existing.PauseReason = null;
                    }
                    return existing;
                });

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets or creates a state for a job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <returns>The job state.</returns>
        public CronJobState GetOrCreate(string jobName)
        {
            return _states.GetOrAdd(jobName, name => new CronJobState(name));
        }

        /// <summary>
        /// Clears all stored states.
        /// </summary>
        public void Clear()
        {
            _states.Clear();
        }
    }
}

