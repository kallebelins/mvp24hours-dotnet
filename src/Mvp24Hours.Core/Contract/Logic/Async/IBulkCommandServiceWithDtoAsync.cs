//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Core.Contract.Logic
{
    /// <summary>
    /// Interface for bulk command operations with DTO support on application services.
    /// </summary>
    /// <typeparam name="TDto">The DTO type used for data transfer.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides high-performance bulk operations with automatic DTO-to-Entity mapping,
    /// complementing the standard CRUD operations from <see cref="ICommandServiceAsync{TEntity}"/>.
    /// </para>
    /// <para>
    /// <strong>Features:</strong>
    /// <list type="bullet">
    /// <item>Automatic DTO to Entity mapping via AutoMapper</item>
    /// <item>Validation of DTOs before bulk operation</item>
    /// <item>Progress callback for long-running operations</item>
    /// <item>Configurable batch size and timeout</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerBulkService : BulkApplicationServiceWithDtoBaseAsync&lt;Customer, CustomerDto, MyDbContext&gt;
    /// {
    ///     public async Task ImportFromCsvAsync(IList&lt;CustomerDto&gt; customers, CancellationToken ct)
    ///     {
    ///         var options = new BulkOperationOptions
    ///         {
    ///             BatchSize = 5000,
    ///             ProgressCallback = (processed, total) =&gt; 
    ///                 Console.WriteLine($"Importing: {processed}/{total}")
    ///         };
    ///         
    ///         var result = await BulkAddAsync(customers, options, ct);
    ///         
    ///         Console.WriteLine($"Imported {result.Data.RowsAffected} records in {result.Data.ElapsedTime}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IBulkCommandServiceWithDtoAsync<TDto>
        where TDto : class
    {
        #region Bulk Add

        /// <summary>
        /// Asynchronously adds a large collection of entities from DTOs using optimized bulk operations.
        /// </summary>
        /// <param name="dtos">The collection of DTOs to add.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        /// <remarks>
        /// <para>
        /// DTOs are validated before mapping to entities. If any DTO fails validation,
        /// the entire operation is aborted and validation errors are returned.
        /// </para>
        /// </remarks>
        Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TDto> dtos,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously adds a large collection of entities from DTOs using optimized bulk operations with custom options.
        /// </summary>
        /// <param name="dtos">The collection of DTOs to add.</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Modify

        /// <summary>
        /// Asynchronously updates a large collection of entities from DTOs using optimized bulk operations.
        /// </summary>
        /// <param name="dtos">The collection of DTOs containing updated data (identified by primary key).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Entities are matched by their primary key (mapped from DTO).
        /// All non-key properties are updated with values from the DTO.
        /// </para>
        /// </remarks>
        Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TDto> dtos,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates a large collection of entities from DTOs using optimized bulk operations with custom options.
        /// </summary>
        /// <param name="dtos">The collection of DTOs containing updated data (identified by primary key).</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Remove

        /// <summary>
        /// Asynchronously removes a large collection of entities identified by DTOs using optimized bulk operations.
        /// </summary>
        /// <param name="dtos">The collection of DTOs identifying entities to remove (by primary key).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(
            IList<TDto> dtos,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes a large collection of entities identified by DTOs using optimized bulk operations with custom options.
        /// </summary>
        /// <param name="dtos">The collection of DTOs identifying entities to remove (by primary key).</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(
            IList<TDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion
    }

    /// <summary>
    /// Interface for bulk command operations with separate Create/Update DTO support on application services.
    /// </summary>
    /// <typeparam name="TCreateDto">The DTO type for create operations.</typeparam>
    /// <typeparam name="TUpdateDto">The DTO type for update operations.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface supports the pattern of using different DTOs for create and update operations,
    /// which is common in RESTful APIs where POST and PUT/PATCH have different payload structures.
    /// </para>
    /// </remarks>
    public interface IBulkCommandServiceWithSeparateDtosAsync<TCreateDto, TUpdateDto>
        where TCreateDto : class
        where TUpdateDto : class
    {
        #region Bulk Add

        /// <summary>
        /// Asynchronously adds a large collection of entities from create DTOs using optimized bulk operations.
        /// </summary>
        /// <param name="dtos">The collection of create DTOs to add.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TCreateDto> dtos,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously adds a large collection of entities from create DTOs using optimized bulk operations with custom options.
        /// </summary>
        /// <param name="dtos">The collection of create DTOs to add.</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TCreateDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Modify

        /// <summary>
        /// Asynchronously updates a large collection of entities from update DTOs using optimized bulk operations.
        /// </summary>
        /// <param name="dtos">The collection of update DTOs with entity IDs and updated data.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TUpdateDto> dtos,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates a large collection of entities from update DTOs using optimized bulk operations with custom options.
        /// </summary>
        /// <param name="dtos">The collection of update DTOs with entity IDs and updated data.</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TUpdateDto> dtos,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion
    }
}

