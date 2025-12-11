//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Cqrs.EventSourcing;

/// <summary>
/// Exception thrown when an optimistic concurrency conflict is detected.
/// This typically occurs when attempting to append events to an event stream
/// with an incorrect expected version.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Handling Concurrency Conflicts:</strong>
/// <list type="bullet">
/// <item>Reload the aggregate and retry the operation</item>
/// <item>Merge changes if possible</item>
/// <item>Return an error to the user if manual resolution is needed</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// try
/// {
///     await repository.SaveAsync(order);
/// }
/// catch (ConcurrencyException ex)
/// {
///     // Reload and retry
///     var freshOrder = await repository.GetByIdAsync(order.Id);
///     freshOrder.ApplyChanges(...);
///     await repository.SaveAsync(freshOrder);
/// }
/// </code>
/// </example>
public class ConcurrencyException : Exception
{
    /// <summary>
    /// Gets the aggregate ID that caused the conflict.
    /// </summary>
    public Guid? AggregateId { get; }

    /// <summary>
    /// Gets the expected version.
    /// </summary>
    public long? ExpectedVersion { get; }

    /// <summary>
    /// Gets the actual version found.
    /// </summary>
    public long? ActualVersion { get; }

    /// <summary>
    /// Initializes a new instance with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ConcurrencyException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConcurrencyException(string message, Exception innerException) 
        : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance with detailed concurrency information.
    /// </summary>
    /// <param name="aggregateId">The aggregate ID.</param>
    /// <param name="expectedVersion">The expected version.</param>
    /// <param name="actualVersion">The actual version found.</param>
    public ConcurrencyException(Guid aggregateId, long expectedVersion, long actualVersion)
        : base($"Concurrency conflict for aggregate {aggregateId}: expected version {expectedVersion}, but found {actualVersion}")
    {
        AggregateId = aggregateId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

