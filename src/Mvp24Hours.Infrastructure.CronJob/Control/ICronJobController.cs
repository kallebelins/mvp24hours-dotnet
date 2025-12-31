//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.CronJob.Resiliency;
using Mvp24Hours.Infrastructure.CronJob.State;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.CronJob.Control
{
    /// <summary>
    /// Interface for controlling CronJob execution at runtime.
    /// Provides pause/resume and status inspection capabilities.
    /// </summary>
    public interface ICronJobController
    {
        /// <summary>
        /// Pauses a CronJob, preventing further scheduled executions.
        /// </summary>
        /// <param name="jobName">The name of the job to pause.</param>
        /// <param name="reason">Optional reason for pausing.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the job was paused, false if not found.</returns>
        Task<bool> PauseAsync(string jobName, string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Pauses a CronJob by type.
        /// </summary>
        /// <typeparam name="T">The type of the job.</typeparam>
        /// <param name="reason">Optional reason for pausing.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the job was paused, false if not found.</returns>
        Task<bool> PauseAsync<T>(string? reason = null, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Resumes a paused CronJob.
        /// </summary>
        /// <param name="jobName">The name of the job to resume.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the job was resumed, false if not found.</returns>
        Task<bool> ResumeAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes a paused CronJob by type.
        /// </summary>
        /// <typeparam name="T">The type of the job.</typeparam>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the job was resumed, false if not found.</returns>
        Task<bool> ResumeAsync<T>(CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Checks if a CronJob is currently paused.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if paused, false otherwise.</returns>
        Task<bool> IsPausedAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a CronJob is currently paused by type.
        /// </summary>
        /// <typeparam name="T">The type of the job.</typeparam>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if paused, false otherwise.</returns>
        Task<bool> IsPausedAsync<T>(CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Gets the status of a CronJob.
        /// </summary>
        /// <param name="jobName">The name of the job.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The job status, or null if not found.</returns>
        Task<CronJobStatus?> GetStatusAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of all registered CronJobs.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of all job statuses.</returns>
        Task<IReadOnlyList<CronJobStatus>> GetAllStatusesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers immediate execution of a CronJob (outside of schedule).
        /// </summary>
        /// <param name="jobName">The name of the job to trigger.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the job was triggered, false if not found or already running.</returns>
        Task<bool> TriggerAsync(string jobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers immediate execution of a CronJob by type.
        /// </summary>
        /// <typeparam name="T">The type of the job.</typeparam>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>True if the job was triggered, false if not found or already running.</returns>
        Task<bool> TriggerAsync<T>(CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Pauses all registered CronJobs.
        /// </summary>
        /// <param name="reason">Optional reason for pausing.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PauseAllAsync(string? reason = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Resumes all paused CronJobs.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task ResumeAllAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the current status of a CronJob.
    /// </summary>
    public sealed class CronJobStatus
    {
        /// <summary>
        /// Gets the name of the job.
        /// </summary>
        public string JobName { get; init; } = string.Empty;

        /// <summary>
        /// Gets the CRON expression for the job.
        /// </summary>
        public string? CronExpression { get; init; }

        /// <summary>
        /// Gets the current state of the job.
        /// </summary>
        public CronJobExecutionState State { get; init; }

        /// <summary>
        /// Gets whether the job is currently paused.
        /// </summary>
        public bool IsPaused { get; init; }

        /// <summary>
        /// Gets the reason for pausing, if applicable.
        /// </summary>
        public string? PauseReason { get; init; }

        /// <summary>
        /// Gets when the job was paused.
        /// </summary>
        public DateTimeOffset? PausedAt { get; init; }

        /// <summary>
        /// Gets the last execution time.
        /// </summary>
        public DateTimeOffset? LastExecutionTime { get; init; }

        /// <summary>
        /// Gets the next scheduled execution time.
        /// </summary>
        public DateTimeOffset? NextExecutionTime { get; init; }

        /// <summary>
        /// Gets the total execution count.
        /// </summary>
        public long ExecutionCount { get; init; }

        /// <summary>
        /// Gets the success count.
        /// </summary>
        public long SuccessCount { get; init; }

        /// <summary>
        /// Gets the failure count.
        /// </summary>
        public long FailureCount { get; init; }

        /// <summary>
        /// Gets the last error message, if any.
        /// </summary>
        public string? LastError { get; init; }

        /// <summary>
        /// Gets the average execution duration in milliseconds.
        /// </summary>
        public double AverageDurationMs { get; init; }

        /// <summary>
        /// Gets the circuit breaker state.
        /// </summary>
        public CircuitBreakerState? CircuitBreakerState { get; init; }

        /// <summary>
        /// Creates a status from a job state.
        /// </summary>
        public static CronJobStatus FromState(CronJobState state, string? cronExpression = null, DateTimeOffset? nextExecution = null)
        {
            return new CronJobStatus
            {
                JobName = state.JobName,
                CronExpression = cronExpression,
                State = state.IsPaused ? CronJobExecutionState.Paused : CronJobExecutionState.Idle,
                IsPaused = state.IsPaused,
                PauseReason = state.PauseReason,
                PausedAt = state.PausedAt,
                LastExecutionTime = state.LastExecutionTime,
                NextExecutionTime = nextExecution,
                ExecutionCount = state.ExecutionCount,
                SuccessCount = state.SuccessCount,
                FailureCount = state.FailureCount,
                LastError = state.LastErrorMessage,
                AverageDurationMs = state.AverageDurationMs
            };
        }
    }

    /// <summary>
    /// Represents the execution state of a CronJob.
    /// </summary>
    public enum CronJobExecutionState
    {
        /// <summary>
        /// The job is idle, waiting for the next scheduled execution.
        /// </summary>
        Idle,

        /// <summary>
        /// The job is currently executing.
        /// </summary>
        Running,

        /// <summary>
        /// The job is paused and will not execute.
        /// </summary>
        Paused,

        /// <summary>
        /// The job is stopped.
        /// </summary>
        Stopped,

        /// <summary>
        /// The job execution was skipped (e.g., overlapping or circuit breaker).
        /// </summary>
        Skipped
    }
}

