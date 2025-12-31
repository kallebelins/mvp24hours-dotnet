//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mvp24Hours.Infrastructure.CronJob.Context
{
    /// <summary>
    /// Provides execution context information for CronJob operations.
    /// Contains metadata about the current execution including job identity, timing, and attempt information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The context is created for each execution and provides immutable information about:
    /// </para>
    /// <list type="bullet">
    /// <item><b>JobId:</b> Unique identifier for this execution instance</item>
    /// <item><b>JobName:</b> Name of the CronJob type</item>
    /// <item><b>StartTime:</b> When the execution started</item>
    /// <item><b>Attempt:</b> Current retry attempt number (1-based)</item>
    /// <item><b>ExecutionCount:</b> Total executions for this job</item>
    /// <item><b>Properties:</b> Custom key-value properties</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// public override async Task DoWork(CancellationToken cancellationToken)
    /// {
    ///     var context = GetContext();
    ///     _logger.LogInformation("Executing {JobName} (ID: {JobId}, Attempt: {Attempt})",
    ///         context.JobName, context.JobId, context.CurrentAttempt);
    ///     
    ///     // Your job logic here
    /// }
    /// </code>
    /// </example>
    public interface ICronJobContext
    {
        /// <summary>
        /// Gets the unique identifier for this execution instance.
        /// A new ID is generated for each execution (not each retry).
        /// </summary>
        Guid JobId { get; }

        /// <summary>
        /// Gets the name of the CronJob (typically the type name).
        /// </summary>
        string JobName { get; }

        /// <summary>
        /// Gets the UTC timestamp when this execution started.
        /// </summary>
        DateTimeOffset StartTime { get; }

        /// <summary>
        /// Gets the scheduled time for this execution (based on CRON expression).
        /// May be null for run-once jobs or immediate executions.
        /// </summary>
        DateTimeOffset? ScheduledTime { get; }

        /// <summary>
        /// Gets the current retry attempt number (1-based).
        /// First execution is attempt 1, first retry is attempt 2, etc.
        /// </summary>
        int CurrentAttempt { get; }

        /// <summary>
        /// Gets the maximum number of attempts allowed (1 + max retries).
        /// </summary>
        int MaxAttempts { get; }

        /// <summary>
        /// Gets whether this is a retry execution.
        /// </summary>
        bool IsRetry { get; }

        /// <summary>
        /// Gets the total number of executions for this job instance.
        /// </summary>
        long ExecutionCount { get; }

        /// <summary>
        /// Gets the cancellation token for this execution.
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Gets the CRON expression used for scheduling.
        /// May be null for run-once jobs.
        /// </summary>
        string? CronExpression { get; }

        /// <summary>
        /// Gets the timezone used for CRON expression evaluation.
        /// </summary>
        TimeZoneInfo? TimeZone { get; }

        /// <summary>
        /// Gets the correlation ID for distributed tracing.
        /// </summary>
        string? CorrelationId { get; }

        /// <summary>
        /// Gets the parent job ID if this job was triggered by another job (dependency).
        /// </summary>
        Guid? ParentJobId { get; }

        /// <summary>
        /// Gets custom properties associated with this execution.
        /// </summary>
        IReadOnlyDictionary<string, object?> Properties { get; }

        /// <summary>
        /// Gets a property value by key, or default if not found.
        /// </summary>
        /// <typeparam name="T">The type of the property value.</typeparam>
        /// <param name="key">The property key.</param>
        /// <param name="defaultValue">Default value if property not found.</param>
        /// <returns>The property value or default.</returns>
        T? GetProperty<T>(string key, T? defaultValue = default);

        /// <summary>
        /// Gets the elapsed time since the execution started.
        /// </summary>
        TimeSpan Elapsed { get; }

        /// <summary>
        /// Gets whether the execution has exceeded its timeout (if configured).
        /// </summary>
        bool IsTimedOut { get; }
    }
}

