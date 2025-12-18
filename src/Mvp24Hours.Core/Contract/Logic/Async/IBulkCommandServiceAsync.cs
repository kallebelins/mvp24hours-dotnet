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
    /// Interface for bulk command operations on application services.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be managed by this service.</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides high-performance bulk operations that complement
    /// the standard CRUD operations from <see cref="ICommandServiceAsync{TEntity}"/>.
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Importing large datasets (CSV, Excel, API)</item>
    /// <item>Batch processing of entities</item>
    /// <item>Data migration scenarios</item>
    /// <item>Synchronization with external systems</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// <list type="bullet">
    /// <item>Bulk operations bypass EF Core change tracking for better performance</item>
    /// <item>Use BatchSize option to control memory usage</item>
    /// <item>Progress callback available for long-running operations</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerBulkService : BulkApplicationServiceBaseAsync&lt;Customer, MyDbContext&gt;
    /// {
    ///     public async Task ImportCustomersAsync(IList&lt;Customer&gt; customers, CancellationToken ct)
    ///     {
    ///         var options = new BulkOperationOptions
    ///         {
    ///             BatchSize = 2000,
    ///             ProgressCallback = (processed, total) =&gt; 
    ///                 _logger.LogInformation("Imported {Processed}/{Total}", processed, total)
    ///         };
    ///         
    ///         var result = await BulkAddAsync(customers, options, ct);
    ///         
    ///         if (!result.Data.IsSuccess)
    ///             throw new ApplicationException(result.Data.ErrorMessage);
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface IBulkCommandServiceAsync<TEntity>
        where TEntity : class
    {
        #region Bulk Add

        /// <summary>
        /// Asynchronously adds a large collection of entities using optimized bulk operations.
        /// </summary>
        /// <param name="entities">The collection of entities to add.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method uses default options (BatchSize=1000, BypassChangeTracking=true).
        /// For custom configuration, use the overload that accepts <see cref="BulkOperationOptions"/>.
        /// </para>
        /// </remarks>
        Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously adds a large collection of entities using optimized bulk operations with custom options.
        /// </summary>
        /// <param name="entities">The collection of entities to add.</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Use this overload to:
        /// <list type="bullet">
        /// <item>Set custom batch size for memory management</item>
        /// <item>Register progress callback for monitoring</item>
        /// <item>Configure timeout for long operations</item>
        /// <item>Control transaction behavior</item>
        /// </list>
        /// </para>
        /// </remarks>
        Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Modify

        /// <summary>
        /// Asynchronously updates a large collection of entities using optimized bulk operations.
        /// </summary>
        /// <param name="entities">The collection of entities to update (identified by primary key).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Entities are matched by their primary key. All non-key properties are updated.
        /// Use <see cref="BulkOperationOptions"/> for custom configuration.
        /// </para>
        /// </remarks>
        Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously updates a large collection of entities using optimized bulk operations with custom options.
        /// </summary>
        /// <param name="entities">The collection of entities to update (identified by primary key).</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion

        #region Bulk Remove

        /// <summary>
        /// Asynchronously removes a large collection of entities using optimized bulk operations.
        /// </summary>
        /// <param name="entities">The collection of entities to remove (identified by primary key).</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This performs a physical delete. For soft delete, use the standard
        /// ModifyAsync method or bulk update operation.
        /// </para>
        /// </remarks>
        Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously removes a large collection of entities using optimized bulk operations with custom options.
        /// </summary>
        /// <param name="entities">The collection of entities to remove (identified by primary key).</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>
        /// A task containing a business result with <see cref="BulkOperationResult"/>
        /// including rows affected and execution statistics.
        /// </returns>
        Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default);

        #endregion
    }
}

