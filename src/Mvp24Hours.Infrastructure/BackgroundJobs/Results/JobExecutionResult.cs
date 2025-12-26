//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Results
{
    /// <summary>
    /// Represents the result of a job execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class encapsulates the outcome of a job execution, including success status,
    /// execution duration, error information, and whether the job will be retried.
    /// </para>
    /// <para>
    /// The result is immutable and provides factory methods for creating success and failure results.
    /// </para>
    /// </remarks>
    public class JobExecutionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobExecutionResult"/> class.
        /// </summary>
        /// <param name="status">The execution status.</param>
        /// <param name="duration">The execution duration.</param>
        /// <param name="errorMessage">The error message (if failed).</param>
        /// <param name="exception">The exception that occurred (if failed).</param>
        /// <param name="willRetry">Whether the job will be retried.</param>
        /// <param name="completedAt">When the job execution completed.</param>
        private JobExecutionResult(
            JobExecutionStatus status,
            TimeSpan? duration = null,
            string? errorMessage = null,
            Exception? exception = null,
            bool willRetry = false,
            DateTimeOffset? completedAt = null)
        {
            Status = status;
            Duration = duration;
            ErrorMessage = errorMessage;
            Exception = exception;
            WillRetry = willRetry;
            CompletedAt = completedAt ?? DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the execution status.
        /// </summary>
        public JobExecutionStatus Status { get; }

        /// <summary>
        /// Gets the execution duration.
        /// </summary>
        /// <remarks>
        /// This is the time taken to execute the job. It may be <c>null</c> if the
        /// execution was cancelled before completion or if duration tracking is not available.
        /// </remarks>
        public TimeSpan? Duration { get; }

        /// <summary>
        /// Gets the error message (if failed).
        /// </summary>
        /// <remarks>
        /// This contains a human-readable error message describing why the job failed.
        /// It is <c>null</c> if the job succeeded.
        /// </remarks>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets the exception that occurred during execution, if any; otherwise, <c>null</c>.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Gets whether the job will be retried.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This indicates whether the job scheduler will retry this job after failure.
        /// Retries are controlled by the retry policy configured for the job.
        /// </para>
        /// <para>
        /// If <c>true</c>, the job will be scheduled for retry according to the retry policy.
        /// If <c>false</c>, the job will not be retried (either because it succeeded, was cancelled,
        /// or exceeded the maximum retry attempts).
        /// </para>
        /// </remarks>
        public bool WillRetry { get; }

        /// <summary>
        /// Gets when the job execution completed (or was attempted).
        /// </summary>
        public DateTimeOffset CompletedAt { get; }

        /// <summary>
        /// Gets whether the job execution succeeded.
        /// </summary>
        public bool IsSuccess => Status == JobExecutionStatus.Success;

        /// <summary>
        /// Gets whether the job execution failed.
        /// </summary>
        public bool IsFailure => Status == JobExecutionStatus.Failed;

        /// <summary>
        /// Gets whether the job execution was cancelled.
        /// </summary>
        public bool IsCancelled => Status == JobExecutionStatus.Cancelled;

        /// <summary>
        /// Gets whether the job execution is retrying.
        /// </summary>
        public bool IsRetrying => Status == JobExecutionStatus.Retrying;

        /// <summary>
        /// Creates a successful job execution result.
        /// </summary>
        /// <param name="duration">The execution duration.</param>
        /// <param name="completedAt">When the job completed.</param>
        /// <returns>A result indicating successful job execution.</returns>
        public static JobExecutionResult Success(
            TimeSpan? duration = null,
            DateTimeOffset? completedAt = null)
        {
            return new JobExecutionResult(
                status: JobExecutionStatus.Success,
                duration: duration,
                completedAt: completedAt);
        }

        /// <summary>
        /// Creates a failed job execution result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the job failed.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="willRetry">Whether the job will be retried.</param>
        /// <param name="duration">The execution duration before failure.</param>
        /// <param name="completedAt">When the job execution failed.</param>
        /// <returns>A result indicating failed job execution.</returns>
        /// <exception cref="ArgumentException">Thrown when errorMessage is null or empty.</exception>
        public static JobExecutionResult Failed(
            string errorMessage,
            Exception? exception = null,
            bool willRetry = false,
            TimeSpan? duration = null,
            DateTimeOffset? completedAt = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            }

            return new JobExecutionResult(
                status: willRetry ? JobExecutionStatus.Retrying : JobExecutionStatus.Failed,
                duration: duration,
                errorMessage: errorMessage,
                exception: exception,
                willRetry: willRetry,
                completedAt: completedAt);
        }

        /// <summary>
        /// Creates a failed job execution result from an exception.
        /// </summary>
        /// <param name="exception">The exception that caused the failure.</param>
        /// <param name="willRetry">Whether the job will be retried.</param>
        /// <param name="duration">The execution duration before failure.</param>
        /// <param name="completedAt">When the job execution failed.</param>
        /// <returns>A result indicating failed job execution.</returns>
        /// <exception cref="ArgumentNullException">Thrown when exception is null.</exception>
        public static JobExecutionResult Failed(
            Exception exception,
            bool willRetry = false,
            TimeSpan? duration = null,
            DateTimeOffset? completedAt = null)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new JobExecutionResult(
                status: willRetry ? JobExecutionStatus.Retrying : JobExecutionStatus.Failed,
                duration: duration,
                errorMessage: exception.Message,
                exception: exception,
                willRetry: willRetry,
                completedAt: completedAt);
        }

        /// <summary>
        /// Creates a cancelled job execution result.
        /// </summary>
        /// <param name="duration">The execution duration before cancellation.</param>
        /// <param name="completedAt">When the job was cancelled.</param>
        /// <returns>A result indicating cancelled job execution.</returns>
        public static JobExecutionResult Cancelled(
            TimeSpan? duration = null,
            DateTimeOffset? completedAt = null)
        {
            return new JobExecutionResult(
                status: JobExecutionStatus.Cancelled,
                duration: duration,
                completedAt: completedAt);
        }

        /// <summary>
        /// Creates a retrying job execution result.
        /// </summary>
        /// <param name="errorMessage">The error message describing why the job failed.</param>
        /// <param name="exception">Optional exception that caused the failure.</param>
        /// <param name="duration">The execution duration before failure.</param>
        /// <param name="completedAt">When the job execution failed.</param>
        /// <returns>A result indicating the job will be retried.</returns>
        /// <exception cref="ArgumentException">Thrown when errorMessage is null or empty.</exception>
        public static JobExecutionResult Retrying(
            string errorMessage,
            Exception? exception = null,
            TimeSpan? duration = null,
            DateTimeOffset? completedAt = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or empty.", nameof(errorMessage));
            }

            return new JobExecutionResult(
                status: JobExecutionStatus.Retrying,
                duration: duration,
                errorMessage: errorMessage,
                exception: exception,
                willRetry: true,
                completedAt: completedAt);
        }
    }

    /// <summary>
    /// Represents the status of a job execution.
    /// </summary>
    public enum JobExecutionStatus
    {
        /// <summary>
        /// The job executed successfully.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The job execution failed.
        /// </summary>
        Failed = 1,

        /// <summary>
        /// The job execution was cancelled.
        /// </summary>
        Cancelled = 2,

        /// <summary>
        /// The job execution failed and will be retried.
        /// </summary>
        Retrying = 3
    }
}

