//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Logic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Application.Logic
{
    /// <summary>
    /// Asynchronous command service base class with high-performance bulk operations.
    /// Extends <see cref="CommandServiceBaseAsync{TEntity, TUoW}"/> with optimized batch processing.
    /// </summary>
    /// <typeparam name="TEntity">The entity type to be managed by this service.</typeparam>
    /// <typeparam name="TUoW">The unit of work/DbContext type.</typeparam>
    /// <remarks>
    /// <para>
    /// This class provides bulk operations that bypass EF Core change tracking for significantly
    /// better performance when processing large datasets (1000+ entities).
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// <list type="bullet">
    /// <item>Importing large datasets from external systems (CSV, Excel, API)</item>
    /// <item>Batch processing of entities (e.g., monthly billing, batch updates)</item>
    /// <item>Data migration scenarios</item>
    /// <item>Synchronization with external systems</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Performance Considerations:</strong>
    /// <list type="bullet">
    /// <item>Bulk operations bypass EF Core change tracking for better performance</item>
    /// <item>Use <see cref="BulkOperationOptions.BatchSize"/> to control memory usage</item>
    /// <item>Progress callback available for monitoring long-running operations</item>
    /// <item>Validation is performed before the bulk operation</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class CustomerBulkService : BulkCommandServiceBaseAsync&lt;Customer, MyDbContext&gt;
    /// {
    ///     public CustomerBulkService(MyDbContext dbContext, IValidator&lt;Customer&gt; validator) 
    ///         : base(dbContext, validator) { }
    ///     
    ///     public async Task&lt;IBusinessResult&lt;BulkOperationResult&gt;&gt; ImportCustomersAsync(
    ///         IList&lt;Customer&gt; customers, 
    ///         IProgress&lt;(int processed, int total)&gt; progress,
    ///         CancellationToken ct = default)
    ///     {
    ///         var options = new BulkOperationOptions
    ///         {
    ///             BatchSize = 2000,
    ///             ProgressCallback = (processed, total) =&gt; 
    ///                 progress.Report((processed, total))
    ///         };
    ///         
    ///         return await BulkAddAsync(customers, options, ct);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="IBulkCommandServiceAsync{TEntity}"/>
    /// <seealso cref="CommandServiceBaseAsync{TEntity, TUoW}"/>
    public abstract class BulkCommandServiceBaseAsync<TEntity, TUoW>
        : CommandServiceBaseAsync<TEntity, TUoW>, IBulkCommandServiceAsync<TEntity>
        where TEntity : class, IEntityBase
        where TUoW : DbContext, IUnitOfWorkAsync
    {
        #region [ Properties / Fields ]

        private readonly TUoW _dbContext;
        private readonly IValidator<TEntity>? _validator;

        /// <summary>
        /// Gets the DbContext for bulk operations.
        /// </summary>
        protected TUoW DbContext => _dbContext;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkCommandServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="dbContext">The DbContext for database operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when dbContext is null.</exception>
        protected BulkCommandServiceBaseAsync(TUoW dbContext)
            : this(dbContext, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkCommandServiceBaseAsync{TEntity, TUoW}"/> class.
        /// </summary>
        /// <param name="dbContext">The DbContext for database operations.</param>
        /// <param name="validator">The validator for entity validation.</param>
        /// <exception cref="ArgumentNullException">Thrown when dbContext is null.</exception>
        protected BulkCommandServiceBaseAsync(TUoW dbContext, IValidator<TEntity>? validator)
            : base(dbContext, validator)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _validator = validator;
        }

        #endregion

        #region [ IBulkCommandServiceAsync - Bulk Add ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default)
        {
            return BulkAddAsync(entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<BulkOperationResult>> BulkAddAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceasync-bulkaddasync-start",
                $"count:{entities?.Count ?? 0}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate all entities before bulk operation
                var validationResult = ValidateEntities(entities);
                if (!validationResult.HasData() || !validationResult.Data)
                {
                    stopwatch.Stop();
                    return BulkOperationResult.Failure(
                        "One or more entities failed validation.",
                        stopwatch.Elapsed).ToBusiness();
                }

                // Execute bulk insert
                var result = await _dbContext.BulkInsertAsync(entities, options, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceasync-bulkaddasync-end",
                    $"rows:{result.RowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms|success:{result.IsSuccess}");

                return result.ToBusiness();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "application-bulkcommandserviceasync-bulkaddasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed).ToBusiness();
            }
        }

        #endregion

        #region [ IBulkCommandServiceAsync - Bulk Modify ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default)
        {
            return BulkModifyAsync(entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<BulkOperationResult>> BulkModifyAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceasync-bulkmodifyasync-start",
                $"count:{entities?.Count ?? 0}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate all entities before bulk operation
                var validationResult = ValidateEntities(entities);
                if (!validationResult.HasData() || !validationResult.Data)
                {
                    stopwatch.Stop();
                    return BulkOperationResult.Failure(
                        "One or more entities failed validation.",
                        stopwatch.Elapsed).ToBusiness();
                }

                // Execute bulk update
                var result = await _dbContext.BulkUpdateAsync(entities, options, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceasync-bulkmodifyasync-end",
                    $"rows:{result.RowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms|success:{result.IsSuccess}");

                return result.ToBusiness();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "application-bulkcommandserviceasync-bulkmodifyasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed).ToBusiness();
            }
        }

        #endregion

        #region [ IBulkCommandServiceAsync - Bulk Remove ]

        /// <inheritdoc/>
        public virtual Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(
            IList<TEntity> entities,
            CancellationToken cancellationToken = default)
        {
            return BulkRemoveAsync(entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc/>
        public virtual async Task<IBusinessResult<BulkOperationResult>> BulkRemoveAsync(
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceasync-bulkremoveasync-start",
                $"count:{entities?.Count ?? 0}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Execute bulk delete (no validation needed for delete)
                var result = await _dbContext.BulkDeleteAsync(entities, options, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceasync-bulkremoveasync-end",
                    $"rows:{result.RowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms|success:{result.IsSuccess}");

                return result.ToBusiness();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "application-bulkcommandserviceasync-bulkremoveasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed).ToBusiness();
            }
        }

        #endregion

        #region [ Protected Helpers ]

        /// <summary>
        /// Validates all entities in the collection before bulk operation.
        /// </summary>
        /// <param name="entities">The entities to validate.</param>
        /// <returns>A business result indicating if all entities are valid.</returns>
        protected virtual IBusinessResult<bool> ValidateEntities(IList<TEntity>? entities)
        {
            if (entities == null || entities.Count == 0)
            {
                return true.ToBusiness();
            }

            foreach (var entity in entities)
            {
                var errors = entity.TryValidate(_validator);
                if (errors.AnySafe())
                {
                    TelemetryHelper.Execute(TelemetryLevels.Verbose, "application-bulkcommandserviceasync-validateentities-failed",
                        $"entityType:{typeof(TEntity).Name}|errorCount:{errors.Count}");
                    return errors.ToBusiness<bool>();
                }
            }

            return true.ToBusiness();
        }

        #endregion
    }
}

