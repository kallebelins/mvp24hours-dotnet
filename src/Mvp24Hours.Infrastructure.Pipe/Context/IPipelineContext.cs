//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using Mvp24Hours.Core.Contract.Infrastructure;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Mvp24Hours.Infrastructure.Pipe.Context
{
    /// <summary>
    /// Provides contextual information for pipeline execution, including correlation tracking,
    /// user context, metadata, and diagnostics integration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Purpose:</strong>
    /// The pipeline context provides a unified way to access and propagate contextual information
    /// throughout the execution of a pipeline, including:
    /// <list type="bullet">
    /// <item>Correlation ID - for distributed tracing across operations</item>
    /// <item>Causation ID - tracks what triggered the current execution</item>
    /// <item>User context - authenticated user information</item>
    /// <item>Tenant context - for multi-tenant applications</item>
    /// <item>Operation metadata - custom key-value pairs</item>
    /// <item>State snapshots - for debugging and auditing</item>
    /// <item>Activity/Span integration - OpenTelemetry compatibility</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Context Propagation:</strong>
    /// The context is automatically propagated between pipeline operations, allowing
    /// for consistent tracking and correlation across the entire pipeline execution.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class MyOperation : OperationBaseAsync
    /// {
    ///     private readonly IPipelineContextAccessor _contextAccessor;
    ///     
    ///     public override async Task ExecuteAsync(IPipelineMessage input)
    ///     {
    ///         var context = _contextAccessor.Context;
    ///         _logger.LogInformation("Processing with CorrelationId: {CorrelationId}", context.CorrelationId);
    ///         
    ///         // Add custom metadata
    ///         context.SetMetadata("processed_at", DateTime.UtcNow);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IPipelineContext
    {
        #region [ Identification ]

        /// <summary>
        /// Gets the unique identifier for this pipeline execution.
        /// Used for distributed tracing and logging correlation.
        /// </summary>
        string CorrelationId { get; }

        /// <summary>
        /// Gets or sets the causation ID, representing what triggered this pipeline execution.
        /// Useful for tracking event chains and debugging.
        /// </summary>
        string? CausationId { get; set; }

        /// <summary>
        /// Gets or sets the parent context ID, for nested pipeline executions.
        /// </summary>
        string? ParentContextId { get; set; }

        /// <summary>
        /// Gets the timestamp when this context was created.
        /// </summary>
        DateTime CreatedAt { get; }

        #endregion

        #region [ User Context ]

        /// <summary>
        /// Gets or sets the current user's ID, or null if not authenticated.
        /// </summary>
        string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the current user's name, or null if not authenticated.
        /// </summary>
        string? UserName { get; set; }

        /// <summary>
        /// Gets or sets the current tenant's ID, or null if not in a multi-tenant context.
        /// </summary>
        string? TenantId { get; set; }

        /// <summary>
        /// Gets whether a user is associated with this context.
        /// </summary>
        bool HasUser { get; }

        #endregion

        #region [ Metadata ]

        /// <summary>
        /// Gets a read-only view of all metadata associated with this context.
        /// </summary>
        IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Gets a metadata value by key.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The metadata key.</param>
        /// <returns>The value if found and of correct type; otherwise, default.</returns>
        T? GetMetadata<T>(string key);

        /// <summary>
        /// Sets a metadata value.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The metadata key.</param>
        /// <param name="value">The value to store.</param>
        void SetMetadata<T>(string key, T value);

        /// <summary>
        /// Removes a metadata value.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <returns>True if the value was removed; otherwise, false.</returns>
        bool RemoveMetadata(string key);

        /// <summary>
        /// Checks if a metadata key exists.
        /// </summary>
        /// <param name="key">The metadata key.</param>
        /// <returns>True if the key exists; otherwise, false.</returns>
        bool HasMetadata(string key);

        #endregion

        #region [ Diagnostics ]

        /// <summary>
        /// Gets or sets the current Activity for distributed tracing (OpenTelemetry integration).
        /// </summary>
        Activity? CurrentActivity { get; set; }

        /// <summary>
        /// Gets the trace ID from the current Activity, or null if no Activity is present.
        /// </summary>
        string? TraceId { get; }

        /// <summary>
        /// Gets the span ID from the current Activity, or null if no Activity is present.
        /// </summary>
        string? SpanId { get; }

        /// <summary>
        /// Starts a new Activity/span for an operation within the pipeline.
        /// </summary>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="kind">The kind of activity (default: Internal).</param>
        /// <returns>The started Activity, or null if tracing is not enabled.</returns>
        Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Internal);

        #endregion

        #region [ State Snapshots ]

        /// <summary>
        /// Gets all state snapshots captured during pipeline execution.
        /// </summary>
        IReadOnlyList<PipelineStateSnapshot> Snapshots { get; }

        /// <summary>
        /// Captures a snapshot of the current state.
        /// </summary>
        /// <param name="operationName">The name of the operation capturing the snapshot.</param>
        /// <param name="state">The state data to capture.</param>
        /// <param name="description">Optional description of the snapshot.</param>
        void CaptureSnapshot(string operationName, object? state, string? description = null);

        /// <summary>
        /// Gets the last captured snapshot, or null if none exist.
        /// </summary>
        PipelineStateSnapshot? LastSnapshot { get; }

        /// <summary>
        /// Clears all snapshots. Useful for memory management in long-running pipelines.
        /// </summary>
        void ClearSnapshots();

        #endregion

        #region [ Factory Methods ]

        /// <summary>
        /// Creates a child context for nested pipeline executions.
        /// The child inherits correlation tracking and user context.
        /// </summary>
        /// <returns>A new child context.</returns>
        IPipelineContext CreateChildContext();

        /// <summary>
        /// Clones this context with a new correlation ID.
        /// </summary>
        /// <param name="newCorrelationId">The new correlation ID.</param>
        /// <returns>A cloned context with the new correlation ID.</returns>
        IPipelineContext CloneWithCorrelationId(string newCorrelationId);

        #endregion

        #region [ Integration ]

        /// <summary>
        /// Populates this context from an <see cref="IRequestContext"/>.
        /// </summary>
        /// <param name="requestContext">The request context to copy from.</param>
        void PopulateFromRequestContext(IRequestContext requestContext);

        /// <summary>
        /// Creates an <see cref="IRequestContext"/> from this pipeline context.
        /// </summary>
        /// <returns>A new request context with values from this pipeline context.</returns>
        IRequestContext ToRequestContext();

        #endregion
    }

    /// <summary>
    /// Represents a snapshot of pipeline state at a specific point in time.
    /// </summary>
    public sealed record PipelineStateSnapshot
    {
        /// <summary>
        /// Gets the timestamp when the snapshot was captured.
        /// </summary>
        public DateTime CapturedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the name of the operation that captured the snapshot.
        /// </summary>
        public required string OperationName { get; init; }

        /// <summary>
        /// Gets the correlation ID at the time of capture.
        /// </summary>
        public required string CorrelationId { get; init; }

        /// <summary>
        /// Gets the optional description of the snapshot.
        /// </summary>
        public string? Description { get; init; }

        /// <summary>
        /// Gets the state data captured in the snapshot.
        /// </summary>
        public object? State { get; init; }

        /// <summary>
        /// Gets the Activity/Span ID at the time of capture.
        /// </summary>
        public string? SpanId { get; init; }

        /// <summary>
        /// Gets metadata associated with the snapshot.
        /// </summary>
        public IReadOnlyDictionary<string, object>? Metadata { get; init; }

        /// <summary>
        /// Gets the sequence number of this snapshot within the pipeline execution.
        /// </summary>
        public int SequenceNumber { get; init; }
    }
}

