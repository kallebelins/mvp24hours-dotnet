//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using Mvp24Hours.Infrastructure.Data.MongoDb;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for MongoDB bulk operations directly on the context.
    /// </summary>
    /// <remarks>
    /// <para>
    /// These extensions provide a convenient way to perform bulk operations
    /// directly on the MongoDB context without needing a repository:
    /// <list type="bullet">
    ///   <item><see cref="BulkInsertAsync{TEntity}"/> - Efficient multi-document inserts</item>
    ///   <item><see cref="BulkUpdateAsync{TEntity}"/> - Batch updates using BulkWrite</item>
    ///   <item><see cref="BulkDeleteAsync{TEntity}"/> - Batch deletes using BulkWrite</item>
    ///   <item><see cref="UpdateManyAsync{TEntity}"/> - Update all matching documents</item>
    ///   <item><see cref="DeleteManyAsync{TEntity}"/> - Delete all matching documents</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Direct bulk insert on context
    /// var result = await context.BulkInsertAsync(customers, new MongoDbBulkOperationOptions
    /// {
    ///     IsOrdered = false,
    ///     BatchSize = 5000
    /// });
    /// 
    /// // Update many documents matching filter
    /// var count = await context.UpdateManyAsync&lt;Customer&gt;(
    ///     c => c.Status == "pending",
    ///     Builders&lt;Customer&gt;.Update.Set(c => c.Status, "processed")
    /// );
    /// </code>
    /// </example>
    public static class MongoDbBulkOperationsExtensions
    {
        #region [ Bulk Insert ]

        /// <summary>
        /// Inserts a large collection of entities using MongoDB's InsertMany.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="entities">Collection of entities to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of inserted rows and execution statistics.</returns>
        public static Task<BulkOperationResult> BulkInsertAsync<TEntity>(
            this Mvp24HoursContext context,
            IList<TEntity> entities,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            return BulkInsertAsync(context, entities, MongoDbBulkOperationOptions.Default, cancellationToken);
        }

        /// <summary>
        /// Inserts a large collection of entities using MongoDB's InsertMany with options.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="entities">Collection of entities to insert.</param>
        /// <param name="options">Bulk operation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of inserted rows and execution statistics.</returns>
        public static async Task<BulkOperationResult> BulkInsertAsync<TEntity>(
            this Mvp24HoursContext context,
            IList<TEntity> entities,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (entities == null || !entities.Any())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            options ??= MongoDbBulkOperationOptions.Default;

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-bulkinsertasync-start",
                $"count:{entities.Count}|ordered:{options.IsOrdered}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var collection = context.Set<TEntity>();
                var totalCount = entities.Count;
                var processedCount = 0;
                var insertedCount = 0;

                var insertManyOptions = new InsertManyOptions
                {
                    IsOrdered = options.IsOrdered,
                    BypassDocumentValidation = options.BypassDocumentValidation
                };

                foreach (var batch in Batch(entities, options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchList = batch.ToList();

                    if (options.UseTransaction && context.Session != null)
                    {
                        await collection.InsertManyAsync(
                            context.Session,
                            batchList,
                            insertManyOptions,
                            cancellationToken);
                    }
                    else
                    {
                        await collection.InsertManyAsync(
                            batchList,
                            insertManyOptions,
                            cancellationToken);
                    }

                    insertedCount += batchList.Count;
                    processedCount += batchList.Count;
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-bulkinsertasync-end",
                    $"rows:{insertedCount}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Success(insertedCount, stopwatch.Elapsed);
            }
            catch (MongoBulkWriteException<TEntity> ex)
            {
                stopwatch.Stop();

                var insertedCount = ex.Result?.InsertedCount ?? 0;

                TelemetryHelper.Execute(TelemetryLevels.Warning, "mongodb-ext-bulkinsertasync-partial",
                    $"inserted:{insertedCount}|errors:{ex.WriteErrors?.Count ?? 0}");

                if (!options.IsOrdered)
                {
                    return BulkOperationResult.Success((int)insertedCount, stopwatch.Elapsed);
                }

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "mongodb-ext-bulkinsertasync-error",
                    $"error:{ex.Message}");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region [ Bulk Update ]

        /// <summary>
        /// Updates a large collection of entities using MongoDB's BulkWrite with ReplaceOne.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="entities">Collection of entities to update.</param>
        /// <param name="keySelector">Expression to select the key field for matching documents.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of updated rows and execution statistics.</returns>
        public static Task<BulkOperationResult> BulkUpdateAsync<TEntity>(
            this Mvp24HoursContext context,
            IList<TEntity> entities,
            Expression<Func<TEntity, object>> keySelector,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            return BulkUpdateAsync(context, entities, keySelector, MongoDbBulkOperationOptions.Default, cancellationToken);
        }

        /// <summary>
        /// Updates a large collection of entities using MongoDB's BulkWrite with ReplaceOne and options.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="entities">Collection of entities to update.</param>
        /// <param name="keySelector">Expression to select the key field for matching documents.</param>
        /// <param name="options">Bulk operation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of updated rows and execution statistics.</returns>
        public static async Task<BulkOperationResult> BulkUpdateAsync<TEntity>(
            this Mvp24HoursContext context,
            IList<TEntity> entities,
            Expression<Func<TEntity, object>> keySelector,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (entities == null || !entities.Any())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            options ??= MongoDbBulkOperationOptions.Default;

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-bulkupdateasync-start",
                $"count:{entities.Count}|ordered:{options.IsOrdered}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var collection = context.Set<TEntity>();
                var totalCount = entities.Count;
                var processedCount = 0;
                long modifiedCount = 0;

                var bulkWriteOptions = new BulkWriteOptions
                {
                    IsOrdered = options.IsOrdered,
                    BypassDocumentValidation = options.BypassDocumentValidation
                };

                var compiledKeySelector = keySelector.Compile();

                foreach (var batch in Batch(entities, options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchList = batch.ToList();
                    var writeModels = new List<WriteModel<TEntity>>();

                    foreach (var entity in batchList)
                    {
                        var keyValue = compiledKeySelector(entity);
                        var filter = Builders<TEntity>.Filter.Eq(keySelector, keyValue);
                        writeModels.Add(new ReplaceOneModel<TEntity>(filter, entity));
                    }

                    BulkWriteResult<TEntity> result;
                    if (options.UseTransaction && context.Session != null)
                    {
                        result = await collection.BulkWriteAsync(
                            context.Session,
                            writeModels,
                            bulkWriteOptions,
                            cancellationToken);
                    }
                    else
                    {
                        result = await collection.BulkWriteAsync(
                            writeModels,
                            bulkWriteOptions,
                            cancellationToken);
                    }

                    modifiedCount += result.ModifiedCount;
                    processedCount += batchList.Count;
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-bulkupdateasync-end",
                    $"modified:{modifiedCount}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Success((int)modifiedCount, stopwatch.Elapsed);
            }
            catch (MongoBulkWriteException<TEntity> ex)
            {
                stopwatch.Stop();

                var modifiedCount = ex.Result?.ModifiedCount ?? 0;

                TelemetryHelper.Execute(TelemetryLevels.Warning, "mongodb-ext-bulkupdateasync-partial",
                    $"modified:{modifiedCount}|errors:{ex.WriteErrors?.Count ?? 0}");

                if (!options.IsOrdered)
                {
                    return BulkOperationResult.Success((int)modifiedCount, stopwatch.Elapsed);
                }

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "mongodb-ext-bulkupdateasync-error",
                    $"error:{ex.Message}");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region [ Bulk Delete ]

        /// <summary>
        /// Deletes a large collection of entities using MongoDB's BulkWrite with DeleteOne.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="entities">Collection of entities to delete.</param>
        /// <param name="keySelector">Expression to select the key field for matching documents.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of deleted rows and execution statistics.</returns>
        public static Task<BulkOperationResult> BulkDeleteAsync<TEntity>(
            this Mvp24HoursContext context,
            IList<TEntity> entities,
            Expression<Func<TEntity, object>> keySelector,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            return BulkDeleteAsync(context, entities, keySelector, MongoDbBulkOperationOptions.Default, cancellationToken);
        }

        /// <summary>
        /// Deletes a large collection of entities using MongoDB's BulkWrite with DeleteOne and options.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="entities">Collection of entities to delete.</param>
        /// <param name="keySelector">Expression to select the key field for matching documents.</param>
        /// <param name="options">Bulk operation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Result containing the number of deleted rows and execution statistics.</returns>
        public static async Task<BulkOperationResult> BulkDeleteAsync<TEntity>(
            this Mvp24HoursContext context,
            IList<TEntity> entities,
            Expression<Func<TEntity, object>> keySelector,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (entities == null || !entities.Any())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            options ??= MongoDbBulkOperationOptions.Default;

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-bulkdeleteasync-start",
                $"count:{entities.Count}|ordered:{options.IsOrdered}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var collection = context.Set<TEntity>();
                var totalCount = entities.Count;
                var processedCount = 0;
                long deletedCount = 0;

                var bulkWriteOptions = new BulkWriteOptions
                {
                    IsOrdered = options.IsOrdered,
                    BypassDocumentValidation = options.BypassDocumentValidation
                };

                var compiledKeySelector = keySelector.Compile();

                foreach (var batch in Batch(entities, options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchList = batch.ToList();
                    var writeModels = new List<WriteModel<TEntity>>();

                    foreach (var entity in batchList)
                    {
                        var keyValue = compiledKeySelector(entity);
                        var filter = Builders<TEntity>.Filter.Eq(keySelector, keyValue);
                        writeModels.Add(new DeleteOneModel<TEntity>(filter));
                    }

                    BulkWriteResult<TEntity> result;
                    if (options.UseTransaction && context.Session != null)
                    {
                        result = await collection.BulkWriteAsync(
                            context.Session,
                            writeModels,
                            bulkWriteOptions,
                            cancellationToken);
                    }
                    else
                    {
                        result = await collection.BulkWriteAsync(
                            writeModels,
                            bulkWriteOptions,
                            cancellationToken);
                    }

                    deletedCount += result.DeletedCount;
                    processedCount += batchList.Count;
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-bulkdeleteasync-end",
                    $"deleted:{deletedCount}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return BulkOperationResult.Success((int)deletedCount, stopwatch.Elapsed);
            }
            catch (MongoBulkWriteException<TEntity> ex)
            {
                stopwatch.Stop();

                var deletedCount = ex.Result?.DeletedCount ?? 0;

                TelemetryHelper.Execute(TelemetryLevels.Warning, "mongodb-ext-bulkdeleteasync-partial",
                    $"deleted:{deletedCount}|errors:{ex.WriteErrors?.Count ?? 0}");

                if (!options.IsOrdered)
                {
                    return BulkOperationResult.Success((int)deletedCount, stopwatch.Elapsed);
                }

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "mongodb-ext-bulkdeleteasync-error",
                    $"error:{ex.Message}");

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region [ Update Many ]

        /// <summary>
        /// Updates all documents matching the filter.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="filter">Filter expression to match documents.</param>
        /// <param name="update">Update definition.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of modified documents.</returns>
        /// <example>
        /// <code>
        /// var count = await context.UpdateManyAsync&lt;Customer&gt;(
        ///     c => c.Status == "pending",
        ///     Builders&lt;Customer&gt;.Update.Set(c => c.Status, "processed")
        /// );
        /// </code>
        /// </example>
        public static async Task<long> UpdateManyAsync<TEntity>(
            this Mvp24HoursContext context,
            Expression<Func<TEntity, bool>> filter,
            UpdateDefinition<TEntity> update,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (update == null) throw new ArgumentNullException(nameof(update));

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-updatemanyasync-start");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var collection = context.Set<TEntity>();
                var filterDefinition = Builders<TEntity>.Filter.Where(filter);

                UpdateResult result;
                if (context.Session != null)
                {
                    result = await collection.UpdateManyAsync(
                        context.Session,
                        filterDefinition,
                        update,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    result = await collection.UpdateManyAsync(
                        filterDefinition,
                        update,
                        cancellationToken: cancellationToken);
                }

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-updatemanyasync-end",
                    $"matched:{result.MatchedCount}|modified:{result.ModifiedCount}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return result.ModifiedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "mongodb-ext-updatemanyasync-error",
                    $"error:{ex.Message}");

                throw;
            }
        }

        #endregion

        #region [ Delete Many ]

        /// <summary>
        /// Deletes all documents matching the filter.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="filter">Filter expression to match documents.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Number of deleted documents.</returns>
        /// <example>
        /// <code>
        /// var count = await context.DeleteManyAsync&lt;Customer&gt;(
        ///     c => c.CreatedAt &lt; DateTime.UtcNow.AddYears(-5)
        /// );
        /// </code>
        /// </example>
        public static async Task<long> DeleteManyAsync<TEntity>(
            this Mvp24HoursContext context,
            Expression<Func<TEntity, bool>> filter,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-deletemanyasync-start");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var collection = context.Set<TEntity>();
                var filterDefinition = Builders<TEntity>.Filter.Where(filter);

                DeleteResult result;
                if (context.Session != null)
                {
                    result = await collection.DeleteManyAsync(
                        context.Session,
                        filterDefinition,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    result = await collection.DeleteManyAsync(
                        filterDefinition,
                        cancellationToken: cancellationToken);
                }

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-deletemanyasync-end",
                    $"deleted:{result.DeletedCount}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "mongodb-ext-deletemanyasync-error",
                    $"error:{ex.Message}");

                throw;
            }
        }

        #endregion

        #region [ BulkWrite ]

        /// <summary>
        /// Executes a bulk write operation with mixed write models.
        /// </summary>
        /// <typeparam name="TEntity">Entity type.</typeparam>
        /// <param name="context">MongoDB context.</param>
        /// <param name="requests">Collection of write models.</param>
        /// <param name="options">Bulk operation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Detailed result of the bulk operation.</returns>
        public static async Task<MongoDbBulkOperationResult> BulkWriteAsync<TEntity>(
            this Mvp24HoursContext context,
            IEnumerable<WriteModel<TEntity>> requests,
            MongoDbBulkOperationOptions options = null,
            CancellationToken cancellationToken = default)
            where TEntity : class, IEntityBase
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (requests == null) throw new ArgumentNullException(nameof(requests));

            var requestList = requests.ToList();
            if (!requestList.Any())
            {
                return MongoDbBulkOperationResult.Success();
            }

            options ??= MongoDbBulkOperationOptions.Default;

            TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-bulkwriteasync-start",
                $"count:{requestList.Count}|ordered:{options.IsOrdered}");

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var collection = context.Set<TEntity>();

                var bulkWriteOptions = new BulkWriteOptions
                {
                    IsOrdered = options.IsOrdered,
                    BypassDocumentValidation = options.BypassDocumentValidation
                };

                BulkWriteResult<TEntity> result;
                if (options.UseTransaction && context.Session != null)
                {
                    result = await collection.BulkWriteAsync(
                        context.Session,
                        requestList,
                        bulkWriteOptions,
                        cancellationToken);
                }
                else
                {
                    result = await collection.BulkWriteAsync(
                        requestList,
                        bulkWriteOptions,
                        cancellationToken);
                }

                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Verbose, "mongodb-ext-bulkwriteasync-end",
                    $"inserted:{result.InsertedCount}|modified:{result.ModifiedCount}|deleted:{result.DeletedCount}|elapsed:{stopwatch.ElapsedMilliseconds}ms");

                return MongoDbBulkOperationResult.Success(
                    result.InsertedCount,
                    result.MatchedCount,
                    result.ModifiedCount,
                    result.DeletedCount,
                    stopwatch.Elapsed);
            }
            catch (MongoBulkWriteException<TEntity> ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Warning, "mongodb-ext-bulkwriteasync-partial",
                    $"errors:{ex.WriteErrors?.Count ?? 0}");

                var result = ex.Result;
                if (!options.IsOrdered && result != null)
                {
                    return MongoDbBulkOperationResult.Success(
                        result.InsertedCount,
                        result.MatchedCount,
                        result.ModifiedCount,
                        result.DeletedCount,
                        stopwatch.Elapsed);
                }

                return MongoDbBulkOperationResult.Failure(
                    ex.Message,
                    stopwatch.Elapsed,
                    ex.WriteErrors?.Count ?? 0);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                TelemetryHelper.Execute(TelemetryLevels.Error, "mongodb-ext-bulkwriteasync-error",
                    $"error:{ex.Message}");

                return MongoDbBulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region [ Helpers ]

        private static IEnumerable<IEnumerable<TItem>> Batch<TItem>(IEnumerable<TItem> source, int batchSize)
        {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return YieldBatchElements(enumerator, batchSize - 1);
            }
        }

        private static IEnumerable<TItem> YieldBatchElements<TItem>(IEnumerator<TItem> source, int batchSize)
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

