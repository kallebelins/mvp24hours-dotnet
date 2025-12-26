//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.BackgroundJobs.Options;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Contract
{
    /// <summary>
    /// Represents a job continuation that executes after another job completes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Job continuations allow you to chain jobs together, where a continuation job
    /// executes only after its parent job completes successfully. This is useful for
    /// implementing workflows and multi-step processes.
    /// </para>
    /// <para>
    /// <strong>Continuation Conditions:</strong>
    /// Continuations can be configured to execute:
    /// - Only on success (default)
    /// - On success or failure
    /// - Only on failure
    /// </para>
    /// <para>
    /// <strong>Parent Job:</strong>
    /// The parent job must complete before the continuation can execute. If the parent
    /// job is cancelled or fails (depending on continuation conditions), the continuation
    /// will not execute.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Schedule a parent job
    /// var parentJobId = await jobScheduler.EnqueueAsync&lt;ProcessOrderJob, ProcessOrderArgs&gt;(
    ///     new ProcessOrderArgs { OrderId = 123 });
    /// 
    /// // Schedule a continuation that executes after the parent completes successfully
    /// var continuationJobId = await jobScheduler.ContinueWithAsync&lt;SendConfirmationEmailJob, SendEmailArgs&gt;(
    ///     parentJobId,
    ///     new SendEmailArgs { To = "customer@example.com", Subject = "Order Confirmed" },
    ///     continuationOptions: new ContinuationOptions { ExecuteOnSuccessOnly = true });
    /// </code>
    /// </example>
    public interface IJobContinuation
    {
        /// <summary>
        /// Gets the ID of the parent job that must complete before this continuation executes.
        /// </summary>
        string ParentJobId { get; }

        /// <summary>
        /// Gets the continuation options that control when this continuation executes.
        /// </summary>
        ContinuationOptions Options { get; }
    }

    /// <summary>
    /// Options that control when a continuation job executes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These options determine the conditions under which a continuation job will execute
    /// after its parent job completes.
    /// </para>
    /// </remarks>
    public class ContinuationOptions
    {
        /// <summary>
        /// Gets or sets whether the continuation executes only when the parent job succeeds.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the continuation executes only if the parent job completes successfully.
        /// When <c>false</c>, the continuation executes regardless of parent job outcome (unless
        /// <see cref="ExecuteOnFailureOnly"/> is <c>true</c>).
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool ExecuteOnSuccessOnly { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the continuation executes only when the parent job fails.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the continuation executes only if the parent job fails.
        /// This is useful for error handling and cleanup jobs.
        /// </para>
        /// <para>
        /// If both <see cref="ExecuteOnSuccessOnly"/> and <see cref="ExecuteOnFailureOnly"/>
        /// are <c>true</c>, the continuation will execute in both cases (equivalent to both being <c>false</c>).
        /// </para>
        /// <para>
        /// Default is <c>false</c>.
        /// </para>
        /// </remarks>
        public bool ExecuteOnFailureOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum time to wait for the parent job to complete before
        /// cancelling the continuation.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the parent job doesn't complete within this time, the continuation will be
        /// cancelled and not executed. This prevents continuations from waiting indefinitely
        /// for parent jobs that may never complete.
        /// </para>
        /// <para>
        /// Set to <c>null</c> to wait indefinitely (not recommended for production).
        /// </para>
        /// <para>
        /// Default is 24 hours.
        /// </para>
        /// </remarks>
        public System.TimeSpan? MaxWaitTime { get; set; } = System.TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets the job options for the continuation job.
        /// </summary>
        /// <remarks>
        /// <para>
        /// These options control the execution behavior of the continuation job itself
        /// (retry policy, timeout, priority, queue, etc.).
        /// </para>
        /// </remarks>
        public JobOptions? JobOptions { get; set; }
    }
}

