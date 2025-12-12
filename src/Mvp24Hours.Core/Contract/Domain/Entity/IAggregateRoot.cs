//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Core.Contract.Domain.Entity
{
    /// <summary>
    /// Marker interface for Aggregate Roots in Domain-Driven Design.
    /// An Aggregate Root is the entry point to an aggregate - a cluster of domain objects
    /// that can be treated as a single unit for data changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Aggregate Root Principles:</strong>
    /// <list type="bullet">
    /// <item>Only aggregate roots can be loaded directly from a repository</item>
    /// <item>External objects can only reference the aggregate root, not internal entities</item>
    /// <item>All state changes to the aggregate go through the root</item>
    /// <item>The root ensures all invariants are enforced</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>When to use:</strong>
    /// Use this interface to mark entities that serve as aggregate roots.
    /// This helps enforce architectural boundaries and DDD patterns.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Order : EntityBase&lt;Guid&gt;, IAggregateRoot, IHasDomainEvents
    /// {
    ///     private readonly List&lt;OrderItem&gt; _items = new();
    ///     private readonly List&lt;IDomainEvent&gt; _domainEvents = new();
    ///     
    ///     public IReadOnlyList&lt;OrderItem&gt; Items => _items.AsReadOnly();
    ///     public OrderStatus Status { get; private set; }
    ///     
    ///     public IReadOnlyCollection&lt;IDomainEvent&gt; DomainEvents => _domainEvents.AsReadOnly();
    ///     public void ClearDomainEvents() => _domainEvents.Clear();
    ///     
    ///     // All modifications go through the aggregate root
    ///     public void AddItem(Product product, int quantity)
    ///     {
    ///         // Enforce business rules
    ///         if (Status != OrderStatus.Draft)
    ///             throw new InvalidOperationException("Cannot modify a placed order.");
    ///             
    ///         _items.Add(new OrderItem(product.Id, product.Price, quantity));
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IAggregateRoot : IEntityBase
    {
    }

    /// <summary>
    /// Generic interface for Aggregate Roots with typed identifier.
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
    /// <example>
    /// <code>
    /// public class Order : EntityBase&lt;Guid&gt;, IAggregateRoot&lt;Guid&gt;
    /// {
    ///     // Order implementation
    /// }
    /// </code>
    /// </example>
    public interface IAggregateRoot<TId> : IAggregateRoot
    {
        /// <summary>
        /// Gets the typed identifier of the aggregate.
        /// </summary>
        TId Id { get; }
    }

    /// <summary>
    /// Interface for aggregates that support versioning/optimistic concurrency.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Versioning is essential for:
    /// <list type="bullet">
    /// <item>Optimistic concurrency control in databases</item>
    /// <item>Event sourcing version tracking</item>
    /// <item>Conflict detection in distributed systems</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IVersionedAggregate : IAggregateRoot
    {
        /// <summary>
        /// Gets the current version of the aggregate.
        /// This value is typically incremented on each modification.
        /// </summary>
        long Version { get; }
    }

    /// <summary>
    /// Generic interface for versioned aggregates with typed identifier.
    /// </summary>
    /// <typeparam name="TId">The type of the aggregate identifier.</typeparam>
    public interface IVersionedAggregate<TId> : IAggregateRoot<TId>, IVersionedAggregate
    {
    }
}

