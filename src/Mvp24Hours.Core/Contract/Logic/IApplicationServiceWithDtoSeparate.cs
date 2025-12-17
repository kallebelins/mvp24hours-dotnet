//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Application service contract with separate DTOs for create, update, and read operations.
    /// Provides a unified interface for CRUD operations with distinct DTO types for each operation.
    /// </summary>
    /// <typeparam name="TEntity">The entity type used for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type used for read operations (queries).</typeparam>
    /// <typeparam name="TCreateDto">The DTO type used for create operations.</typeparam>
    /// <typeparam name="TUpdateDto">The DTO type used for update operations.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface allows using different DTO structures for different operations,
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
    /// public class CustomerService : ApplicationServiceBaseWithSeparateDtos&lt;
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
    /// <seealso cref="IApplicationServiceWithDto{TEntity,TDto}"/>
    public interface IApplicationServiceWithSeparateDtos<TEntity, TDto, TCreateDto, TUpdateDto>
        where TEntity : class
        where TDto : class
        where TCreateDto : class
        where TUpdateDto : class
    {
        #region [ Query Operations ]

        /// <summary>
        /// Checks whether any records exist in the data source.
        /// </summary>
        /// <returns>A business result indicating whether any records exist.</returns>
        IBusinessResult<bool> ListAny();

        /// <summary>
        /// Gets the total count of records in the data source.
        /// </summary>
        /// <returns>A business result containing the count of records.</returns>
        IBusinessResult<int> ListCount();

        /// <summary>
        /// Gets all entities from the data source, mapped to read DTOs.
        /// </summary>
        /// <returns>A business result containing all entities as read DTOs.</returns>
        IBusinessResult<IList<TDto>> List();

        /// <summary>
        /// Gets all entities from the data source with paging criteria, mapped to read DTOs.
        /// </summary>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing entities as read DTOs matching the criteria.</returns>
        IBusinessResult<IList<TDto>> List(IPagingCriteria criteria);

        /// <summary>
        /// Checks whether any records match the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <returns>A business result indicating whether any matching records exist.</returns>
        IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets the count of records matching the specified filter.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <returns>A business result containing the count of matching records.</returns>
        IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets entities matching the specified filter, mapped to read DTOs.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <returns>A business result containing matching entities as read DTOs.</returns>
        IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets entities matching the specified filter with paging criteria, mapped to read DTOs.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing matching entities as read DTOs.</returns>
        IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria);

        /// <summary>
        /// Gets a single entity by its identifier, mapped to read DTO.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns>A business result containing the entity as read DTO, or null if not found.</returns>
        IBusinessResult<TDto> GetById(object id);

        /// <summary>
        /// Gets a single entity by its identifier with paging criteria, mapped to read DTO.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing the entity as read DTO, or null if not found.</returns>
        IBusinessResult<TDto> GetById(object id, IPagingCriteria criteria);

        #endregion

        #region [ Create Operations ]

        /// <summary>
        /// Adds a new entity from a create DTO.
        /// </summary>
        /// <param name="dto">The create DTO containing the data to add.</param>
        /// <returns>A business result containing the created entity as read DTO.</returns>
        IBusinessResult<TDto> Add(TCreateDto dto);

        /// <summary>
        /// Adds multiple entities from create DTOs.
        /// </summary>
        /// <param name="dtos">The list of create DTOs containing the data to add.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> Add(IList<TCreateDto> dtos);

        #endregion

        #region [ Update Operations ]

        /// <summary>
        /// Updates an existing entity from an update DTO.
        /// </summary>
        /// <param name="id">The entity identifier to update.</param>
        /// <param name="dto">The update DTO containing the updated data.</param>
        /// <returns>A business result containing the updated entity as read DTO.</returns>
        IBusinessResult<TDto> Modify(object id, TUpdateDto dto);

        /// <summary>
        /// Partially updates an existing entity from an update DTO (PATCH-style).
        /// Only non-null properties in the DTO will be applied to the entity.
        /// </summary>
        /// <param name="id">The entity identifier to update.</param>
        /// <param name="dto">The update DTO containing the partial data.</param>
        /// <returns>A business result containing the updated entity as read DTO.</returns>
        IBusinessResult<TDto> Patch(object id, TUpdateDto dto);

        #endregion

        #region [ Delete Operations ]

        /// <summary>
        /// Removes an entity by its identifier.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> RemoveById(object id);

        /// <summary>
        /// Removes multiple entities by their identifiers.
        /// </summary>
        /// <param name="ids">The list of entity identifiers.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> RemoveById(IList<object> ids);

        #endregion
    }

    /// <summary>
    /// Read-only application service contract with separate read DTO.
    /// Provides query-only access to entities with automatic Entity/DTO conversion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type used for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type used for read operations.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides read-only access with automatic DTO mapping,
    /// ideal for reporting services or read-side services in CQRS patterns.
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceWithSeparateDtos{TEntity,TDto,TCreateDto,TUpdateDto}"/>
    public interface IReadOnlyApplicationServiceWithSeparateDtos<TEntity, TDto>
        where TEntity : class
        where TDto : class
    {
        /// <summary>
        /// Checks whether any records exist in the data source.
        /// </summary>
        IBusinessResult<bool> ListAny();

        /// <summary>
        /// Gets the total count of records in the data source.
        /// </summary>
        IBusinessResult<int> ListCount();

        /// <summary>
        /// Gets all entities from the data source, mapped to read DTOs.
        /// </summary>
        IBusinessResult<IList<TDto>> List();

        /// <summary>
        /// Gets all entities from the data source with paging criteria, mapped to read DTOs.
        /// </summary>
        IBusinessResult<IList<TDto>> List(IPagingCriteria criteria);

        /// <summary>
        /// Checks whether any records match the specified filter.
        /// </summary>
        IBusinessResult<bool> GetByAny(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets the count of records matching the specified filter.
        /// </summary>
        IBusinessResult<int> GetByCount(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets entities matching the specified filter, mapped to read DTOs.
        /// </summary>
        IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets entities matching the specified filter with paging criteria, mapped to read DTOs.
        /// </summary>
        IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria);

        /// <summary>
        /// Gets a single entity by its identifier, mapped to read DTO.
        /// </summary>
        IBusinessResult<TDto> GetById(object id);

        /// <summary>
        /// Gets a single entity by its identifier with paging criteria, mapped to read DTO.
        /// </summary>
        IBusinessResult<TDto> GetById(object id, IPagingCriteria criteria);
    }
}

