//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Contract
{
    /// <summary>
    /// Represents a batch of jobs that are executed together as a group.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Job batches allow you to group multiple jobs together and execute them as a unit.
    /// Batches provide features such as:
    /// - Atomic execution (all jobs succeed or fail together)
    /// - Batch-level retry policies
    /// - Progress tracking for the entire batch
    /// - Batch completion callbacks
    /// </para>
    /// <para>
    /// <strong>Batch Execution:</strong>
    /// Jobs in a batch can be executed:
    /// - Sequentially (one after another)
    /// - In parallel (all at once, up to a concurrency limit)
    /// - With dependencies (some jobs wait for others)
    /// </para>
    /// <para>
    /// <strong>Batch Status:</strong>
    /// A batch has an overall status that reflects the status of its jobs:
    /// - Pending: Batch is scheduled but not yet started
    /// - Running: At least one job in the batch is executing
    /// - Completed: All jobs completed successfully
    /// - PartialFailure: Some jobs succeeded, some failed
    /// - Failed: All jobs failed
    /// - Cancelled: Batch was cancelled
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a batch of jobs
    /// var batch = new JobBatch
    /// {
    ///     Name = "ProcessOrderBatch",
    ///     Options = new BatchOptions
    ///     {
    ///         ExecutionMode = BatchExecutionMode.Parallel,
    ///         MaxConcurrency = 5,
    ///         StopOnFirstFailure = false
    ///     }
    /// };
    /// 
    /// // Add jobs to the batch
    /// batch.AddJob&lt;ValidateOrderJob, ValidateOrderArgs&gt;(new ValidateOrderArgs { OrderId = 123 });
    /// batch.AddJob&lt;ChargePaymentJob, ChargePaymentArgs&gt;(new ChargePaymentArgs { OrderId = 123 });
    /// batch.AddJob&lt;SendConfirmationJob, SendConfirmationArgs&gt;(new SendConfirmationArgs { OrderId = 123 });
    /// 
    /// // Schedule the batch
    /// var batchId = await jobScheduler.ScheduleBatchAsync(batch);
    /// </code>
    /// </example>
    public interface IJobBatch
    {
        /// <summary>
        /// Gets the unique identifier of the batch.
        /// </summary>
        string BatchId { get; }

        /// <summary>
        /// Gets the name of the batch (optional, for identification).
        /// </summary>
        string? Name { get; }

        /// <summary>
        /// Gets the batch options that control execution behavior.
        /// </summary>
        BatchOptions Options { get; }

        /// <summary>
        /// Gets the jobs in this batch.
        /// </summary>
        IReadOnlyList<IBatchJob> Jobs { get; }

        /// <summary>
        /// Gets the current status of the batch.
        /// </summary>
        BatchStatus Status { get; }

        /// <summary>
        /// Gets when the batch execution started.
        /// </summary>
        System.DateTimeOffset? StartedAt { get; }

        /// <summary>
        /// Gets when the batch execution completed (or failed/cancelled).
        /// </summary>
        System.DateTimeOffset? CompletedAt { get; }
    }

    /// <summary>
    /// Represents a job within a batch.
    /// </summary>
    public interface IBatchJob
    {
        /// <summary>
        /// Gets the unique identifier of the job within the batch.
        /// </summary>
        string JobId { get; }

        /// <summary>
        /// Gets the job type name.
        /// </summary>
        string JobType { get; }

        /// <summary>
        /// Gets the serialized job arguments.
        /// </summary>
        string SerializedArgs { get; }

        /// <summary>
        /// Gets the job options.
        /// </summary>
        JobOptions? JobOptions { get; }

        /// <summary>
        /// Gets the IDs of jobs that must complete before this job executes (dependencies).
        /// </summary>
        IReadOnlyList<string> Dependencies { get; }
    }

    /// <summary>
    /// Options that control batch execution behavior.
    /// </summary>
    public class BatchOptions
    {
        /// <summary>
        /// Gets or sets the execution mode for jobs in the batch.
        /// </summary>
        /// <remarks>
        /// <para>
        /// - Sequential: Jobs execute one after another in order
        /// - Parallel: Jobs execute simultaneously (up to MaxConcurrency)
        /// - DependencyBased: Jobs execute based on their dependencies
        /// </para>
        /// <para>
        /// Default is <see cref="BatchExecutionMode.Parallel"/>.
        /// </para>
        /// </remarks>
        public BatchExecutionMode ExecutionMode { get; set; } = BatchExecutionMode.Parallel;

        /// <summary>
        /// Gets or sets the maximum number of jobs that can execute concurrently.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This setting is only used when <see cref="ExecutionMode"/> is <see cref="BatchExecutionMode.Parallel"/>
        /// or <see cref="BatchExecutionMode.DependencyBased"/>.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to allow unlimited concurrency (not recommended for production).
        /// </para>
        /// <para>
        /// Default is 5.
        /// </para>
        /// </remarks>
        public int? MaxConcurrency { get; set; } = 5;

        /// <summary>
        /// Gets or sets whether to stop batch execution when the first job fails.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, if any job in the batch fails, the batch execution stops
        /// and remaining jobs are cancelled. When <c>false</c>, all jobs are executed
        /// regardless of individual job failures.
        /// </para>
        /// <para>
        /// Default is <c>false</c>.
        /// </para>
        /// </remarks>
        public bool StopOnFirstFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to retry the entire batch if it fails.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, if the batch fails (all jobs fail or <see cref="StopOnFirstFailure"/>
        /// is <c>true</c> and a job fails), the entire batch will be retried according to
        /// the retry policy.
        /// </para>
        /// <para>
        /// Default is <c>false</c> (individual jobs retry, but the batch itself doesn't retry).
        /// </para>
        /// </remarks>
        public bool RetryBatchOnFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum number of batch retry attempts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is only used when <see cref="RetryBatchOnFailure"/> is <c>true</c>.
        /// </para>
        /// <para>
        /// Default is 3.
        /// </para>
        /// </remarks>
        public int MaxBatchRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the maximum execution time for the entire batch.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the batch execution exceeds this time, all running jobs will be cancelled
        /// and the batch will be marked as failed.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to disable timeout (not recommended for production).
        /// </para>
        /// <para>
        /// Default is 1 hour.
        /// </para>
        /// </remarks>
        public System.TimeSpan? Timeout { get; set; } = System.TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets custom metadata associated with the batch.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Represents the execution mode for jobs in a batch.
    /// </summary>
    public enum BatchExecutionMode
    {
        /// <summary>
        /// Jobs execute sequentially, one after another.
        /// </summary>
        Sequential = 0,

        /// <summary>
        /// Jobs execute in parallel, up to the concurrency limit.
        /// </summary>
        Parallel = 1,

        /// <summary>
        /// Jobs execute based on their dependencies (dependency graph).
        /// </summary>
        DependencyBased = 2
    }

    /// <summary>
    /// Represents the status of a job batch.
    /// </summary>
    public enum BatchStatus
    {
        /// <summary>
        /// The batch is scheduled but not yet started.
        /// </summary>
        Pending = 0,

        /// <summary>
        /// At least one job in the batch is currently executing.
        /// </summary>
        Running = 1,

        /// <summary>
        /// All jobs in the batch completed successfully.
        /// </summary>
        Completed = 2,

        /// <summary>
        /// Some jobs succeeded, some failed.
        /// </summary>
        PartialFailure = 3,

        /// <summary>
        /// All jobs in the batch failed.
        /// </summary>
        Failed = 4,

        /// <summary>
        /// The batch was cancelled.
        /// </summary>
        Cancelled = 5
    }
}

