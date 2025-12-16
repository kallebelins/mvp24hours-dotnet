//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Cqrs
{
    /// <summary>
    /// Interface for dispatching domain events in EF Core context.
    /// This is a lightweight abstraction that allows EF Core to dispatch events
    /// without a hard dependency on the full CQRS module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Purpose:</strong>
    /// This interface decouples the EF Core module from the CQRS module,
    /// allowing domain events to be dispatched without requiring the full
    /// mediator infrastructure.
    /// </para>
    /// <para>
    /// <strong>Implementations:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="NoOpDomainEventDispatcher"/>: Default no-op implementation</description></item>
    /// <item><description>Integration with Mvp24Hours.Infrastructure.Cqrs via adapter</description></item>
    /// </list>
    /// <para>
    /// <strong>Usage:</strong>
    /// Register your implementation in the DI container. The <see cref="UnitOfWorkWithEventsAsync"/>
    /// will use it to dispatch events after successful persistence.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register the CQRS adapter
    /// services.AddScoped&lt;IDomainEventDispatcherEFCore, CqrsDomainEventDispatcherAdapter&gt;();
    /// 
    /// // Or use the no-op for scenarios without event handling
    /// services.AddScoped&lt;IDomainEventDispatcherEFCore, NoOpDomainEventDispatcher&gt;();
    /// </code>
    /// </example>
    public interface IDomainEventDispatcherEFCore
    {
        /// <summary>
        /// Dispatches domain events from a single entity.
        /// </summary>
        /// <param name="entity">The entity containing domain events.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Events should be cleared from the entity after successful dispatch.
        /// </remarks>
        Task DispatchEventsAsync(IHasDomainEvents entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Dispatches domain events from multiple entities.
        /// </summary>
        /// <param name="entities">The entities containing domain events.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <remarks>
        /// Events should be cleared from entities after successful dispatch.
        /// Events are dispatched in the order they were collected.
        /// </remarks>
        Task DispatchEventsAsync(IEnumerable<IHasDomainEvents> entities, CancellationToken cancellationToken = default);
    }
}

