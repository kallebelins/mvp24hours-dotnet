//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Options
{
    /// <summary>
    /// Configuration options for background job execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options control various aspects of job execution behavior, including retry policies,
    /// timeouts, priority, queue assignment, and metadata. Options can be specified when scheduling
    /// a job and will override default options configured for the job scheduler.
    /// </para>
    /// <para>
    /// <strong>Retry Policy:</strong>
    /// Jobs can be configured to retry automatically on failure. The retry policy controls:
    /// - Maximum number of retry attempts
    /// - Delay between retries (with exponential backoff support)
    /// - Which exceptions trigger retries
    /// </para>
    /// <para>
    /// <strong>Timeout:</strong>
    /// Jobs can have a maximum execution time. If the job exceeds this time, it will be cancelled.
    /// This helps prevent jobs from running indefinitely.
    /// </para>
    /// <para>
    /// <strong>Priority:</strong>
    /// Jobs can be assigned different priority levels. Higher priority jobs are executed before
    /// lower priority jobs. This is useful for ensuring critical jobs are processed first.
    /// </para>
    /// <para>
    /// <strong>Queue:</strong>
    /// Jobs can be assigned to different queues for workload isolation or priority-based processing.
    /// Different queues can have different worker configurations (e.g., number of concurrent workers).
    /// </para>
    /// </remarks>
    public class JobOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobOptions"/> class.
        /// </summary>
        public JobOptions()
        {
            Metadata = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If a job fails, it will be retried up to this number of times. Set to 0 to disable retries.
        /// </para>
        /// <para>
        /// Default is 3 retry attempts.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var options = new JobOptions
        /// {
        ///     MaxRetryAttempts = 5 // Retry up to 5 times
        /// };
        /// </code>
        /// </example>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the initial delay before the first retry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When a job fails and is retried, this is the delay before the first retry attempt.
        /// Subsequent retries will use exponential backoff if <see cref="UseExponentialBackoff"/> is enabled.
        /// </para>
        /// <para>
        /// Default is 5 seconds.
        /// </para>
        /// </remarks>
        public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the maximum delay between retries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When using exponential backoff, the delay between retries will not exceed this value.
        /// This prevents excessively long delays for jobs that fail many times.
        /// </para>
        /// <para>
        /// Default is 1 hour.
        /// </para>
        /// </remarks>
        public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets whether to use exponential backoff for retries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the delay between retries increases exponentially:
        /// - First retry: <see cref="InitialRetryDelay"/>
        /// - Second retry: <see cref="InitialRetryDelay"/> * 2
        /// - Third retry: <see cref="InitialRetryDelay"/> * 4
        /// - And so on, up to <see cref="MaxRetryDelay"/>
        /// </para>
        /// <para>
        /// When <c>false</c>, all retries use <see cref="InitialRetryDelay"/>.
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum execution time for the job.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the job execution exceeds this time, it will be cancelled. This helps prevent
        /// jobs from running indefinitely and consuming resources.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to disable timeout (not recommended for production).
        /// </para>
        /// <para>
        /// Default is 1 hour.
        /// </para>
        /// </remarks>
        public TimeSpan? Timeout { get; set; } = TimeSpan.FromHours(1);

        /// <summary>
        /// Gets or sets the job priority.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Higher priority jobs are executed before lower priority jobs. This is useful for
        /// ensuring critical jobs are processed first.
        /// </para>
        /// <para>
        /// Priority is only considered within the same queue. Jobs in different queues are
        /// processed independently.
        /// </para>
        /// <para>
        /// Default is <see cref="JobPriority.Normal"/>.
        /// </para>
        /// </remarks>
        public JobPriority Priority { get; set; } = JobPriority.Normal;

        /// <summary>
        /// Gets or sets the queue name where the job will be processed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Jobs can be assigned to different queues for workload isolation or priority-based
        /// processing. Different queues can have different worker configurations (e.g., number
        /// of concurrent workers, retry policies, timeouts).
        /// </para>
        /// <para>
        /// If not specified, the job will be assigned to the default queue.
        /// </para>
        /// <para>
        /// Queue names are case-sensitive and should follow naming conventions (e.g., lowercase
        /// with hyphens: "high-priority", "low-priority", "email-processing").
        /// </para>
        /// </remarks>
        public string? Queue { get; set; }

        /// <summary>
        /// Gets or sets custom metadata associated with the job.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Metadata is a dictionary of key-value pairs that can be used to pass additional
        /// context or configuration to the job. Common use cases include:
        /// - User ID who triggered the job
        /// - Tenant ID for multi-tenant scenarios
        /// - Custom configuration values
        /// - Correlation IDs for tracing
        /// </para>
        /// <para>
        /// Metadata is accessible during job execution via <see cref="Contract.IJobContext.Metadata"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var options = new JobOptions
        /// {
        ///     Metadata = new Dictionary&lt;string, string&gt;
        ///     {
        ///         { "UserId", "12345" },
        ///         { "TenantId", "tenant-abc" },
        ///         { "CorrelationId", Guid.NewGuid().ToString() }
        ///     }
        /// };
        /// </code>
        /// </example>
        public IDictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Gets or sets whether to delete the job after successful execution.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the job will be deleted from storage after successful execution.
        /// This is useful for fire-and-forget jobs that don't need to be retained.
        /// </para>
        /// <para>
        /// When <c>false</c>, the job will be retained in storage for auditing, debugging,
        /// or reprocessing purposes.
        /// </para>
        /// <para>
        /// Default is <c>false</c> (jobs are retained).
        /// </para>
        /// </remarks>
        public bool DeleteOnSuccess { get; set; } = false;

        /// <summary>
        /// Gets or sets the number of days to retain the job after completion (success or failure).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Jobs are retained in storage for this number of days after completion for auditing
        /// and debugging purposes. After this period, jobs are automatically deleted.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to retain jobs indefinitely (not recommended for production).
        /// </para>
        /// <para>
        /// This setting is ignored if <see cref="DeleteOnSuccess"/> is <c>true</c>.
        /// </para>
        /// <para>
        /// Default is 30 days.
        /// </para>
        /// </remarks>
        public int? RetentionDays { get; set; } = 30;

        /// <summary>
        /// Validates the configuration options.
        /// </summary>
        /// <returns>A list of validation errors, or an empty list if valid.</returns>
        /// <remarks>
        /// This method checks for logical inconsistencies in the configuration (e.g., negative
        /// retry attempts, invalid timeouts).
        /// </remarks>
        public IList<string> Validate()
        {
            var errors = new List<string>();

            if (MaxRetryAttempts < 0)
            {
                errors.Add("Maximum retry attempts must be greater than or equal to zero.");
            }

            if (InitialRetryDelay <= TimeSpan.Zero)
            {
                errors.Add("Initial retry delay must be greater than zero.");
            }

            if (MaxRetryDelay <= TimeSpan.Zero)
            {
                errors.Add("Maximum retry delay must be greater than zero.");
            }

            if (MaxRetryDelay < InitialRetryDelay)
            {
                errors.Add("Maximum retry delay must be greater than or equal to initial retry delay.");
            }

            if (Timeout.HasValue && Timeout.Value <= TimeSpan.Zero)
            {
                errors.Add("Timeout must be greater than zero.");
            }

            if (RetentionDays.HasValue && RetentionDays.Value < 0)
            {
                errors.Add("Retention days must be greater than or equal to zero.");
            }

            return errors;
        }

        /// <summary>
        /// Creates default job options suitable for most scenarios.
        /// </summary>
        /// <returns>Default options instance.</returns>
        public static JobOptions Default => new();
    }

    /// <summary>
    /// Represents the priority of a background job.
    /// </summary>
    public enum JobPriority
    {
        /// <summary>
        /// Low priority (processed last).
        /// </summary>
        Low = 0,

        /// <summary>
        /// Normal priority (default).
        /// </summary>
        Normal = 1,

        /// <summary>
        /// High priority (processed first).
        /// </summary>
        High = 2,

        /// <summary>
        /// Critical priority (processed immediately).
        /// </summary>
        Critical = 3
    }
}

