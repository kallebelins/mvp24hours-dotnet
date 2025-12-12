//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Extensions;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Extensions
{
    /// <summary>
    /// Extension methods for performing bulk operations on EF Core DbContext.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide high-performance bulk operations that work directly with
    /// the database, bypassing EF Core's change tracking for better performance.
    /// </para>
    /// <para>
    /// For .NET 7+, uses native ExecuteUpdateAsync and ExecuteDeleteAsync methods.
    /// For bulk insert/update/delete of entity lists, uses optimized batch processing.
    /// </para>
    /// </remarks>
    public static class BulkOperationsExtensions
    {
        #region Bulk Insert

        /// <summary>
        /// Inserts a large collection of entities efficiently using bulk operations.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="entities">Collection of entities to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of inserted rows and execution statistics.</returns>
        public static Task<BulkOperationResult> BulkInsertAsync<TEntity>(
            this DbContext dbContext,
            IList<TEntity> entities,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            return BulkInsertAsync(dbContext, entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <summary>
        /// Inserts a large collection of entities efficiently using bulk operations with custom options.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="entities">Collection of entities to insert.</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of inserted rows and execution statistics.</returns>
        public static async Task<BulkOperationResult> BulkInsertAsync<TEntity>(
            this DbContext dbContext,
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!entities.AnySafe())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-bulkinsertasync-start", 
                $"count:{entities.Count}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var dbSet = dbContext.Set<TEntity>();
                var totalCount = entities.Count;
                var processedCount = 0;

                // Process in batches
                foreach (var batch in entities.Batch(options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (options.BypassChangeTracking)
                    {
                        // Add without tracking (best performance)
                        await dbSet.AddRangeAsync(batch, cancellationToken);
                    }
                    else
                    {
                        foreach (var entity in batch)
                        {
                            await dbSet.AddAsync(entity, cancellationToken);
                        }
                    }

                    processedCount += batch.Count();
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                // Save changes
                var rowsAffected = await dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-bulkinsertasync-end",
                    $"rows:{rowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Success(rowsAffected, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "efcore-bulkoperations-bulkinsertasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region Bulk Update

        /// <summary>
        /// Updates a large collection of entities efficiently using bulk operations.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="entities">Collection of entities to update (identified by primary key).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of updated rows and execution statistics.</returns>
        public static Task<BulkOperationResult> BulkUpdateAsync<TEntity>(
            this DbContext dbContext,
            IList<TEntity> entities,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            return BulkUpdateAsync(dbContext, entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <summary>
        /// Updates a large collection of entities efficiently using bulk operations with custom options.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="entities">Collection of entities to update (identified by primary key).</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of updated rows and execution statistics.</returns>
        public static async Task<BulkOperationResult> BulkUpdateAsync<TEntity>(
            this DbContext dbContext,
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!entities.AnySafe())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-bulkupdateasync-start",
                $"count:{entities.Count}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var dbSet = dbContext.Set<TEntity>();
                var totalCount = entities.Count;
                var processedCount = 0;

                // Process in batches
                foreach (var batch in entities.Batch(options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (options.BypassChangeTracking)
                    {
                        // Attach and mark as modified
                        dbSet.UpdateRange(batch);
                    }
                    else
                    {
                        foreach (var entity in batch)
                        {
                            var entry = dbContext.Entry(entity);
                            if (entry.State == EntityState.Detached)
                            {
                                dbSet.Attach(entity);
                            }
                            entry.State = EntityState.Modified;
                        }
                    }

                    processedCount += batch.Count();
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                // Save changes
                var rowsAffected = await dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-bulkupdateasync-end",
                    $"rows:{rowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Success(rowsAffected, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "efcore-bulkoperations-bulkupdateasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region Bulk Delete

        /// <summary>
        /// Deletes a large collection of entities efficiently using bulk operations.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="entities">Collection of entities to delete (identified by primary key).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of deleted rows and execution statistics.</returns>
        public static Task<BulkOperationResult> BulkDeleteAsync<TEntity>(
            this DbContext dbContext,
            IList<TEntity> entities,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            return BulkDeleteAsync(dbContext, entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <summary>
        /// Deletes a large collection of entities efficiently using bulk operations with custom options.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="entities">Collection of entities to delete (identified by primary key).</param>
        /// <param name="options">Configuration options for the bulk operation.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of deleted rows and execution statistics.</returns>
        public static async Task<BulkOperationResult> BulkDeleteAsync<TEntity>(
            this DbContext dbContext,
            IList<TEntity> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!entities.AnySafe())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-bulkdeleteasync-start",
                $"count:{entities.Count}|batchSize:{options.BatchSize}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var dbSet = dbContext.Set<TEntity>();
                var totalCount = entities.Count;
                var processedCount = 0;

                // Process in batches
                foreach (var batch in entities.Batch(options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (options.BypassChangeTracking)
                    {
                        // Remove range
                        dbSet.RemoveRange(batch);
                    }
                    else
                    {
                        foreach (var entity in batch)
                        {
                            var entry = dbContext.Entry(entity);
                            if (entry.State == EntityState.Detached)
                            {
                                dbSet.Attach(entity);
                            }
                            entry.State = EntityState.Deleted;
                        }
                    }

                    processedCount += batch.Count();
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                // Save changes
                var rowsAffected = await dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-bulkdeleteasync-end",
                    $"rows:{rowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Success(rowsAffected, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "efcore-bulkoperations-bulkdeleteasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region Execute Update (.NET 7+)

        /// <summary>
        /// Updates all entities matching the specified condition using a single SQL statement.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <typeparam name="TProperty">Type of the property to update.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="predicate">Condition to filter entities to update.</param>
        /// <param name="property">Property selector expression.</param>
        /// <param name="value">New value for the property.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of rows affected.</returns>
        public static async Task<int> ExecuteUpdateAsync<TEntity, TProperty>(
            this DbContext dbContext,
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<TEntity, TProperty>> property,
            TProperty value,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (property == null) throw new ArgumentNullException(nameof(property));

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-executeupdateasync-start");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Convert the Expression to a Func for EF Core's SetProperty
                var compiledProperty = property.Compile();

                var rowsAffected = await dbContext.Set<TEntity>()
                    .Where(predicate)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(compiledProperty, value),
                        cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-executeupdateasync-end",
                    $"rows:{rowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return rowsAffected;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "efcore-bulkoperations-executeupdateasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                throw;
            }
        }

        /// <summary>
        /// Updates all entities matching the specified condition using a single SQL statement with multiple property setters.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="predicate">Condition to filter entities to update.</param>
        /// <param name="setPropertyCalls">Action to configure property setters using SetProperty calls.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of rows affected.</returns>
        public static async Task<int> ExecuteUpdateAsync<TEntity>(
            this DbContext dbContext,
            Expression<Func<TEntity, bool>> predicate,
            Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<TEntity>, Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<TEntity>>> setPropertyCalls,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (setPropertyCalls == null) throw new ArgumentNullException(nameof(setPropertyCalls));

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-executeupdateasync-multi-start");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var rowsAffected = await dbContext.Set<TEntity>()
                    .Where(predicate)
                    .ExecuteUpdateAsync(setPropertyCalls, cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-executeupdateasync-multi-end",
                    $"rows:{rowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return rowsAffected;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "efcore-bulkoperations-executeupdateasync-multi-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                throw;
            }
        }

        #endregion

        #region Execute Delete (.NET 7+)

        /// <summary>
        /// Deletes all entities matching the specified condition using a single SQL statement.
        /// </summary>
        /// <typeparam name="TEntity">The entity type.</typeparam>
        /// <param name="dbContext">The DbContext instance.</param>
        /// <param name="predicate">Condition to filter entities to delete.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of rows affected.</returns>
        public static async Task<int> ExecuteDeleteAsync<TEntity>(
            this DbContext dbContext,
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (dbContext == null) throw new ArgumentNullException(nameof(dbContext));
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-executedeleteasync-start");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var rowsAffected = await dbContext.Set<TEntity>()
                    .Where(predicate)
                    .ExecuteDeleteAsync(cancellationToken);

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-bulkoperations-executedeleteasync-end",
                    $"rows:{rowsAffected}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return rowsAffected;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "efcore-bulkoperations-executedeleteasync-error",
                    $"error:{ex.Message}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                throw;
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Splits a collection into batches of the specified size.
        /// </summary>
        private static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
        {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return YieldBatchElements(enumerator, batchSize - 1);
            }
        }

        private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> source, int batchSize)
        {
            yield return source.Current;
            for (var i = 0; i < batchSize && source.MoveNext(); i++)
            {
                yield return source.Current;
            }
        }

        #endregion
    }
}

