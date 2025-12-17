//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Asynchronous application service contract with automatic DTO mapping support.
    /// Provides a unified interface for async CRUD operations with automatic Entity/DTO conversion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type used for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type used for data transfer.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface extends the basic application service contract to include automatic
    /// mapping between entities and DTOs with full async support.
    /// </para>
    /// <para>
    /// <strong>Key Features:</strong>
    /// <list type="bullet">
    /// <item>Automatic Entity ↔ DTO mapping via AutoMapper</item>
    /// <item>All query operations return DTOs instead of entities</item>
    /// <item>Command operations accept DTOs and convert to entities internally</item>
    /// <item>Full async/await support with CancellationToken</item>
    /// <item>Clean separation between domain entities and API contracts</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class CustomerService : ApplicationServiceBaseWithDtoAsync&lt;Customer, CustomerDto, MyDbContext&gt;
    /// {
    ///     public CustomerService(MyDbContext unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    /// }
    /// 
    /// // In controller:
    /// var customers = await _customerService.ListAsync(); // Returns Task&lt;IBusinessResult&lt;IList&lt;CustomerDto&gt;&gt;&gt;
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceAsync{TEntity}"/>
    /// <seealso cref="IReadOnlyApplicationServiceWithDtoAsync{TEntity,TDto}"/>
    public interface IApplicationServiceWithDtoAsync<TEntity, TDto>
        where TEntity : class
        where TDto : class
    {
        #region [ Query Operations ]

        /// <summary>
        /// Asynchronously checks whether any records exist in the data source.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result indicating whether any records exist.</returns>
        Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the total count of records in the data source.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the count of records.</returns>
        Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all entities from the data source, mapped to DTOs.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with all entities as DTOs.</returns>
        Task<IBusinessResult<IList<TDto>>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all entities from the data source with paging criteria, mapped to DTOs.
        /// </summary>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with entities as DTOs matching the criteria.</returns>
        Task<IBusinessResult<IList<TDto>>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously checks whether any records match the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result indicating whether any matching records exist.</returns>
        Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the count of records matching the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the count of matching records.</returns>
        Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets entities matching the specified filter, mapped to DTOs.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with matching entities as DTOs.</returns>
        Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets entities matching the specified filter with paging criteria, mapped to DTOs.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with matching entities as DTOs.</returns>
        Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier, mapped to DTO.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the entity as DTO, or null if not found.</returns>
        Task<IBusinessResult<TDto>> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier with paging criteria, mapped to DTO.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the entity as DTO, or null if not found.</returns>
        Task<IBusinessResult<TDto>> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default);

        #endregion

        #region [ Command Operations ]

        /// <summary>
        /// Asynchronously adds a new entity from a DTO.
        /// </summary>
        /// <param name="dto">The DTO containing the data to add.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> AddAsync(TDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously adds multiple entities from DTOs.
        /// </summary>
        /// <param name="dtos">The list of DTOs containing the data to add.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> AddAsync(IList<TDto> dtos, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates an existing entity from a DTO.
        /// </summary>
        /// <param name="dto">The DTO containing the updated data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> ModifyAsync(TDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates multiple entities from DTOs.
        /// </summary>
        /// <param name="dtos">The list of DTOs containing the updated data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> ModifyAsync(IList<TDto> dtos, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes an entity identified by the DTO.
        /// </summary>
        /// <param name="dto">The DTO identifying the entity to remove.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> RemoveAsync(TDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes multiple entities identified by DTOs.
        /// </summary>
        /// <param name="dtos">The list of DTOs identifying the entities to remove.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> RemoveAsync(IList<TDto> dtos, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes an entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> RemoveByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes multiple entities by their identifiers.
        /// </summary>
        /// <param name="ids">The list of entity identifiers.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> RemoveByIdAsync(IList<object> ids, CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// Asynchronous read-only application service contract with automatic DTO mapping support.
    /// Provides async query-only access to entities with automatic Entity/DTO conversion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type used for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type used for data transfer.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides async read-only access with automatic DTO mapping,
    /// ideal for reporting services or read-side services in CQRS patterns.
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceWithDtoAsync{TEntity,TDto}"/>
    /// <seealso cref="IReadOnlyApplicationServiceAsync{TEntity}"/>
    public interface IReadOnlyApplicationServiceWithDtoAsync<TEntity, TDto>
        where TEntity : class
        where TDto : class
    {
        /// <summary>
        /// Asynchronously checks whether any records exist in the data source.
        /// </summary>
        Task<IBusinessResult<bool>> ListAnyAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the total count of records in the data source.
        /// </summary>
        Task<IBusinessResult<int>> ListCountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all entities from the data source, mapped to DTOs.
        /// </summary>
        Task<IBusinessResult<IList<TDto>>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all entities from the data source with paging criteria, mapped to DTOs.
        /// </summary>
        Task<IBusinessResult<IList<TDto>>> ListAsync(IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously checks whether any records match the specified filter.
        /// </summary>
        Task<IBusinessResult<bool>> GetByAnyAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets the count of records matching the specified filter.
        /// </summary>
        Task<IBusinessResult<int>> GetByCountAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets entities matching the specified filter, mapped to DTOs.
        /// </summary>
        Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets entities matching the specified filter with paging criteria, mapped to DTOs.
        /// </summary>
        Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier, mapped to DTO.
        /// </summary>
        Task<IBusinessResult<TDto>> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier with paging criteria, mapped to DTO.
        /// </summary>
        Task<IBusinessResult<TDto>> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default);
    }
}

