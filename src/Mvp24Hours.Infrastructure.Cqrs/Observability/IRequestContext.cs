//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.Observability;

/// <summary>
/// Provides context information for a request execution, including tracing identifiers.
/// </summary>
/// <remarks>
/// <para>
/// The request context provides:
/// <list type="bullet">
/// <item><strong>CorrelationId</strong> - Groups all operations for a single user request across services</item>
/// <item><strong>CausationId</strong> - Identifies the direct cause of an event or command</item>
/// <item><strong>RequestId</strong> - Unique identifier for the current request</item>
/// </list>
/// </para>
/// <para>
/// <strong>Tracing Hierarchy:</strong>
/// <code>
/// CorrelationId (same for all) ─────────────────────────────────────────────────┐
/// │                                                                              │
/// ├─ Request 1 (RequestId: A, CausationId: null)                                │
/// │   └─ Event 1.1 (RequestId: B, CausationId: A)                               │
/// │       └─ Command 1.1.1 (RequestId: C, CausationId: B)                       │
/// │                                                                              │
/// └─ Request 2 (RequestId: D, CausationId: null)                                │
///     └─ Event 2.1 (RequestId: E, CausationId: D)                               │
/// └──────────────────────────────────────────────────────────────────────────────┘
/// </code>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyCommandHandler : IMediatorCommandHandler&lt;MyCommand&gt;
/// {
///     private readonly IRequestContext _context;
///     
///     public MyCommandHandler(IRequestContext context)
///     {
///         _context = context;
///     }
///     
///     public async Task&lt;Unit&gt; Handle(MyCommand request, CancellationToken cancellationToken)
///     {
///         // Log with context
///         _logger.LogInformation(
///             "Processing command. CorrelationId: {CorrelationId}, RequestId: {RequestId}",
///             _context.CorrelationId,
///             _context.RequestId);
///             
///         return Unit.Value;
///     }
/// }
/// </code>
/// </example>
public interface IRequestContext
{
    /// <summary>
    /// Gets the correlation ID that groups all operations for a single user request.
    /// This ID is propagated across service boundaries.
    /// </summary>
    /// <remarks>
    /// The correlation ID should remain constant for all operations triggered by
    /// the same external request, even when crossing service boundaries.
    /// </remarks>
    string CorrelationId { get; }

    /// <summary>
    /// Gets the causation ID that identifies the direct cause of this operation.
    /// </summary>
    /// <remarks>
    /// The causation ID is the RequestId of the operation that directly caused
    /// this operation to execute. For the initial request, this will be null.
    /// </remarks>
    string? CausationId { get; }

    /// <summary>
    /// Gets the unique identifier for the current request.
    /// </summary>
    /// <remarks>
    /// Each individual request/command/query gets its own unique RequestId.
    /// This ID becomes the CausationId for any operations triggered by this request.
    /// </remarks>
    string RequestId { get; }

    /// <summary>
    /// Gets the timestamp when the request context was created.
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets the optional user identifier associated with this request.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Gets the optional tenant identifier for multi-tenant scenarios.
    /// </summary>
    string? TenantId { get; }

    /// <summary>
    /// Gets additional metadata associated with the request.
    /// </summary>
    IReadOnlyDictionary<string, object?> Metadata { get; }

    /// <summary>
    /// Creates a child context for a nested operation, using this request's ID as the causation ID.
    /// </summary>
    /// <returns>A new request context with inherited correlation ID and new request ID.</returns>
    IRequestContext CreateChildContext();
}

/// <summary>
/// Mutable interface for setting request context values during request processing.
/// </summary>
public interface IRequestContextAccessor
{
    /// <summary>
    /// Gets or sets the current request context.
    /// </summary>
    IRequestContext? Context { get; set; }
}

/// <summary>
/// Factory interface for creating request contexts.
/// </summary>
public interface IRequestContextFactory
{
    /// <summary>
    /// Creates a new request context with the specified parameters.
    /// </summary>
    /// <param name="correlationId">Optional correlation ID. If null, a new one will be generated.</param>
    /// <param name="causationId">Optional causation ID for event chains.</param>
    /// <param name="userId">Optional user identifier.</param>
    /// <param name="tenantId">Optional tenant identifier.</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <returns>A new request context.</returns>
    IRequestContext Create(
        string? correlationId = null,
        string? causationId = null,
        string? userId = null,
        string? tenantId = null,
        IReadOnlyDictionary<string, object?>? metadata = null);
}

