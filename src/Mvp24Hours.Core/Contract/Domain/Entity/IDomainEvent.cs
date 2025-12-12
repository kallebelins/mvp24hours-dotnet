//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Base interface for domain events.
    /// Domain events represent something that happened in the domain that other parts of the system might be interested in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>What are Domain Events?</strong>
    /// Domain events capture the occurrence of something interesting in the domain.
    /// They are named in the past tense (OrderPlaced, UserRegistered, PaymentReceived)
    /// because they represent facts that have already occurred.
    /// </para>
    /// <para>
    /// <strong>Common Use Cases:</strong>
    /// <list type="bullet">
    /// <item>OrderPlaced, OrderShipped, OrderCancelled</item>
    /// <item>UserRegistered, UserEmailVerified</item>
    /// <item>PaymentReceived, PaymentFailed</item>
    /// <item>InventoryUpdated, StockLevelCritical</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This is a fundamental DDD interface. For Mediator integration,
    /// use the extended interface in <c>Mvp24Hours.Infrastructure.Cqrs</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Define a domain event
    /// public record OrderPlacedEvent : IDomainEvent
    /// {
    ///     public int OrderId { get; init; }
    ///     public string CustomerEmail { get; init; }
    ///     public decimal TotalAmount { get; init; }
    ///     public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    ///     public Guid EventId { get; init; } = Guid.NewGuid();
    /// }
    /// </code>
    /// </example>
    public interface IDomainEvent
    {
        /// <summary>
        /// Gets the timestamp when the event occurred.
        /// </summary>
        DateTime OccurredAt { get; }

        /// <summary>
        /// Gets the unique identifier for this event instance.
        /// Useful for idempotency and event tracking.
        /// </summary>
        Guid EventId { get; }
    }

    /// <summary>
    /// Base record for domain events with common properties.
    /// Provides a convenient base class with automatic timestamp and event ID.
    /// </summary>
    /// <example>
    /// <code>
    /// public record OrderPlacedEvent(int OrderId, decimal Amount) : DomainEventBase;
    /// 
    /// public record CustomerCreatedEvent : DomainEventBase
    /// {
    ///     public int CustomerId { get; init; }
    ///     public string Email { get; init; } = string.Empty;
    /// }
    /// </code>
    /// </example>
    public abstract record DomainEventBase : IDomainEvent
    {
        /// <inheritdoc />
        public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

        /// <inheritdoc />
        public Guid EventId { get; init; } = Guid.NewGuid();
    }
}

