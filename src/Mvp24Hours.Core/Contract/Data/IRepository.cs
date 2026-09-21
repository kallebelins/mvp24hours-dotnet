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
public interface IRepository<T> : IQuery<T>, ICommand<T>, IQueryRelation<T>
    where T : IEntityBase
{
}
/// <summary>
/// Repository with a strongly-typed entity identifier.
/// </summary>
/// <typeparam name="T">Represents an entity</typeparam>
/// <typeparam name="TId">Type of the entity identifier, as declared by <see cref="IEntity{TId}"/></typeparam>
/// <remarks>
/// <para>
/// This contract is optional and purely additive. It inherits the whole surface of
/// <see cref="IRepository{T}"/> and only adds identifier-based members that take
/// <typeparamref name="TId"/> instead of <see cref="object"/>, so the compiler catches
/// a wrong identifier type and the caller avoids boxing at the call site.
/// </para>
/// <para>
/// The untyped members (<c>GetById(object)</c>, <c>RemoveById(object)</c>) remain the
/// real implementation and remain fully supported: the typed members delegate to them.
/// Existing code using <see cref="IRepository{T}"/> does not need to change.
/// </para>
/// <para>
/// Resolve this contract directly from the container. <c>IUnitOfWork.GetRepository&lt;T&gt;()</c>
/// has a single type parameter and therefore always returns <see cref="IRepository{T}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public sealed class CustomerStore(IRepository&lt;Customer, int&gt; repository)
/// {
///     public Customer? Find(int id) => repository.GetById(id);
/// }
/// </code>
/// </example>
public interface IRepository<T, TId> : IRepository<T>
    where T : IEntityBase, IEntity<TId>
{
    /// <summary>
    /// Gets an entity by its strongly-typed identifier.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <returns>The entity, or <c>null</c> when not found</returns>
    T? GetById(TId id);

    /// <summary>
    /// Gets an entity by its strongly-typed identifier, applying paging criteria
    /// (used for navigation/relation loading).
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <param name="criteria">Paging, ordering and navigation criteria</param>
    /// <returns>The entity, or <c>null</c> when not found</returns>
    T? GetById(TId id, IPagingCriteria? criteria);

    /// <summary>
    /// Removes an entity by its strongly-typed identifier.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    void RemoveById(TId id);

    /// <summary>
    /// Removes a set of entities by their strongly-typed identifiers.
    /// </summary>
    /// <param name="ids">Entity identifiers</param>
    void RemoveById(IList<TId> ids);
}
