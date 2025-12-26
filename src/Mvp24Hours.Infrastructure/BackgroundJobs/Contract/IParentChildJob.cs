//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Infrastructure.BackgroundJobs.Contract
{
    /// <summary>
    /// Represents a parent-child relationship between jobs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Parent-child jobs allow you to create hierarchical job structures where:
    /// - A parent job can have multiple child jobs
    /// - Child jobs can have their own child jobs (grandchildren)
    /// - The parent job can control the execution of its children
    /// - Child jobs can report progress back to the parent
    /// </para>
    /// <para>
    /// <strong>Parent Job:</strong>
    /// The parent job is the root of the hierarchy. It can:
    /// - Schedule child jobs
    /// - Wait for child jobs to complete
    /// - Cancel child jobs
    /// - Aggregate results from child jobs
    /// </para>
    /// <para>
    /// <strong>Child Jobs:</strong>
    /// Child jobs are scheduled by the parent and execute independently. They can:
    /// - Report progress to the parent
    /// - Access parent job context
    /// - Have their own retry policies and options
    /// </para>
    /// <para>
    /// <strong>Execution Modes:</strong>
    /// Child jobs can execute:
    /// - Sequentially (one after another)
    /// - In parallel (all at once, up to a concurrency limit)
    /// - Based on dependencies (some children wait for others)
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Parent job schedules child jobs
    /// public class ProcessOrderJob : IBackgroundJob&lt;ProcessOrderArgs&gt;, IParentJob
    /// {
    ///     private readonly IJobScheduler _jobScheduler;
    ///     
    ///     public ProcessOrderJob(IJobScheduler jobScheduler)
    ///     {
    ///         _jobScheduler = jobScheduler;
    ///     }
    ///     
    ///     public async Task ExecuteAsync(ProcessOrderArgs args, IJobContext context, CancellationToken cancellationToken)
    ///     {
    ///         // Schedule child jobs
    ///         var childJob1 = await _jobScheduler.EnqueueChildAsync&lt;ValidateOrderJob, ValidateOrderArgs&gt;(
    ///             context.JobId,
    ///             new ValidateOrderArgs { OrderId = args.OrderId });
    ///         
    ///         var childJob2 = await _jobScheduler.EnqueueChildAsync&lt;ChargePaymentJob, ChargePaymentArgs&gt;(
    ///             context.JobId,
    ///             new ChargePaymentArgs { OrderId = args.OrderId });
    ///         
    ///         // Wait for children to complete
    ///         await _jobScheduler.WaitForChildrenAsync(context.JobId, cancellationToken);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IParentJob
    {
        /// <summary>
        /// Gets the ID of the parent job.
        /// </summary>
        string ParentJobId { get; }

        /// <summary>
        /// Gets the child job IDs.
        /// </summary>
        IReadOnlyList<string> ChildJobIds { get; }

        /// <summary>
        /// Gets the parent-child job options.
        /// </summary>
        ParentChildJobOptions Options { get; }
    }

    /// <summary>
    /// Represents a child job that belongs to a parent job.
    /// </summary>
    public interface IChildJob
    {
        /// <summary>
        /// Gets the ID of the parent job.
        /// </summary>
        string ParentJobId { get; }

        /// <summary>
        /// Gets the child job ID.
        /// </summary>
        string ChildJobId { get; }

        /// <summary>
        /// Gets the execution order of this child job (for sequential execution).
        /// </summary>
        int? ExecutionOrder { get; }

        /// <summary>
        /// Gets the IDs of sibling jobs that must complete before this child executes (dependencies).
        /// </summary>
        IReadOnlyList<string> SiblingDependencies { get; }
    }

    /// <summary>
    /// Options that control parent-child job execution behavior.
    /// </summary>
    public class ParentChildJobOptions
    {
        /// <summary>
        /// Gets or sets the execution mode for child jobs.
        /// </summary>
        /// <remarks>
        /// <para>
        /// - Sequential: Children execute one after another in order
        /// - Parallel: Children execute simultaneously (up to MaxConcurrency)
        /// - DependencyBased: Children execute based on their dependencies
        /// </para>
        /// <para>
        /// Default is <see cref="ChildExecutionMode.Parallel"/>.
        /// </para>
        /// </remarks>
        public ChildExecutionMode ExecutionMode { get; set; } = ChildExecutionMode.Parallel;

        /// <summary>
        /// Gets or sets the maximum number of child jobs that can execute concurrently.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This setting is only used when <see cref="ExecutionMode"/> is <see cref="ChildExecutionMode.Parallel"/>
        /// or <see cref="ChildExecutionMode.DependencyBased"/>.
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
        /// Gets or sets whether the parent job waits for all children to complete before finishing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, the parent job will not complete until all child jobs have completed
        /// (successfully or failed). When <c>false</c>, the parent job completes immediately after
        /// scheduling children (fire-and-forget children).
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool WaitForChildren { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to cancel child jobs if the parent job fails or is cancelled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, if the parent job fails or is cancelled, all running and pending
        /// child jobs will be cancelled. When <c>false</c>, child jobs continue executing
        /// independently even if the parent fails.
        /// </para>
        /// <para>
        /// Default is <c>true</c>.
        /// </para>
        /// </remarks>
        public bool CancelChildrenOnParentFailure { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to fail the parent job if any child job fails.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <c>true</c>, if any child job fails, the parent job will be marked as failed
        /// (even if other children succeed). When <c>false</c>, the parent job succeeds as long
        /// as it completes successfully, regardless of child job outcomes.
        /// </para>
        /// <para>
        /// Default is <c>false</c>.
        /// </para>
        /// </remarks>
        public bool FailParentOnChildFailure { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum time to wait for child jobs to complete.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If child jobs don't complete within this time, they will be cancelled and the parent
        /// job will proceed (or fail, depending on <see cref="FailParentOnChildFailure"/>).
        /// </para>
        /// <para>
        /// Set to <c>null</c> to wait indefinitely (not recommended for production).
        /// </para>
        /// <para>
        /// Default is 1 hour.
        /// </para>
        /// </remarks>
        public System.TimeSpan? MaxWaitTime { get; set; } = System.TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Represents the execution mode for child jobs.
    /// </summary>
    public enum ChildExecutionMode
    {
        /// <summary>
        /// Child jobs execute sequentially, one after another.
        /// </summary>
        Sequential = 0,

        /// <summary>
        /// Child jobs execute in parallel, up to the concurrency limit.
        /// </summary>
        Parallel = 1,

        /// <summary>
        /// Child jobs execute based on their dependencies (dependency graph).
        /// </summary>
        DependencyBased = 2
    }
}

