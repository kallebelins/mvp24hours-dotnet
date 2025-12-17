//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Asynchronous unified application service contract that combines query and command operations.
    /// This interface provides a single entry point for all async CRUD operations on an entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be managed by this service.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface combines <see cref="IQueryServiceAsync{TEntity}"/> and <see cref="ICommandServiceAsync{TEntity}"/>
    /// to provide a unified application service that handles both read and write operations asynchronously.
    /// </para>
    /// <para>
    /// Use this interface when you need a single service to handle all async operations for an entity.
    /// For more fine-grained control or CQRS patterns, consider using the separate query and command interfaces.
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class CustomerService : ApplicationServiceBaseAsync&lt;Customer, MyDbContext&gt;, IApplicationServiceAsync&lt;Customer&gt;
    /// {
    ///     public CustomerService(MyDbContext unitOfWork) : base(unitOfWork) { }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IQueryServiceAsync{TEntity}"/>
    /// <seealso cref="ICommandServiceAsync{TEntity}"/>
    /// <seealso cref="IReadOnlyApplicationServiceAsync{TEntity}"/>
    public interface IApplicationServiceAsync<TEntity> : IQueryServiceAsync<TEntity>, ICommandServiceAsync<TEntity>
        where TEntity : class
    {
    }
}

