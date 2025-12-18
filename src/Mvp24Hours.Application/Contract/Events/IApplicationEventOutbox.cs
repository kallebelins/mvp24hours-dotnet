//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Contract.Events;

/// <summary>
/// Interface for the Application Event Outbox.
/// Implements the Outbox Pattern for reliable event dispatching.
/// </summary>
/// <remarks>
/// <para>
/// The Outbox Pattern ensures that application events are reliably dispatched
/// even if the application crashes or restarts.
/// </para>
/// <para>
/// <strong>How it works:</strong>
/// <list type="number">
/// <item>Events are stored in an outbox (database table) within the same transaction as the data changes</item>
/// <item>A background process reads pending events from the outbox</item>
/// <item>Events are dispatched to their handlers</item>
/// <item>Successfully dispatched events are marked as processed</item>
/// </list>
/// </para>
/// <para>
/// <strong>Benefits:</strong>
/// <list type="bullet">
/// <item>Guaranteed event delivery (at-least-once)</item>
/// <item>Atomic consistency between data changes and events</item>
/// <item>Resilience to failures during event dispatch</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In your application service
/// public async Task&lt;IBusinessResult&lt;int&gt;&gt; CreateCustomerAsync(Customer customer, CancellationToken ct)
/// {
///     await _repository.AddAsync(customer, ct);
///     
///     // Add event to outbox (will be dispatched later)
///     await _outbox.AddAsync(new EntityCreatedEvent&lt;Customer&gt;(customer)
///     {
///         CorrelationId = _correlationIdAccessor.CorrelationId
///     }, ct);
///     
///     return await _unitOfWork.SaveChangesAsync(ct).ToBusinessAsync();
/// }
/// </code>
/// </example>
public interface IApplicationEventOutbox
{
    /// <summary>
    /// Adds an application event to the outbox for later dispatching.
    /// </summary>
    /// <typeparam name="TEvent">The type of application event.</typeparam>
    /// <param name="event">The event to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IApplicationEvent;

    /// <summary>
    /// Adds multiple application events to the outbox.
    /// </summary>
    /// <param name="events">The events to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AddRangeAsync(IEnumerable<IApplicationEvent> events, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets pending (undispatched) events from the outbox.
    /// </summary>
    /// <param name="batchSize">Maximum number of events to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of pending outbox entries.</returns>
    Task<IReadOnlyList<ApplicationEventOutboxEntry>> GetPendingAsync(
        int batchSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an event as successfully dispatched.
    /// </summary>
    /// <param name="entryId">The ID of the outbox entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsDispatchedAsync(Guid entryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an event as failed to dispatch.
    /// </summary>
    /// <param name="entryId">The ID of the outbox entry.</param>
    /// <param name="error">The error message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkAsFailedAsync(Guid entryId, string error, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up old dispatched events.
    /// </summary>
    /// <param name="olderThan">Delete entries dispatched before this date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entries deleted.</returns>
    Task<int> CleanupAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an entry in the application event outbox.
/// </summary>
public sealed class ApplicationEventOutboxEntry
{
    /// <summary>
    /// Unique identifier for the entry.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The fully qualified type name of the event.
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// Serialized event payload (JSON).
    /// </summary>
    public string Payload { get; init; } = string.Empty;

    /// <summary>
    /// When the entry was created.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// When the entry was dispatched (if any).
    /// </summary>
    public DateTime? DispatchedAt { get; set; }

    /// <summary>
    /// Current status of the entry.
    /// </summary>
    public ApplicationEventOutboxStatus Status { get; set; } = ApplicationEventOutboxStatus.Pending;

    /// <summary>
    /// Number of dispatch retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Error message if dispatch failed.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Correlation ID for tracing.
    /// </summary>
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Status of an application event outbox entry.
/// </summary>
public enum ApplicationEventOutboxStatus
{
    /// <summary>
    /// Entry is pending dispatch.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Entry has been successfully dispatched.
    /// </summary>
    Dispatched = 1,

    /// <summary>
    /// Entry failed to dispatch (will be retried).
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Entry has exceeded retry limit and moved to dead letter.
    /// </summary>
    DeadLetter = 3
}

