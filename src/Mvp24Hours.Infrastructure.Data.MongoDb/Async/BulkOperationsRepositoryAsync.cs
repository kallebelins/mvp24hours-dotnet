//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Entities;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.MongoDb.Configuration;
using Mvp24Hours.Infrastructure.Data.MongoDb.Core.Contract.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb
{
    /// <summary>
    /// Repository with support for high-performance bulk operations in MongoDB.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This repository extends the standard MongoDB repository with optimized bulk operations:
    /// <list type="bullet">
    ///   <item><b>BulkInsertAsync</b> - Uses InsertMany for efficient multi-document inserts</item>
    ///   <item><b>BulkUpdateAsync</b> - Uses BulkWrite with ReplaceOne for batch updates</item>
    ///   <item><b>BulkDeleteAsync</b> - Uses BulkWrite with DeleteOne for batch deletes</item>
    ///   <item><b>UpdateManyAsync</b> - Updates all documents matching a filter</item>
    ///   <item><b>DeleteManyAsync</b> - Deletes all documents matching a filter</item>
    ///   <item><b>BulkWriteAsync</b> - Full access to mixed write operations</item>
    /// </list>
    /// </para>
    /// <para>
    /// MongoDB-specific features:
    /// <list type="bullet">
    ///   <item><b>Ordered vs Unordered</b> - Control execution order and error handling</item>
    ///   <item><b>Write Concern</b> - Configure durability guarantees</item>
    ///   <item><b>Bypass Validation</b> - Skip server-side document validation</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI
    /// services.AddScoped(typeof(IBulkOperationsMongoDbAsync&lt;&gt;), typeof(BulkOperationsRepositoryAsync&lt;&gt;));
    /// 
    /// // Use in service
    /// public class CustomerService
    /// {
    ///     private readonly IBulkOperationsMongoDbAsync&lt;Customer&gt; _repository;
    ///     
    ///     public async Task ImportCustomers(IList&lt;Customer&gt; customers)
    ///     {
    ///         var options = new MongoDbBulkOperationOptions
    ///         {
    ///             IsOrdered = false, // Better performance
    ///             BatchSize = 5000,
    ///             ProgressCallback = (processed, total) =&gt; Console.WriteLine($"{processed}/{total}")
    ///         };
    ///         
    ///         var result = await _repository.BulkInsertAsync(customers, options);
    ///         Console.WriteLine($"Inserted {result.RowsAffected} in {result.ElapsedTime.TotalSeconds}s");
    ///     }
    /// }
    /// </code>
    /// </example>
    public class BulkOperationsRepositoryAsync<T> : RepositoryAsync<T>, IBulkOperationsMongoDbAsync<T>
        where T : class, IEntityBase
    {
        #region [ Constructor ]

        private readonly ILogger<BulkOperationsRepositoryAsync<T>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BulkOperationsRepositoryAsync{T}"/> class.
        /// </summary>
        /// <param name="dbContext">MongoDB context.</param>
        /// <param name="options">Repository options.</param>
        /// <param name="logger">Optional logger for structured logging.</param>
        public BulkOperationsRepositoryAsync(
            Mvp24HoursContext dbContext,
            IOptions<MongoDbRepositoryOptions> options,
            ILogger<BulkOperationsRepositoryAsync<T>> logger = null)
            : base(dbContext, options, logger)
        {
            _logger = logger;
        }

        #endregion

        #region [ IBulkOperationsAsync - Bulk Insert ]

        /// <inheritdoc />
        public Task<BulkOperationResult> BulkInsertAsync(
            IList<T> entities,
            CancellationToken cancellationToken = default)
        {
            return BulkInsertAsync(entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc />
        public Task<BulkOperationResult> BulkInsertAsync(
            IList<T> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            var mongoOptions = ConvertToMongoOptions(options);
            return BulkInsertAsync(entities, mongoOptions, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<BulkOperationResult> BulkInsertAsync(
            IList<T> entities,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!entities.AnySafe())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            _logger?.LogDebug("Starting bulk insert: {Count} entities, BatchSize: {BatchSize}, Ordered: {IsOrdered}",
                entities.Count, options.BatchSize, options.IsOrdered);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var totalCount = entities.Count;
                var processedCount = 0;
                var insertedCount = 0;

                // Configure insert options
                var insertManyOptions = new InsertManyOptions
                {
                    IsOrdered = options.IsOrdered,
                    BypassDocumentValidation = options.BypassDocumentValidation
                };

                // Process in batches
                foreach (var batch in Batch(entities, options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchList = batch.ToList();

                    if (options.UseTransaction && dbContext.Session != null)
                    {
                        await dbEntities.InsertManyAsync(
                            dbContext.Session,
                            batchList,
                            insertManyOptions,
                            cancellationToken);
                    }
                    else
                    {
                        await dbEntities.InsertManyAsync(
                            batchList,
                            insertManyOptions,
                            cancellationToken);
                    }

                    insertedCount += batchList.Count;
                    processedCount += batchList.Count;
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                stopwatch.Stop();

                _logger?.LogInformation("Bulk insert completed: {InsertedCount} entities inserted in {ElapsedMs}ms",
                    insertedCount, stopwatch.ElapsedMilliseconds);

                return BulkOperationResult.Success(insertedCount, stopwatch.Elapsed);
            }
            catch (MongoBulkWriteException<T> ex)
            {
                stopwatch.Stop();

                var insertedCount = ex.Result?.InsertedCount ?? 0;
                var errorMessage = $"Bulk insert partially failed. Inserted: {insertedCount}, Errors: {ex.WriteErrors?.Count ?? 0}. {ex.Message}";

                _logger?.LogWarning(ex, "Bulk insert partially failed: {InsertedCount} inserted, {ErrorCount} errors in {ElapsedMs}ms",
                    insertedCount, ex.WriteErrors?.Count ?? 0, stopwatch.ElapsedMilliseconds);

                // For unordered operations, return partial success
                if (!options.IsOrdered)
                {
                    return BulkOperationResult.Success((int)insertedCount, stopwatch.Elapsed);
                }

                return BulkOperationResult.Failure(errorMessage, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger?.LogError(ex, "Bulk insert failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region [ IBulkOperationsAsync - Bulk Update ]

        /// <inheritdoc />
        public Task<BulkOperationResult> BulkUpdateAsync(
            IList<T> entities,
            CancellationToken cancellationToken = default)
        {
            return BulkUpdateAsync(entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc />
        public Task<BulkOperationResult> BulkUpdateAsync(
            IList<T> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            var mongoOptions = ConvertToMongoOptions(options);
            return BulkUpdateAsync(entities, mongoOptions, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<BulkOperationResult> BulkUpdateAsync(
            IList<T> entities,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!entities.AnySafe())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            _logger?.LogDebug("Starting bulk update: {Count} entities, BatchSize: {BatchSize}, Ordered: {IsOrdered}",
                entities.Count, options.BatchSize, options.IsOrdered);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var totalCount = entities.Count;
                var processedCount = 0;
                long modifiedCount = 0;

                // Build bulk write options
                var bulkWriteOptions = new BulkWriteOptions
                {
                    IsOrdered = options.IsOrdered,
                    BypassDocumentValidation = options.BypassDocumentValidation
                };

                // Process in batches
                foreach (var batch in Batch(entities, options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchList = batch.ToList();
                    var writeModels = new List<WriteModel<T>>();

                    foreach (var entity in batchList)
                    {
                        var filter = GetKeyFilter(entity);
                        writeModels.Add(new ReplaceOneModel<T>(filter, entity));
                    }

                    BulkWriteResult<T> result;
                    if (options.UseTransaction && dbContext.Session != null)
                    {
                        result = await dbEntities.BulkWriteAsync(
                            dbContext.Session,
                            writeModels,
                            bulkWriteOptions,
                            cancellationToken);
                    }
                    else
                    {
                        result = await dbEntities.BulkWriteAsync(
                            writeModels,
                            bulkWriteOptions,
                            cancellationToken);
                    }

                    modifiedCount += result.ModifiedCount;
                    processedCount += batchList.Count;
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                stopwatch.Stop();

                _logger?.LogInformation("Bulk update completed: {ModifiedCount} entities modified in {ElapsedMs}ms",
                    modifiedCount, stopwatch.ElapsedMilliseconds);

                return BulkOperationResult.Success((int)modifiedCount, stopwatch.Elapsed);
            }
            catch (MongoBulkWriteException<T> ex)
            {
                stopwatch.Stop();

                var modifiedCount = ex.Result?.ModifiedCount ?? 0;
                var errorMessage = $"Bulk update partially failed. Modified: {modifiedCount}, Errors: {ex.WriteErrors?.Count ?? 0}. {ex.Message}";

                _logger?.LogWarning(ex, "Bulk update partially failed: {ModifiedCount} modified, {ErrorCount} errors in {ElapsedMs}ms",
                    modifiedCount, ex.WriteErrors?.Count ?? 0, stopwatch.ElapsedMilliseconds);

                if (!options.IsOrdered)
                {
                    return BulkOperationResult.Success((int)modifiedCount, stopwatch.Elapsed);
                }

                return BulkOperationResult.Failure(errorMessage, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger?.LogError(ex, "Bulk update failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region [ IBulkOperationsAsync - Bulk Delete ]

        /// <inheritdoc />
        public Task<BulkOperationResult> BulkDeleteAsync(
            IList<T> entities,
            CancellationToken cancellationToken = default)
        {
            return BulkDeleteAsync(entities, new BulkOperationOptions(), cancellationToken);
        }

        /// <inheritdoc />
        public Task<BulkOperationResult> BulkDeleteAsync(
            IList<T> entities,
            BulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            var mongoOptions = ConvertToMongoOptions(options);
            return BulkDeleteAsync(entities, mongoOptions, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<BulkOperationResult> BulkDeleteAsync(
            IList<T> entities,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            if (!entities.AnySafe())
            {
                return BulkOperationResult.Success(0, TimeSpan.Zero);
            }

            _logger?.LogDebug("Starting bulk delete: {Count} entities, BatchSize: {BatchSize}, Ordered: {IsOrdered}",
                entities.Count, options.BatchSize, options.IsOrdered);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var totalCount = entities.Count;
                var processedCount = 0;
                long deletedCount = 0;

                // Build bulk write options
                var bulkWriteOptions = new BulkWriteOptions
                {
                    IsOrdered = options.IsOrdered,
                    BypassDocumentValidation = options.BypassDocumentValidation
                };

                // Process in batches
                foreach (var batch in Batch(entities, options.BatchSize))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batchList = batch.ToList();
                    var writeModels = new List<WriteModel<T>>();

                    foreach (var entity in batchList)
                    {
                        var filter = GetKeyFilter(entity);
                        writeModels.Add(new DeleteOneModel<T>(filter));
                    }

                    BulkWriteResult<T> result;
                    if (options.UseTransaction && dbContext.Session != null)
                    {
                        result = await dbEntities.BulkWriteAsync(
                            dbContext.Session,
                            writeModels,
                            bulkWriteOptions,
                            cancellationToken);
                    }
                    else
                    {
                        result = await dbEntities.BulkWriteAsync(
                            writeModels,
                            bulkWriteOptions,
                            cancellationToken);
                    }

                    deletedCount += result.DeletedCount;
                    processedCount += batchList.Count;
                    options.ProgressCallback?.Invoke(processedCount, totalCount);
                }

                stopwatch.Stop();

                _logger?.LogInformation("Bulk delete completed: {DeletedCount} entities deleted in {ElapsedMs}ms",
                    deletedCount, stopwatch.ElapsedMilliseconds);

                return BulkOperationResult.Success((int)deletedCount, stopwatch.Elapsed);
            }
            catch (MongoBulkWriteException<T> ex)
            {
                stopwatch.Stop();

                var deletedCount = ex.Result?.DeletedCount ?? 0;
                var errorMessage = $"Bulk delete partially failed. Deleted: {deletedCount}, Errors: {ex.WriteErrors?.Count ?? 0}. {ex.Message}";

                _logger?.LogWarning(ex, "Bulk delete partially failed: {DeletedCount} deleted, {ErrorCount} errors in {ElapsedMs}ms",
                    deletedCount, ex.WriteErrors?.Count ?? 0, stopwatch.ElapsedMilliseconds);

                if (!options.IsOrdered)
                {
                    return BulkOperationResult.Success((int)deletedCount, stopwatch.Elapsed);
                }

                return BulkOperationResult.Failure(errorMessage, stopwatch.Elapsed);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger?.LogError(ex, "Bulk delete failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region [ IBulkOperationsAsync - Execute Update ]

        /// <inheritdoc />
        public async Task<int> ExecuteUpdateAsync<TProperty>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TProperty>> property,
            TProperty value,
            CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (property == null) throw new ArgumentNullException(nameof(property));

            _logger?.LogDebug("Starting ExecuteUpdateAsync for collection {CollectionName}", typeof(T).Name);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var filter = Builders<T>.Filter.Where(predicate);
                var update = Builders<T>.Update.Set(property, value);

                UpdateResult result;
                if (dbContext.Session != null)
                {
                    result = await dbEntities.UpdateManyAsync(
                        dbContext.Session,
                        filter,
                        update,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    result = await dbEntities.UpdateManyAsync(
                        filter,
                        update,
                        cancellationToken: cancellationToken);
                }

                stopwatch.Stop();

                _logger?.LogInformation("ExecuteUpdateAsync completed: {ModifiedCount} entities modified in {ElapsedMs}ms",
                    result.ModifiedCount, stopwatch.ElapsedMilliseconds);

                return (int)result.ModifiedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger?.LogError(ex, "ExecuteUpdateAsync failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> ExecuteUpdateAsync(
            Expression<Func<T, bool>> predicate,
            Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls,
            CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            if (setPropertyCalls == null) throw new ArgumentNullException(nameof(setPropertyCalls));

            _logger?.LogDebug("Starting ExecuteUpdateAsync (multi-property) for collection {CollectionName}", typeof(T).Name);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Build the SetPropertyCalls from the expression
                var ourSetters = new SetPropertyCalls<T>();
                setPropertyCalls.Compile()(ourSetters);

                var setters = ourSetters.Setters;
                if (!setters.Any())
                {
                    return 0;
                }

                var filter = Builders<T>.Filter.Where(predicate);
                var updateBuilder = Builders<T>.Update;
                var updates = new List<UpdateDefinition<T>>();

                foreach (var setter in setters)
                {
                    // Get the property name from the expression
                    var propertyName = GetPropertyName(setter.Property);

                    if (setter.ValueExpression != null)
                    {
                        // For expression-based values, we need to use aggregation pipeline
                        // This is more complex in MongoDB - for now, compile and evaluate
                        var compiledExpr = setter.ValueExpression.Compile();
                        throw new NotSupportedException(
                            "Expression-based property setters are not supported in MongoDB bulk updates. " +
                            "Use constant values or the UpdateDefinition builder directly.");
                    }
                    else
                    {
                        // Create Set update for constant value
                        var setMethod = typeof(UpdateDefinitionBuilder<T>)
                            .GetMethods()
                            .First(m => m.Name == "Set" && m.GetParameters().Length == 2);

                        var propertyType = GetPropertyTypeFromExpression(setter.Property);
                        var genericSetMethod = setMethod.MakeGenericMethod(propertyType);

                        var updateDef = (UpdateDefinition<T>)genericSetMethod.Invoke(
                            updateBuilder,
                            new[] { setter.Property, setter.Value });

                        updates.Add(updateDef);
                    }
                }

                var combinedUpdate = updateBuilder.Combine(updates);

                UpdateResult result;
                if (dbContext.Session != null)
                {
                    result = await dbEntities.UpdateManyAsync(
                        dbContext.Session,
                        filter,
                        combinedUpdate,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    result = await dbEntities.UpdateManyAsync(
                        filter,
                        combinedUpdate,
                        cancellationToken: cancellationToken);
                }

                stopwatch.Stop();

                _logger?.LogInformation("ExecuteUpdateAsync (multi-property) completed: {ModifiedCount} entities modified in {ElapsedMs}ms",
                    result.ModifiedCount, stopwatch.ElapsedMilliseconds);

                return (int)result.ModifiedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger?.LogError(ex, "ExecuteUpdateAsync (multi-property) failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }

        #endregion

        #region [ IBulkOperationsAsync - Execute Delete ]

        /// <inheritdoc />
        public async Task<int> ExecuteDeleteAsync(
            Expression<Func<T, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));

            _logger?.LogDebug("Starting ExecuteDeleteAsync for collection {CollectionName}", typeof(T).Name);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var filter = Builders<T>.Filter.Where(predicate);

                DeleteResult result;
                if (dbContext.Session != null)
                {
                    result = await dbEntities.DeleteManyAsync(
                        dbContext.Session,
                        filter,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    result = await dbEntities.DeleteManyAsync(
                        filter,
                        cancellationToken: cancellationToken);
                }

                stopwatch.Stop();

                _logger?.LogInformation("ExecuteDeleteAsync completed: {DeletedCount} entities deleted in {ElapsedMs}ms",
                    result.DeletedCount, stopwatch.ElapsedMilliseconds);

                return (int)result.DeletedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger?.LogError(ex, "ExecuteDeleteAsync failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }

        #endregion

        #region [ IBulkOperationsMongoDbAsync - BulkWrite ]

        /// <inheritdoc />
        public async Task<MongoDbBulkOperationResult> BulkWriteAsync(
            IEnumerable<WriteModel<T>> requests,
            MongoDbBulkOperationOptions options,
            CancellationToken cancellationToken = default)
        {
            if (requests == null) throw new ArgumentNullException(nameof(requests));
            options ??= MongoDbBulkOperationOptions.Default;

            var requestList = requests.ToList();
            if (!requestList.Any())
            {
                return MongoDbBulkOperationResult.Success();
            }

            _logger?.LogDebug("Starting BulkWriteAsync: {Count} write models, Ordered: {IsOrdered}",
                requestList.Count, options.IsOrdered);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var bulkWriteOptions = new BulkWriteOptions
                {
                    IsOrdered = options.IsOrdered,
                    BypassDocumentValidation = options.BypassDocumentValidation
                };

                BulkWriteResult<T> result;
                if (options.UseTransaction && dbContext.Session != null)
                {
                    result = await dbEntities.BulkWriteAsync(
                        dbContext.Session,
                        requestList,
                        bulkWriteOptions,
                        cancellationToken);
                }
                else
                {
                    result = await dbEntities.BulkWriteAsync(
                        requestList,
                        bulkWriteOptions,
                        cancellationToken);
                }

                stopwatch.Stop();

                _logger?.LogInformation("BulkWriteAsync completed: Inserted={InsertedCount}, Modified={ModifiedCount}, Deleted={DeletedCount} in {ElapsedMs}ms",
                    result.InsertedCount, result.ModifiedCount, result.DeletedCount, stopwatch.ElapsedMilliseconds);

                return MongoDbBulkOperationResult.Success(
                    result.InsertedCount,
                    result.MatchedCount,
                    result.ModifiedCount,
                    result.DeletedCount,
                    stopwatch.Elapsed);
            }
            catch (MongoBulkWriteException<T> ex)
            {
                stopwatch.Stop();

                _logger?.LogWarning(ex, "BulkWriteAsync partially failed: {ErrorCount} errors in {ElapsedMs}ms",
                    ex.WriteErrors?.Count ?? 0, stopwatch.ElapsedMilliseconds);

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

                _logger?.LogError(ex, "BulkWriteAsync failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                return MongoDbBulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
            }
        }

        #endregion

        #region [ IBulkOperationsMongoDbAsync - UpdateMany ]

        /// <inheritdoc />
        public async Task<long> UpdateManyAsync(
            Expression<Func<T, bool>> filter,
            UpdateDefinition<T> update,
            CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (update == null) throw new ArgumentNullException(nameof(update));

            var filterDefinition = Builders<T>.Filter.Where(filter);
            return await UpdateManyAsync(filterDefinition, update, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<long> UpdateManyAsync(
            FilterDefinition<T> filter,
            UpdateDefinition<T> update,
            CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (update == null) throw new ArgumentNullException(nameof(update));

            _logger?.LogDebug("Starting UpdateManyAsync for collection {CollectionName}", typeof(T).Name);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                UpdateResult result;
                if (dbContext.Session != null)
                {
                    result = await dbEntities.UpdateManyAsync(
                        dbContext.Session,
                        filter,
                        update,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    result = await dbEntities.UpdateManyAsync(
                        filter,
                        update,
                        cancellationToken: cancellationToken);
                }

                stopwatch.Stop();

                _logger?.LogInformation("UpdateManyAsync completed: Matched={MatchedCount}, Modified={ModifiedCount} in {ElapsedMs}ms",
                    result.MatchedCount, result.ModifiedCount, stopwatch.ElapsedMilliseconds);

                return result.ModifiedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger?.LogError(ex, "UpdateManyAsync failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }

        #endregion

        #region [ IBulkOperationsMongoDbAsync - DeleteMany ]

        /// <inheritdoc />
        public async Task<long> DeleteManyAsync(
            Expression<Func<T, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            var filterDefinition = Builders<T>.Filter.Where(filter);
            return await DeleteManyAsync(filterDefinition, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<long> DeleteManyAsync(
            FilterDefinition<T> filter,
            CancellationToken cancellationToken = default)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            _logger?.LogDebug("Starting DeleteManyAsync for collection {CollectionName}", typeof(T).Name);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                DeleteResult result;
                if (dbContext.Session != null)
                {
                    result = await dbEntities.DeleteManyAsync(
                        dbContext.Session,
                        filter,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    result = await dbEntities.DeleteManyAsync(
                        filter,
                        cancellationToken: cancellationToken);
                }

                stopwatch.Stop();

                _logger?.LogInformation("DeleteManyAsync completed: {DeletedCount} entities deleted in {ElapsedMs}ms",
                    result.DeletedCount, stopwatch.ElapsedMilliseconds);

                return result.DeletedCount;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger?.LogError(ex, "DeleteManyAsync failed after {ElapsedMs}ms: {ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }

        #endregion

        #region [ Helpers ]

        private static MongoDbBulkOperationOptions ConvertToMongoOptions(BulkOperationOptions options)
        {
            return new MongoDbBulkOperationOptions
            {
                BatchSize = options.BatchSize,
                UseTransaction = options.UseTransaction,
                ProgressCallback = options.ProgressCallback,
                TimeoutSeconds = options.TimeoutSeconds,
                BypassDocumentValidation = false,
                IsOrdered = true
            };
        }

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

        private static string GetPropertyName(LambdaExpression expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException("Expression must be a member expression", nameof(expression));
        }

        private static Type GetPropertyTypeFromExpression(LambdaExpression expression)
        {
            if (expression.Body is MemberExpression memberExpression)
            {
                return ((PropertyInfo)memberExpression.Member).PropertyType;
            }
            return expression.ReturnType;
        }

        #endregion

        #region [ Properties ]

        /// <inheritdoc />
        protected override object EntityLogBy => null;

        #endregion
    }
}

