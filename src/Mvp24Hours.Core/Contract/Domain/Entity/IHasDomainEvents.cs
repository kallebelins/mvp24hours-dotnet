//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Interface for entities or aggregates that can raise domain events.
    /// Entities implementing this interface can accumulate domain events that will be
    /// dispatched after the entity is persisted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Pattern: Domain Event Publishing</strong>
    /// </para>
    /// <para>
    /// Domain events are raised by entities/aggregates during state changes but are not
    /// immediately published. Instead, they are accumulated and dispatched after the 
    /// changes are successfully persisted to the database. This ensures:
    /// <list type="bullet">
    /// <item>Events are only published for successful transactions</item>
    /// <item>Events contain the final state of the entity</item>
    /// <item>Handlers can rely on data being committed</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Best Practice:</strong> Clear domain events after successful dispatch
    /// to prevent duplicate processing.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Order : EntityBase&lt;int&gt;, IHasDomainEvents
    /// {
    ///     private readonly List&lt;IDomainEvent&gt; _domainEvents = new();
    ///     
    ///     public IReadOnlyCollection&lt;IDomainEvent&gt; DomainEvents => _domainEvents.AsReadOnly();
    ///     
    ///     public void ClearDomainEvents() => _domainEvents.Clear();
    ///     
    ///     protected void RaiseDomainEvent(IDomainEvent domainEvent)
    ///     {
    ///         _domainEvents.Add(domainEvent);
    ///     }
    ///     
    ///     public void Place()
    ///     {
    ///         if (Status != OrderStatus.Draft)
    ///             throw new InvalidOperationException("Only draft orders can be placed.");
    ///             
    ///         Status = OrderStatus.Placed;
    ///         PlacedAt = DateTime.UtcNow;
    ///         
    ///         // Raise domain event
    ///         RaiseDomainEvent(new OrderPlacedEvent(Id, CustomerEmail, TotalAmount));
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IHasDomainEvents
    {
        /// <summary>
        /// Gets the collection of domain events raised by this entity.
        /// </summary>
        /// <remarks>
        /// Returns a read-only view of the pending domain events.
        /// Events should be dispatched after the entity is persisted.
        /// </remarks>
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

        /// <summary>
        /// Clears all pending domain events.
        /// </summary>
        /// <remarks>
        /// This method should be called after domain events have been successfully dispatched
        /// to prevent duplicate processing.
        /// </remarks>
        void ClearDomainEvents();
    }
}

