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
    /// Asynchronous application service contract with separate DTOs for create, update, and read operations.
    /// Provides a unified async interface for CRUD operations with distinct DTO types for each operation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type used for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type used for read operations (queries).</typeparam>
    /// <typeparam name="TCreateDto">The DTO type used for create operations.</typeparam>
    /// <typeparam name="TUpdateDto">The DTO type used for update operations.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface allows using different DTO structures for different operations with full async support,
    /// which is common in real-world applications where:
    /// <list type="bullet">
    /// <item>Read DTOs may contain computed fields or nested data</item>
    /// <item>Create DTOs may exclude auto-generated fields (Id, CreatedAt)</item>
    /// <item>Update DTOs may only allow certain fields to be modified</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class CustomerService : ApplicationServiceBaseWithSeparateDtosAsync&lt;
    ///     Customer, 
    ///     CustomerDto,           // For reads - includes all fields
    ///     CreateCustomerDto,     // For creates - excludes Id, CreatedAt
    ///     UpdateCustomerDto,     // For updates - only editable fields
    ///     MyDbContext&gt;
    /// {
    ///     public CustomerService(MyDbContext unitOfWork, IMapper mapper) 
    ///         : base(unitOfWork, mapper) { }
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceWithDtoAsync{TEntity,TDto}"/>
    public interface IApplicationServiceWithSeparateDtosAsync<TEntity, TDto, TCreateDto, TUpdateDto>
        where TEntity : class
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
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
        /// Asynchronously gets all entities from the data source, mapped to read DTOs.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with all entities as read DTOs.</returns>
        Task<IBusinessResult<IList<TDto>>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all entities from the data source with paging criteria, mapped to read DTOs.
        /// </summary>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with entities as read DTOs matching the criteria.</returns>
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
        /// Asynchronously gets entities matching the specified filter, mapped to read DTOs.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with matching entities as read DTOs.</returns>
        Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets entities matching the specified filter with paging criteria, mapped to read DTOs.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with matching entities as read DTOs.</returns>
        Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier, mapped to read DTO.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the entity as read DTO, or null if not found.</returns>
        Task<IBusinessResult<TDto>> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier with paging criteria, mapped to read DTO.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the entity as read DTO, or null if not found.</returns>
        Task<IBusinessResult<TDto>> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default);

        #endregion

        #region [ Create Operations ]

        /// <summary>
        /// Asynchronously adds a new entity from a create DTO.
        /// </summary>
        /// <param name="dto">The create DTO containing the data to add.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the created entity as read DTO.</returns>
        Task<IBusinessResult<TDto>> AddAsync(TCreateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously adds multiple entities from create DTOs.
        /// </summary>
        /// <param name="dtos">The list of create DTOs containing the data to add.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the number of affected records.</returns>
        Task<IBusinessResult<int>> AddAsync(IList<TCreateDto> dtos, CancellationToken cancellationToken = default);

        #endregion

        #region [ Update Operations ]

        /// <summary>
        /// Asynchronously updates an existing entity from an update DTO.
        /// </summary>
        /// <param name="id">The entity identifier to update.</param>
        /// <param name="dto">The update DTO containing the updated data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the updated entity as read DTO.</returns>
        Task<IBusinessResult<TDto>> ModifyAsync(object id, TUpdateDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously partially updates an existing entity from an update DTO (PATCH-style).
        /// Only non-null properties in the DTO will be applied to the entity.
        /// </summary>
        /// <param name="id">The entity identifier to update.</param>
        /// <param name="dto">The update DTO containing the partial data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task containing a business result with the updated entity as read DTO.</returns>
        Task<IBusinessResult<TDto>> PatchAsync(object id, TUpdateDto dto, CancellationToken cancellationToken = default);

        #endregion

        #region [ Delete Operations ]

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
    /// Asynchronous read-only application service contract with separate read DTO.
    /// Provides async query-only access to entities with automatic Entity/DTO conversion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type used for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type used for read operations.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides async read-only access with automatic DTO mapping,
    /// ideal for reporting services or read-side services in CQRS patterns.
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceWithSeparateDtosAsync{TEntity,TDto,TCreateDto,TUpdateDto}"/>
    public interface IReadOnlyApplicationServiceWithSeparateDtosAsync<TEntity, TDto>
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
        /// Asynchronously gets all entities from the data source, mapped to read DTOs.
        /// </summary>
        Task<IBusinessResult<IList<TDto>>> ListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets all entities from the data source with paging criteria, mapped to read DTOs.
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
        /// Asynchronously gets entities matching the specified filter, mapped to read DTOs.
        /// </summary>
        Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets entities matching the specified filter with paging criteria, mapped to read DTOs.
        /// </summary>
        Task<IBusinessResult<IList<TDto>>> GetByAsync(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier, mapped to read DTO.
        /// </summary>
        Task<IBusinessResult<TDto>> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously gets a single entity by its identifier with paging criteria, mapped to read DTO.
        /// </summary>
        Task<IBusinessResult<TDto>> GetByIdAsync(object id, IPagingCriteria criteria, CancellationToken cancellationToken = default);
    }
}

