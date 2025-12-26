//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Contract
{
    /// <summary>
    /// Unified interface for scheduling and managing background jobs across different providers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides a consistent API for scheduling background jobs regardless of the
    /// underlying job provider (Hangfire, Quartz.NET, in-memory, etc.). All operations are asynchronous
    /// and support cancellation tokens for proper resource management.
    /// </para>
    /// <para>
    /// <strong>Job Types:</strong>
    /// The scheduler supports different types of jobs:
    /// - Fire-and-forget: Execute once immediately
    /// - Delayed: Execute after a specified delay
    /// - Recurring: Execute on a schedule (CRON expression)
    /// - Continuations: Execute after another job completes
    /// - Batches: Group of jobs executed together
    /// </para>
    /// <para>
    /// <strong>Job Arguments:</strong>
    /// Jobs can accept strongly-typed arguments. Arguments are serialized when scheduled and
    /// deserialized when executed. Arguments should be serializable (typically using JSON).
    /// </para>
    /// <para>
    /// <strong>Job Options:</strong>
    /// Each job can be configured with options such as retry policy, timeout, priority, queue,
    /// and metadata. Options can override default settings configured for the scheduler.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Schedule a fire-and-forget job
    /// var jobId = await jobScheduler.EnqueueAsync&lt;SendEmailJob, SendEmailArgs&gt;(
    ///     new SendEmailArgs { To = "user@example.com", Subject = "Hello" });
    /// 
    /// // Schedule a delayed job
    /// var delayedJobId = await jobScheduler.ScheduleAsync&lt;SendEmailJob, SendEmailArgs&gt;(
    ///     new SendEmailArgs { To = "user@example.com", Subject = "Reminder" },
    ///     TimeSpan.FromHours(24));
    /// 
    /// // Schedule a recurring job
    /// var recurringJobId = await jobScheduler.ScheduleRecurringAsync&lt;CleanupJob&gt;(
    ///     "0 0 * * *", // Daily at midnight
    ///     new JobOptions { Queue = "maintenance" });
    /// </code>
    /// </example>
    public interface IJobScheduler
    {
        /// <summary>
        /// Enqueues a job for immediate execution (fire-and-forget).
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <typeparam name="TArgs">The job arguments type.</typeparam>
        /// <param name="args">The job arguments.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The job ID assigned to the scheduled job.</returns>
        /// <remarks>
        /// <para>
        /// This method schedules a job for immediate execution. The job will be executed as soon
        /// as a worker is available. The job ID can be used to track, cancel, or query the job status.
        /// </para>
        /// <para>
        /// <strong>Job Arguments:</strong>
        /// The arguments are serialized when the job is scheduled. Ensure the arguments type is
        /// serializable (typically using JSON serialization).
        /// </para>
        /// <para>
        /// <strong>Job Options:</strong>
        /// Options can override default settings configured for the scheduler. If not specified,
        /// default options are used.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when args is null.</exception>
        Task<string> EnqueueAsync<TJob, TArgs>(
            TArgs args,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class;

        /// <summary>
        /// Enqueues a job without arguments for immediate execution (fire-and-forget).
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The job ID assigned to the scheduled job.</returns>
        Task<string> EnqueueAsync<TJob>(
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob;

        /// <summary>
        /// Schedules a job for execution after a specified delay.
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <typeparam name="TArgs">The job arguments type.</typeparam>
        /// <param name="args">The job arguments.</param>
        /// <param name="delay">The delay before execution.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The job ID assigned to the scheduled job.</returns>
        /// <remarks>
        /// <para>
        /// This method schedules a job for execution after the specified delay. The delay is
        /// calculated from the current time. The job will be executed once after the delay expires.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when args is null.</exception>
        /// <exception cref="ArgumentException">Thrown when delay is negative or zero.</exception>
        Task<string> ScheduleAsync<TJob, TArgs>(
            TArgs args,
            TimeSpan delay,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class;

        /// <summary>
        /// Schedules a job without arguments for execution after a specified delay.
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <param name="delay">The delay before execution.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The job ID assigned to the scheduled job.</returns>
        /// <exception cref="ArgumentException">Thrown when delay is negative or zero.</exception>
        Task<string> ScheduleAsync<TJob>(
            TimeSpan delay,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob;

        /// <summary>
        /// Schedules a job for execution at a specific date and time.
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <typeparam name="TArgs">The job arguments type.</typeparam>
        /// <param name="args">The job arguments.</param>
        /// <param name="scheduledTime">The date and time when the job should be executed.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The job ID assigned to the scheduled job.</returns>
        /// <remarks>
        /// <para>
        /// This method schedules a job for execution at a specific date and time. The scheduled
        /// time should be in the future. The job will be executed once at the specified time.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when args is null.</exception>
        /// <exception cref="ArgumentException">Thrown when scheduledTime is in the past.</exception>
        Task<string> ScheduleAsync<TJob, TArgs>(
            TArgs args,
            DateTimeOffset scheduledTime,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class;

        /// <summary>
        /// Schedules a job without arguments for execution at a specific date and time.
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <param name="scheduledTime">The date and time when the job should be executed.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The job ID assigned to the scheduled job.</returns>
        /// <exception cref="ArgumentException">Thrown when scheduledTime is in the past.</exception>
        Task<string> ScheduleAsync<TJob>(
            DateTimeOffset scheduledTime,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob;

        /// <summary>
        /// Schedules a recurring job using a CRON expression.
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <typeparam name="TArgs">The job arguments type.</typeparam>
        /// <param name="cronExpression">The CRON expression defining the schedule.</param>
        /// <param name="args">The job arguments.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The recurring job ID assigned to the scheduled job.</returns>
        /// <remarks>
        /// <para>
        /// This method schedules a recurring job that executes according to a CRON expression.
        /// The job will be executed repeatedly according to the schedule until cancelled.
        /// </para>
        /// <para>
        /// <strong>CRON Expression Format:</strong>
        /// Standard CRON format: "minute hour day month day-of-week"
        /// Example: "0 0 * * *" (daily at midnight)
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when cronExpression or args is null.</exception>
        /// <exception cref="ArgumentException">Thrown when cronExpression is invalid.</exception>
        Task<string> ScheduleRecurringAsync<TJob, TArgs>(
            string cronExpression,
            TArgs args,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class;

        /// <summary>
        /// Schedules a recurring job without arguments using a CRON expression.
        /// </summary>
        /// <typeparam name="TJob">The job type.</typeparam>
        /// <param name="cronExpression">The CRON expression defining the schedule.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The recurring job ID assigned to the scheduled job.</returns>
        /// <exception cref="ArgumentNullException">Thrown when cronExpression is null.</exception>
        /// <exception cref="ArgumentException">Thrown when cronExpression is invalid.</exception>
        Task<string> ScheduleRecurringAsync<TJob>(
            string cronExpression,
            JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob;

        /// <summary>
        /// Schedules a continuation job that executes after a parent job completes.
        /// </summary>
        /// <typeparam name="TJob">The continuation job type.</typeparam>
        /// <typeparam name="TArgs">The continuation job arguments type.</typeparam>
        /// <param name="parentJobId">The ID of the parent job that must complete first.</param>
        /// <param name="args">The continuation job arguments.</param>
        /// <param name="continuationOptions">Options that control when the continuation executes.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The continuation job ID assigned to the scheduled job.</returns>
        /// <remarks>
        /// <para>
        /// This method schedules a continuation job that executes after the parent job completes.
        /// The continuation can be configured to execute only on success, only on failure, or in both cases.
        /// </para>
        /// <para>
        /// The continuation job will wait for the parent job to complete before executing. If the
        /// parent job doesn't complete within the maximum wait time (specified in continuation options),
        /// the continuation will be cancelled.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when parentJobId or args is null.</exception>
        /// <exception cref="ArgumentException">Thrown when parentJobId is empty.</exception>
        Task<string> ContinueWithAsync<TJob, TArgs>(
            string parentJobId,
            TArgs args,
            Contract.ContinuationOptions? continuationOptions = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class;

        /// <summary>
        /// Schedules a continuation job without arguments that executes after a parent job completes.
        /// </summary>
        /// <typeparam name="TJob">The continuation job type.</typeparam>
        /// <param name="parentJobId">The ID of the parent job that must complete first.</param>
        /// <param name="continuationOptions">Options that control when the continuation executes.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The continuation job ID assigned to the scheduled job.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parentJobId is null.</exception>
        /// <exception cref="ArgumentException">Thrown when parentJobId is empty.</exception>
        Task<string> ContinueWithAsync<TJob>(
            string parentJobId,
            Contract.ContinuationOptions? continuationOptions = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob;

        /// <summary>
        /// Schedules a batch of jobs for execution.
        /// </summary>
        /// <param name="batch">The batch of jobs to schedule.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The batch ID assigned to the scheduled batch.</returns>
        /// <remarks>
        /// <para>
        /// This method schedules a batch of jobs that will be executed together as a group.
        /// The batch provides features such as atomic execution, batch-level retry policies,
        /// and progress tracking.
        /// </para>
        /// <para>
        /// Jobs in the batch can execute sequentially, in parallel, or based on dependencies,
        /// depending on the batch options.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when batch is null.</exception>
        Task<string> ScheduleBatchAsync(
            Contract.IJobBatch batch,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of a job batch.
        /// </summary>
        /// <param name="batchId">The batch ID to query.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The batch status, or <c>null</c> if the batch was not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when batchId is null or empty.</exception>
        Task<Contract.BatchStatus?> GetBatchStatusAsync(
            string batchId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a job batch and all its jobs.
        /// </summary>
        /// <param name="batchId">The batch ID to cancel.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the batch was cancelled; <c>false</c> if the batch was not found or already completed.</returns>
        /// <exception cref="ArgumentNullException">Thrown when batchId is null or empty.</exception>
        Task<bool> CancelBatchAsync(
            string batchId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Enqueues a child job that belongs to a parent job.
        /// </summary>
        /// <typeparam name="TJob">The child job type.</typeparam>
        /// <typeparam name="TArgs">The child job arguments type.</typeparam>
        /// <param name="parentJobId">The ID of the parent job.</param>
        /// <param name="args">The child job arguments.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The child job ID assigned to the scheduled job.</returns>
        /// <remarks>
        /// <para>
        /// This method schedules a child job that belongs to a parent job. Child jobs can execute
        /// independently but can be controlled by the parent (cancelled, waited for, etc.).
        /// </para>
        /// <para>
        /// Child jobs can have dependencies on other child jobs (siblings) and can report progress
        /// back to the parent job.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when parentJobId or args is null.</exception>
        /// <exception cref="ArgumentException">Thrown when parentJobId is empty.</exception>
        Task<string> EnqueueChildAsync<TJob, TArgs>(
            string parentJobId,
            TArgs args,
            Options.JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob<TArgs>
            where TArgs : class;

        /// <summary>
        /// Enqueues a child job without arguments that belongs to a parent job.
        /// </summary>
        /// <typeparam name="TJob">The child job type.</typeparam>
        /// <param name="parentJobId">The ID of the parent job.</param>
        /// <param name="options">Optional job options (retry, timeout, priority, queue, metadata).</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The child job ID assigned to the scheduled job.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parentJobId is null.</exception>
        /// <exception cref="ArgumentException">Thrown when parentJobId is empty.</exception>
        Task<string> EnqueueChildAsync<TJob>(
            string parentJobId,
            Options.JobOptions? options = null,
            CancellationToken cancellationToken = default)
            where TJob : class, IBackgroundJob;

        /// <summary>
        /// Waits for all child jobs of a parent job to complete.
        /// </summary>
        /// <param name="parentJobId">The ID of the parent job.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A task that completes when all child jobs have finished (successfully or failed).</returns>
        /// <remarks>
        /// <para>
        /// This method blocks until all child jobs of the specified parent job have completed.
        /// It can be used by parent jobs to wait for their children before finishing.
        /// </para>
        /// <para>
        /// The method will return when all children have completed, regardless of whether they
        /// succeeded or failed. Use <see cref="GetChildJobStatusesAsync"/> to check individual
        /// child job outcomes.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when parentJobId is null or empty.</exception>
        Task WaitForChildrenAsync(
            string parentJobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the statuses of all child jobs for a parent job.
        /// </summary>
        /// <param name="parentJobId">The ID of the parent job.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>A dictionary mapping child job IDs to their statuses.</returns>
        /// <exception cref="ArgumentNullException">Thrown when parentJobId is null or empty.</exception>
        Task<System.Collections.Generic.IReadOnlyDictionary<string, JobStatus?>> GetChildJobStatusesAsync(
            string parentJobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels all child jobs of a parent job.
        /// </summary>
        /// <param name="parentJobId">The ID of the parent job.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The number of child jobs that were cancelled.</returns>
        /// <remarks>
        /// <para>
        /// This method cancels all pending and running child jobs of the specified parent job.
        /// Completed child jobs are not affected.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when parentJobId is null or empty.</exception>
        Task<int> CancelChildrenAsync(
            string parentJobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels a scheduled job.
        /// </summary>
        /// <param name="jobId">The job ID to cancel.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns><c>true</c> if the job was cancelled; <c>false</c> if the job was not found or already completed.</returns>
        /// <remarks>
        /// <para>
        /// This method cancels a scheduled job. If the job is currently executing, it will be
        /// cancelled (if supported by the provider). If the job hasn't started yet, it will be
        /// removed from the schedule.
        /// </para>
        /// <para>
        /// Cancellation is best-effort. Some providers may not support cancelling jobs that are
        /// already executing.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown when jobId is null or empty.</exception>
        Task<bool> CancelAsync(
            string jobId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of a job.
        /// </summary>
        /// <param name="jobId">The job ID to query.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
        /// <returns>The job status, or <c>null</c> if the job was not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when jobId is null or empty.</exception>
        Task<JobStatus?> GetStatusAsync(
            string jobId,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents the status of a background job.
    /// </summary>
    public enum JobStatus
    {
        /// <summary>
        /// The job is scheduled but not yet executed.
        /// </summary>
        Scheduled = 0,

        /// <summary>
        /// The job is currently executing.
        /// </summary>
        Running = 1,

        /// <summary>
        /// The job completed successfully.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// The job failed.
        /// </summary>
        Failed = 3,

        /// <summary>
        /// The job was cancelled.
        /// </summary>
        Cancelled = 4,

        /// <summary>
        /// The job is waiting to be retried.
        /// </summary>
        Retrying = 5
    }
}

