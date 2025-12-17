//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Infrastructure.Pipe
{
    /// <summary>
    /// Defines bulkhead (isolation) behavior for an operation.
    /// Operations implementing this interface will have resource isolation to prevent
    /// resource exhaustion from affecting other operations.
    /// </summary>
    /// <remarks>
    /// The bulkhead pattern isolates operations by limiting the number of concurrent
    /// executions. This prevents one slow or failing operation from consuming all
    /// available resources and affecting other operations.
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyBulkheadOperation : OperationBaseAsync, IBulkheadOperation
    /// {
    ///     public string BulkheadKey => "external-api";
    ///     public int MaxConcurrency => 10;
    ///     public int QueueLimit => 50;
    /// 
    ///     public override async Task ExecuteAsync(IPipelineMessage input)
    ///     {
    ///         // Operation logic - limited concurrency
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IBulkheadOperation
    {
        /// <summary>
        /// Gets the unique key identifying this bulkhead.
        /// Operations with the same key share the same bulkhead (concurrency limiter).
        /// Default implementation returns the type name.
        /// </summary>
        string BulkheadKey => GetType().FullName ?? GetType().Name;

        /// <summary>
        /// Gets the maximum number of concurrent executions allowed.
        /// Default implementation returns 10.
        /// </summary>
        int MaxConcurrency => 10;

        /// <summary>
        /// Gets the maximum number of operations that can wait in the queue.
        /// 0 means no queueing (fail immediately if at capacity).
        /// Default implementation returns 20.
        /// </summary>
        int QueueLimit => 20;

        /// <summary>
        /// Gets the timeout for waiting in the queue.
        /// Null means wait indefinitely.
        /// Default implementation returns 30 seconds.
        /// </summary>
        TimeSpan? QueueTimeout => TimeSpan.FromSeconds(30);

        /// <summary>
        /// Called when the operation is queued waiting for capacity.
        /// Override for custom logging or behavior.
        /// </summary>
        /// <param name="queuePosition">The position in the queue.</param>
        void OnQueued(int queuePosition) { }

        /// <summary>
        /// Called when the operation is rejected due to bulkhead capacity.
        /// Override for custom logging or behavior.
        /// </summary>
        void OnRejected() { }

        /// <summary>
        /// Called when the operation is dequeued and about to execute.
        /// Override for custom logging or behavior.
        /// </summary>
        /// <param name="waitTime">The time spent waiting in the queue.</param>
        void OnDequeued(TimeSpan waitTime) { }
    }

    /// <summary>
    /// Configuration options for bulkhead (isolation) behavior.
    /// </summary>
    public class BulkheadOptions
    {
        /// <summary>
        /// Gets or sets the unique key identifying this bulkhead.
        /// </summary>
        public string Key { get; set; } = "default";

        /// <summary>
        /// Gets or sets the maximum number of concurrent executions allowed.
        /// Default: 10.
        /// </summary>
        public int MaxConcurrency { get; set; } = 10;

        /// <summary>
        /// Gets or sets the maximum number of operations that can wait in the queue.
        /// 0 means no queueing (fail immediately if at capacity).
        /// Default: 20.
        /// </summary>
        public int QueueLimit { get; set; } = 20;

        /// <summary>
        /// Gets or sets the timeout for waiting in the queue.
        /// Null means wait indefinitely.
        /// Default: 30 seconds.
        /// </summary>
        public TimeSpan? QueueTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Gets or sets a callback invoked when operation is queued.
        /// Default: null.
        /// </summary>
        public Action<int>? OnQueued { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when operation is rejected.
        /// Default: null.
        /// </summary>
        public Action? OnRejected { get; set; }

        /// <summary>
        /// Gets or sets a callback invoked when operation is dequeued.
        /// Default: null.
        /// </summary>
        public Action<TimeSpan>? OnDequeued { get; set; }

        /// <summary>
        /// Creates default bulkhead options.
        /// </summary>
        public static BulkheadOptions Default => new();

        /// <summary>
        /// Creates narrow bulkhead options for expensive operations.
        /// </summary>
        public static BulkheadOptions Narrow => new()
        {
            MaxConcurrency = 2,
            QueueLimit = 5,
            QueueTimeout = TimeSpan.FromSeconds(10)
        };

        /// <summary>
        /// Creates wide bulkhead options for lightweight operations.
        /// </summary>
        public static BulkheadOptions Wide => new()
        {
            MaxConcurrency = 50,
            QueueLimit = 100,
            QueueTimeout = TimeSpan.FromMinutes(1)
        };

        /// <summary>
        /// Creates bulkhead options with no queueing (fail fast).
        /// </summary>
        public static BulkheadOptions NoQueue => new()
        {
            MaxConcurrency = 10,
            QueueLimit = 0,
            QueueTimeout = null
        };
    }

    /// <summary>
    /// Exception thrown when a bulkhead rejects an operation.
    /// </summary>
    public class PipelineBulkheadRejectedException : Exception
    {
        /// <summary>
        /// Gets the bulkhead key that rejected the operation.
        /// </summary>
        public string BulkheadKey { get; }

        /// <summary>
        /// Gets the reason for rejection.
        /// </summary>
        public BulkheadRejectionReason Reason { get; }

        /// <summary>
        /// Creates a new instance of the exception.
        /// </summary>
        /// <param name="bulkheadKey">The bulkhead key.</param>
        /// <param name="reason">The reason for rejection.</param>
        public PipelineBulkheadRejectedException(string bulkheadKey, BulkheadRejectionReason reason)
            : base($"Bulkhead '{bulkheadKey}' rejected operation: {reason}.")
        {
            BulkheadKey = bulkheadKey;
            Reason = reason;
        }

        /// <summary>
        /// Creates a new instance of the exception.
        /// </summary>
        /// <param name="bulkheadKey">The bulkhead key.</param>
        /// <param name="reason">The reason for rejection.</param>
        /// <param name="innerException">The inner exception.</param>
        public PipelineBulkheadRejectedException(string bulkheadKey, BulkheadRejectionReason reason, Exception innerException)
            : base($"Bulkhead '{bulkheadKey}' rejected operation: {reason}.", innerException)
        {
            BulkheadKey = bulkheadKey;
            Reason = reason;
        }
    }

    /// <summary>
    /// Reasons why a bulkhead may reject an operation.
    /// </summary>
    public enum BulkheadRejectionReason
    {
        /// <summary>
        /// The bulkhead is at capacity and the queue is full.
        /// </summary>
        QueueFull,

        /// <summary>
        /// The operation waited too long in the queue.
        /// </summary>
        QueueTimeout,

        /// <summary>
        /// The bulkhead is at capacity and queueing is disabled.
        /// </summary>
        AtCapacity
    }
}

