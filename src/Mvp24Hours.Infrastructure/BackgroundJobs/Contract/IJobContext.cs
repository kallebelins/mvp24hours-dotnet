//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Contract
{
    /// <summary>
    /// Provides context information about the current job execution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface provides access to metadata about the current job execution,
    /// including the job ID, attempt number, cancellation token, and custom metadata.
    /// It is passed to job handlers during execution to provide context and enable
    /// cancellation and progress tracking.
    /// </para>
    /// <para>
    /// <strong>Job ID:</strong>
    /// Each job execution has a unique identifier that can be used for tracking,
    /// logging, and correlation with other systems.
    /// </para>
    /// <para>
    /// <strong>Attempt Number:</strong>
    /// When a job is retried after failure, the attempt number is incremented.
    /// The first execution has attempt number 1.
    /// </para>
    /// <para>
    /// <strong>Cancellation Token:</strong>
    /// The cancellation token allows jobs to be cancelled gracefully. Jobs should
    /// periodically check the cancellation token and stop processing when cancellation
    /// is requested.
    /// </para>
    /// <para>
    /// <strong>Metadata:</strong>
    /// Custom metadata can be attached to jobs and accessed during execution.
    /// This is useful for passing additional context or configuration.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyJob : IBackgroundJob&lt;MyJobArgs&gt;
    /// {
    ///     public async Task ExecuteAsync(MyJobArgs args, IJobContext context, CancellationToken cancellationToken)
    ///     {
    ///         // Access job ID
    ///         Console.WriteLine($"Executing job {context.JobId}");
    ///         
    ///         // Check attempt number
    ///         if (context.AttemptNumber > 1)
    ///         {
    ///             Console.WriteLine($"This is retry attempt {context.AttemptNumber}");
    ///         }
    ///         
    ///         // Access metadata
    ///         if (context.Metadata.TryGetValue("UserId", out var userId))
    ///         {
    ///             Console.WriteLine($"Job triggered by user: {userId}");
    ///         }
    ///         
    ///         // Check for cancellation
    ///         cancellationToken.ThrowIfCancellationRequested();
    ///         
    ///         // Do work...
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IJobContext
    {
        /// <summary>
        /// Gets the unique identifier of the job execution.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This ID is unique for each job execution and can be used for:
        /// - Logging and tracing
        /// - Correlation with other systems
        /// - Querying job status
        /// - Cancelling specific job executions
        /// </para>
        /// <para>
        /// The format of the ID is provider-dependent, but it should be a string
        /// that uniquely identifies the job execution.
        /// </para>
        /// </remarks>
        string JobId { get; }

        /// <summary>
        /// Gets the attempt number of this job execution.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The attempt number starts at 1 for the first execution. If the job fails
        /// and is retried, the attempt number is incremented for each retry.
        /// </para>
        /// <para>
        /// This can be used to:
        /// - Implement exponential backoff
        /// - Log retry attempts
        /// - Adjust behavior based on retry count
        /// </para>
        /// </remarks>
        int AttemptNumber { get; }

        /// <summary>
        /// Gets the cancellation token for this job execution.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Jobs should periodically check this cancellation token and stop processing
        /// when cancellation is requested. This allows for graceful shutdown and
        /// cancellation of long-running jobs.
        /// </para>
        /// <para>
        /// The cancellation token is automatically propagated from the job scheduler
        /// and can be triggered by:
        /// - Application shutdown
        /// - Explicit job cancellation
        /// - Timeout expiration
        /// </para>
        /// </remarks>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the metadata associated with this job execution.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Metadata is a dictionary of key-value pairs that can be used to pass
        /// additional context or configuration to the job. Common use cases include:
        /// - User ID who triggered the job
        /// - Tenant ID for multi-tenant scenarios
        /// - Custom configuration values
        /// - Correlation IDs for tracing
        /// </para>
        /// <para>
        /// Metadata is set when the job is scheduled and remains constant throughout
        /// the job execution (including retries).
        /// </para>
        /// </remarks>
        IReadOnlyDictionary<string, string> Metadata { get; }

        /// <summary>
        /// Gets when the job execution started.
        /// </summary>
        /// <remarks>
        /// This timestamp indicates when the job execution began. It can be used for:
        /// - Calculating execution duration
        /// - Logging and monitoring
        /// - Timeout detection
        /// </remarks>
        DateTimeOffset StartedAt { get; }

        /// <summary>
        /// Gets the job type name.
        /// </summary>
        /// <remarks>
        /// This is the fully qualified type name of the job handler class.
        /// It can be used for logging, debugging, and job type identification.
        /// </remarks>
        string JobType { get; }

        /// <summary>
        /// Gets the queue name where this job is being processed.
        /// </summary>
        /// <remarks>
        /// Jobs can be assigned to different queues for priority-based processing
        /// or workload isolation. This property indicates which queue the job is in.
        /// </remarks>
        string? Queue { get; }
    }
}

