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
    /// Application service contract with automatic DTO mapping support.
    /// Provides a unified interface for CRUD operations with automatic Entity/DTO conversion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type used for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type used for data transfer.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface extends the basic application service contract to include automatic
    /// mapping between entities and DTOs. It uses AutoMapper internally to handle conversions.
    /// </para>
    /// <para>
    /// <strong>Key Features:</strong>
    /// <list type="bullet">
    /// <item>Automatic Entity ↔ DTO mapping via AutoMapper</item>
    /// <item>All query operations return DTOs instead of entities</item>
    /// <item>Command operations accept DTOs and convert to entities internally</item>
    /// <item>Clean separation between domain entities and API contracts</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Example usage:</strong>
    /// <code>
    /// public class CustomerService : ApplicationServiceBaseWithDto&lt;Customer, CustomerDto, MyDbContext&gt;
    /// {
    ///     public CustomerService(MyDbContext unitOfWork, IMapper mapper) : base(unitOfWork, mapper) { }
    /// }
    /// 
    /// // In controller:
    /// var customers = await _customerService.ListAsync(); // Returns IBusinessResult&lt;IList&lt;CustomerDto&gt;&gt;
    /// </code>
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationService{TEntity}"/>
    /// <seealso cref="IReadOnlyApplicationServiceWithDto{TEntity,TDto}"/>
    public interface IApplicationServiceWithDto<TEntity, TDto>
        where TEntity : class
        where TDto : class
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
        /// Gets all entities from the data source, mapped to DTOs.
        /// </summary>
        /// <returns>A business result containing all entities as DTOs.</returns>
        IBusinessResult<IList<TDto>> List();

        /// <summary>
        /// Gets all entities from the data source with paging criteria, mapped to DTOs.
        /// </summary>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing entities as DTOs matching the criteria.</returns>
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
        /// Gets entities matching the specified filter, mapped to DTOs.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <returns>A business result containing matching entities as DTOs.</returns>
        IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets entities matching the specified filter with paging criteria, mapped to DTOs.
        /// </summary>
        /// <param name="clause">The filter expression (on entity type).</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing matching entities as DTOs.</returns>
        IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria);

        /// <summary>
        /// Gets a single entity by its identifier, mapped to DTO.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <returns>A business result containing the entity as DTO, or null if not found.</returns>
        IBusinessResult<TDto> GetById(object id);

        /// <summary>
        /// Gets a single entity by its identifier with paging criteria, mapped to DTO.
        /// </summary>
        /// <param name="id">The entity identifier.</param>
        /// <param name="criteria">The paging criteria for the query.</param>
        /// <returns>A business result containing the entity as DTO, or null if not found.</returns>
        IBusinessResult<TDto> GetById(object id, IPagingCriteria criteria);

        #endregion

        #region [ Command Operations ]

        /// <summary>
        /// Adds a new entity from a DTO.
        /// </summary>
        /// <param name="dto">The DTO containing the data to add.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> Add(TDto dto);

        /// <summary>
        /// Adds multiple entities from DTOs.
        /// </summary>
        /// <param name="dtos">The list of DTOs containing the data to add.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> Add(IList<TDto> dtos);

        /// <summary>
        /// Updates an existing entity from a DTO.
        /// </summary>
        /// <param name="dto">The DTO containing the updated data.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> Modify(TDto dto);

        /// <summary>
        /// Updates multiple entities from DTOs.
        /// </summary>
        /// <param name="dtos">The list of DTOs containing the updated data.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> Modify(IList<TDto> dtos);

        /// <summary>
        /// Removes an entity identified by the DTO.
        /// </summary>
        /// <param name="dto">The DTO identifying the entity to remove.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> Remove(TDto dto);

        /// <summary>
        /// Removes multiple entities identified by DTOs.
        /// </summary>
        /// <param name="dtos">The list of DTOs identifying the entities to remove.</param>
        /// <returns>A business result containing the number of affected records.</returns>
        IBusinessResult<int> Remove(IList<TDto> dtos);

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
    /// Read-only application service contract with automatic DTO mapping support.
    /// Provides query-only access to entities with automatic Entity/DTO conversion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type used for persistence.</typeparam>
    /// <typeparam name="TDto">The DTO type used for data transfer.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides read-only access with automatic DTO mapping,
    /// ideal for reporting services or read-side services in CQRS patterns.
    /// </para>
    /// </remarks>
    /// <seealso cref="IApplicationServiceWithDto{TEntity,TDto}"/>
    /// <seealso cref="IReadOnlyApplicationService{TEntity}"/>
    public interface IReadOnlyApplicationServiceWithDto<TEntity, TDto>
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
        /// Gets all entities from the data source, mapped to DTOs.
        /// </summary>
        IBusinessResult<IList<TDto>> List();

        /// <summary>
        /// Gets all entities from the data source with paging criteria, mapped to DTOs.
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
        /// Gets entities matching the specified filter, mapped to DTOs.
        /// </summary>
        IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause);

        /// <summary>
        /// Gets entities matching the specified filter with paging criteria, mapped to DTOs.
        /// </summary>
        IBusinessResult<IList<TDto>> GetBy(Expression<Func<TEntity, bool>> clause, IPagingCriteria criteria);

        /// <summary>
        /// Gets a single entity by its identifier, mapped to DTO.
        /// </summary>
        IBusinessResult<TDto> GetById(object id);

        /// <summary>
        /// Gets a single entity by its identifier with paging criteria, mapped to DTO.
        /// </summary>
        IBusinessResult<TDto> GetById(object id, IPagingCriteria criteria);
    }
}

