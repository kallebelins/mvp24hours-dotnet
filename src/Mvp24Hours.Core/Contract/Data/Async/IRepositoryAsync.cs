//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;

namespace Mvp24Hours.Core.Contract.Data;

/// <summary>
/// Design Pattern: Repository
/// Description: Mediation between domain and data mapping layers using a collection as 
/// an interface for accessing domain objects. (Martin Fowler)
/// Learn more: http://martinfowler.com/eaaCatalog/repository.html
/// </summary>
/// <typeparam name="T">Represents an entity</typeparam>
public interface IRepositoryAsync<T> : IQueryAsync<T>, ICommandAsync<T>, IQueryRelationAsync<T>
    where T : IEntityBase
{
}
/// <summary>
/// Asynchronous repository with a strongly-typed entity identifier.
/// </summary>
/// <typeparam name="T">Represents an entity</typeparam>
/// <typeparam name="TId">Type of the entity identifier, as declared by <see cref="IEntity{TId}"/></typeparam>
/// <remarks>
/// <para>
/// This contract is optional and purely additive. It inherits the whole surface of
/// <see cref="IRepositoryAsync{T}"/> and only adds identifier-based members that take
/// <typeparamref name="TId"/> instead of <see cref="object"/>, so the compiler catches
/// a wrong identifier type and the caller avoids boxing at the call site.
/// </para>
/// <para>
/// The untyped members (<c>GetByIdAsync(object, ...)</c>, <c>RemoveByIdAsync(object, ...)</c>)
/// remain the real implementation and remain fully supported: the typed members delegate to
/// them. Existing code using <see cref="IRepositoryAsync{T}"/> does not need to change.
/// </para>
/// <para>
/// Resolve this contract directly from the container. <c>IUnitOfWorkAsync.GetRepository&lt;T&gt;()</c>
/// has a single type parameter and therefore always returns <see cref="IRepositoryAsync{T}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class CustomerStore(IRepositoryAsync&lt;Customer, int&gt; repository)
/// {
///     public Task&lt;Customer?&gt; FindAsync(int id, CancellationToken ct) =&gt;
///         repository.GetByIdAsync(id, ct);
/// }
/// </code>
/// </example>
public interface IRepositoryAsync<T, TId> : IRepositoryAsync<T>
    where T : IEntityBase, IEntity<TId>
{
    /// <summary>
    /// Gets an entity by its strongly-typed identifier.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The entity, or <c>null</c> when not found</returns>
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an entity by its strongly-typed identifier, applying paging criteria
    /// (used for navigation/relation loading).
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="criteria">Paging, ordering and navigation criteria</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The entity, or <c>null</c> when not found</returns>
    Task<T?> GetByIdAsync(TId id, IPagingCriteria? criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an entity by its strongly-typed identifier.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="cancellationToken"></param>
    Task RemoveByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a set of entities by their strongly-typed identifiers.
    /// </summary>
    /// <param name="ids">Entity identifiers</param>
    /// <param name="cancellationToken"></param>
    Task RemoveByIdAsync(IList<TId> ids, CancellationToken cancellationToken = default);
}
