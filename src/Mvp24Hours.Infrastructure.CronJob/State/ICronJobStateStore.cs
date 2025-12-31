//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.State
{
    /// <summary>
    /// Interface for persisting and retrieving CronJob state.
    /// Enables state persistence across restarts and distributed scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The state store can be used to:
    /// </para>
    /// <list type="bullet">
    /// <item>Persist execution history and statistics</item>
    /// <item>Store job pause/resume status</item>
    /// <item>Track last execution time for resume after restart</item>
    /// <item>Store custom job state data</item>
    /// </list>
    /// </remarks>
    public interface ICronJobStateStore
    {
        /// <summary>
        /// Gets the state for a specific job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The job state, or null if not found.</returns>
        Task<CronJobState?> GetStateAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the state for a specific job.
        /// </summary>
        /// <param name="state">The state to save.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SaveStateAsync(CronJobState state, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes the state for a specific job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DeleteStateAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all job states.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>All stored job states.</returns>
        Task<IReadOnlyList<CronJobState>> GetAllStatesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the pause status for a job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the job is paused, false otherwise.</returns>
        Task<bool> IsPausedAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the pause status for a job.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="isPaused">Whether the job should be paused.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetPausedAsync(string jobName, bool isPaused, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the persisted state of a CronJob.
    /// </summary>
    public sealed class CronJobState
    {
        /// <summary>
        /// Creates a new instance of <see cref="CronJobState"/>.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        public CronJobState(string jobName)
        {
            JobName = jobName ?? throw new ArgumentNullException(nameof(jobName));
            Properties = new Dictionary<string, object?>();
        }

        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        public string JobName { get; }

        /// <summary>
        /// Gets or sets the total number of executions.
        /// </summary>
        public long ExecutionCount { get; set; }

        /// <summary>
        /// Gets or sets the number of successful executions.
        /// </summary>
        public long SuccessCount { get; set; }

        /// <summary>
        /// Gets or sets the number of failed executions.
        /// </summary>
        public long FailureCount { get; set; }

        /// <summary>
        /// Gets or sets the total number of retries.
        /// </summary>
        public long RetryCount { get; set; }

        /// <summary>
        /// Gets or sets the number of skipped executions.
        /// </summary>
        public long SkippedCount { get; set; }

        /// <summary>
        /// Gets or sets the last execution time.
        /// </summary>
        public DateTimeOffset? LastExecutionTime { get; set; }

        /// <summary>
        /// Gets or sets the last successful execution time.
        /// </summary>
        public DateTimeOffset? LastSuccessTime { get; set; }

        /// <summary>
        /// Gets or sets the last failure time.
        /// </summary>
        public DateTimeOffset? LastFailureTime { get; set; }

        /// <summary>
        /// Gets or sets the last error message.
        /// </summary>
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the average execution duration in milliseconds.
        /// </summary>
        public double AverageDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the minimum execution duration in milliseconds.
        /// </summary>
        public double MinDurationMs { get; set; }

        /// <summary>
        /// Gets or sets the maximum execution duration in milliseconds.
        /// </summary>
        public double MaxDurationMs { get; set; }

        /// <summary>
        /// Gets or sets whether the job is paused.
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Gets or sets when the job was paused.
        /// </summary>
        public DateTimeOffset? PausedAt { get; set; }

        /// <summary>
        /// Gets or sets the reason for pausing.
        /// </summary>
        public string? PauseReason { get; set; }

        /// <summary>
        /// Gets or sets when the state was last updated.
        /// </summary>
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Gets or sets custom properties.
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; }

        /// <summary>
        /// Updates the state after a successful execution.
        /// </summary>
        /// <param name="duration">The duration of the execution.</param>
        public void RecordSuccess(TimeSpan duration)
        {
            ExecutionCount++;
            SuccessCount++;
            LastExecutionTime = DateTimeOffset.UtcNow;
            LastSuccessTime = DateTimeOffset.UtcNow;
            UpdateDurationStats(duration);
            LastUpdated = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Updates the state after a failed execution.
        /// </summary>
        /// <param name="duration">The duration of the execution.</param>
        /// <param name="error">The error message.</param>
        public void RecordFailure(TimeSpan duration, string? error)
        {
            ExecutionCount++;
            FailureCount++;
            LastExecutionTime = DateTimeOffset.UtcNow;
            LastFailureTime = DateTimeOffset.UtcNow;
            LastErrorMessage = error;
            UpdateDurationStats(duration);
            LastUpdated = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Updates the state after a retry.
        /// </summary>
        public void RecordRetry()
        {
            RetryCount++;
            LastUpdated = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Updates the state after a skipped execution.
        /// </summary>
        public void RecordSkipped()
        {
            SkippedCount++;
            LastUpdated = DateTimeOffset.UtcNow;
        }

        private void UpdateDurationStats(TimeSpan duration)
        {
            var durationMs = duration.TotalMilliseconds;

            if (ExecutionCount == 1)
            {
                AverageDurationMs = durationMs;
                MinDurationMs = durationMs;
                MaxDurationMs = durationMs;
            }
            else
            {
                // Running average
                AverageDurationMs = ((AverageDurationMs * (ExecutionCount - 1)) + durationMs) / ExecutionCount;
                MinDurationMs = Math.Min(MinDurationMs, durationMs);
                MaxDurationMs = Math.Max(MaxDurationMs, durationMs);
            }
        }
    }
}

