//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Unified application service contract that combines query and command operations.
    /// This interface provides a single entry point for all CRUD operations on an entity.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be managed by this service.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface combines <see cref="IQueryService{TEntity}"/> and <see cref="ICommandService{TEntity}"/>
    /// to provide a unified application service that handles both read and write operations.
    /// </para>
    /// <para>
    /// Use this interface when you need a single service to handle all operations for an entity.
    /// For more fine-grained control or CQRS patterns, consider using the separate query and command interfaces.
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class CustomerService : ApplicationServiceBase&lt;Customer, MyDbContext&gt;, IApplicationService&lt;Customer&gt;
    /// {
    ///     public CustomerService(MyDbContext unitOfWork) : base(unitOfWork) { }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IQueryService{TEntity}"/>
    /// <seealso cref="ICommandService{TEntity}"/>
    /// <seealso cref="IReadOnlyApplicationService{TEntity}"/>
    public interface IApplicationService<TEntity> : IQueryService<TEntity>, ICommandService<TEntity>
        where TEntity : class
    {
    }
}

