//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;

namespace Mvp24Hours.Application.Contract.Observability;

/// <summary>
/// Provides access to the current correlation ID for tracing operations across service boundaries.
/// </summary>
/// <remarks>
/// <para>
/// The correlation ID is a unique identifier that follows a request through all service operations,
/// enabling distributed tracing and log correlation.
/// </para>
/// <para>
/// <strong>Usage:</strong>
/// <list type="bullet">
/// <item>Inject <see cref="ICorrelationIdAccessor"/> to read the current correlation ID</item>
/// <item>Use <see cref="ICorrelationIdSetter"/> to set a new correlation ID (typically in middleware)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class MyService
/// {
///     private readonly ICorrelationIdAccessor _correlationId;
///     private readonly ILogger&lt;MyService&gt; _logger;
/// 
///     public MyService(ICorrelationIdAccessor correlationId, ILogger&lt;MyService&gt; logger)
///     {
///         _correlationId = correlationId;
///         _logger = logger;
///     }
/// 
///     public void DoWork()
///     {
///         _logger.LogInformation("Processing request {CorrelationId}", _correlationId.CorrelationId);
///     }
/// }
/// </code>
/// </example>
public interface ICorrelationIdAccessor
{
    /// <summary>
    /// Gets the current correlation ID for the operation.
    /// </summary>
    /// <value>
    /// The correlation ID string, or null if not set.
    /// </value>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the current causation ID (the ID of the operation that caused this one).
    /// </summary>
    /// <value>
    /// The causation ID string, or null if not set.
    /// </value>
    string? CausationId { get; }

    /// <summary>
    /// Gets a value indicating whether a correlation ID is currently set.
    /// </summary>
    bool HasCorrelationId { get; }
}

/// <summary>
/// Provides the ability to set the correlation ID for the current operation context.
/// </summary>
/// <remarks>
/// <para>
/// This interface is typically used by middleware to set the correlation ID
/// at the beginning of a request.
/// </para>
/// </remarks>
public interface ICorrelationIdSetter
{
    /// <summary>
    /// Sets the correlation ID for the current context.
    /// </summary>
    /// <param name="correlationId">The correlation ID to set.</param>
    void SetCorrelationId(string correlationId);

    /// <summary>
    /// Sets the causation ID for the current context.
    /// </summary>
    /// <param name="causationId">The causation ID to set.</param>
    void SetCausationId(string causationId);

    /// <summary>
    /// Creates a new correlation ID if one doesn't exist.
    /// </summary>
    /// <returns>The existing or newly created correlation ID.</returns>
    string EnsureCorrelationId();
}

/// <summary>
/// Combined interface for both reading and writing correlation IDs.
/// </summary>
public interface ICorrelationIdContext : ICorrelationIdAccessor, ICorrelationIdSetter
{
}

