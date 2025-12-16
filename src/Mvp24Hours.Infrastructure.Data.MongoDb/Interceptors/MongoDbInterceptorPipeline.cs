//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Interceptors
{
    /// <summary>
    /// Manages and executes a pipeline of MongoDB interceptors.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class coordinates the execution of multiple interceptors in order,
    /// ensuring they are called before and after each database operation.
    /// </para>
    /// <para>
    /// Interceptors are executed in order based on their <see cref="IMongoDbInterceptor.Order"/> property.
    /// Lower values execute first for "before" operations and last for "after" operations.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var pipeline = new MongoDbInterceptorPipeline(interceptors);
    /// 
    /// // Execute insert with interceptors
    /// await pipeline.ExecuteInsertAsync(entity, async () =>
    /// {
    ///     await collection.InsertOneAsync(entity);
    /// });
    /// </code>
    /// </example>
    public class MongoDbInterceptorPipeline : IMongoDbInterceptorPipeline
    {
        private readonly IReadOnlyList<IMongoDbInterceptor> _interceptors;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbInterceptorPipeline"/> class.
        /// </summary>
        /// <param name="interceptors">The collection of interceptors to execute.</param>
        public MongoDbInterceptorPipeline(IEnumerable<IMongoDbInterceptor> interceptors)
        {
            _interceptors = (interceptors ?? Enumerable.Empty<IMongoDbInterceptor>())
                .OrderBy(i => i.Order)
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Gets a value indicating whether any interceptors are registered.
        /// </summary>
        public bool HasInterceptors => _interceptors.Count > 0;

        /// <summary>
        /// Gets the number of registered interceptors.
        /// </summary>
        public int InterceptorCount => _interceptors.Count;

        /// <inheritdoc />
        public async Task ExecuteInsertAsync<T>(T entity, Func<Task> operation, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            if (entity == null)
            {
                await operation();
                return;
            }

            try
            {
                // Execute before interceptors in order
                foreach (var interceptor in _interceptors)
                {
                    await interceptor.OnBeforeInsertAsync(entity, cancellationToken);
                }

                // Execute the actual operation
                await operation();

                // Execute after interceptors in reverse order
                foreach (var interceptor in _interceptors.Reverse())
                {
                    await interceptor.OnAfterInsertAsync(entity, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                TelemetryHelper.Execute(TelemetryLevels.Error,
                    $"mongodb-interceptor-pipeline-insert-failed-{typeof(T).Name}",
                    ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ExecuteUpdateAsync<T>(T entity, Func<Task> operation, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            if (entity == null)
            {
                await operation();
                return;
            }

            try
            {
                // Execute before interceptors in order
                foreach (var interceptor in _interceptors)
                {
                    await interceptor.OnBeforeUpdateAsync(entity, cancellationToken);
                }

                // Execute the actual operation
                await operation();

                // Execute after interceptors in reverse order
                foreach (var interceptor in _interceptors.Reverse())
                {
                    await interceptor.OnAfterUpdateAsync(entity, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                TelemetryHelper.Execute(TelemetryLevels.Error,
                    $"mongodb-interceptor-pipeline-update-failed-{typeof(T).Name}",
                    ex.Message);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExecuteDeleteAsync<T>(
            T entity,
            Func<Task> hardDeleteOperation,
            Func<Task> softDeleteOperation,
            CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            if (entity == null)
            {
                await hardDeleteOperation();
                return false;
            }

            try
            {
                var wasSoftDeleted = false;
                var shouldProceed = true;

                // Execute before interceptors and collect results
                foreach (var interceptor in _interceptors)
                {
                    var result = await interceptor.OnBeforeDeleteAsync(entity, cancellationToken);

                    if (result.Suppress)
                    {
                        shouldProceed = false;
                        break;
                    }

                    if (result.ConvertToSoftDelete)
                    {
                        wasSoftDeleted = true;
                        shouldProceed = true;
                    }
                }

                if (shouldProceed)
                {
                    // Execute the appropriate operation
                    if (wasSoftDeleted)
                    {
                        await softDeleteOperation();
                    }
                    else
                    {
                        await hardDeleteOperation();
                    }

                    // Execute after interceptors in reverse order
                    foreach (var interceptor in _interceptors.Reverse())
                    {
                        await interceptor.OnAfterDeleteAsync(entity, wasSoftDeleted, cancellationToken);
                    }
                }

                return wasSoftDeleted;
            }
            catch (Exception ex)
            {
                TelemetryHelper.Execute(TelemetryLevels.Error,
                    $"mongodb-interceptor-pipeline-delete-failed-{typeof(T).Name}",
                    ex.Message);
                throw;
            }
        }
    }

    /// <summary>
    /// Interface for the MongoDB interceptor pipeline.
    /// </summary>
    public interface IMongoDbInterceptorPipeline
    {
        /// <summary>
        /// Gets a value indicating whether any interceptors are registered.
        /// </summary>
        bool HasInterceptors { get; }

        /// <summary>
        /// Gets the number of registered interceptors.
        /// </summary>
        int InterceptorCount { get; }

        /// <summary>
        /// Executes an insert operation with interceptors.
        /// </summary>
        Task ExecuteInsertAsync<T>(T entity, Func<Task> operation, CancellationToken cancellationToken = default)
            where T : class, IEntityBase;

        /// <summary>
        /// Executes an update operation with interceptors.
        /// </summary>
        Task ExecuteUpdateAsync<T>(T entity, Func<Task> operation, CancellationToken cancellationToken = default)
            where T : class, IEntityBase;

        /// <summary>
        /// Executes a delete operation with interceptors.
        /// </summary>
        /// <returns>True if the operation was converted to soft delete, false otherwise.</returns>
        Task<bool> ExecuteDeleteAsync<T>(
            T entity,
            Func<Task> hardDeleteOperation,
            Func<Task> softDeleteOperation,
            CancellationToken cancellationToken = default)
            where T : class, IEntityBase;
    }

    /// <summary>
    /// A no-op implementation of the interceptor pipeline for scenarios without interceptors.
    /// </summary>
    public class NoOpInterceptorPipeline : IMongoDbInterceptorPipeline
    {
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static NoOpInterceptorPipeline Instance { get; } = new NoOpInterceptorPipeline();

        /// <inheritdoc />
        public bool HasInterceptors => false;

        /// <inheritdoc />
        public int InterceptorCount => 0;

        /// <inheritdoc />
        public Task ExecuteInsertAsync<T>(T entity, Func<Task> operation, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
            => operation();

        /// <inheritdoc />
        public Task ExecuteUpdateAsync<T>(T entity, Func<Task> operation, CancellationToken cancellationToken = default)
            where T : class, IEntityBase
            => operation();

        /// <inheritdoc />
        public async Task<bool> ExecuteDeleteAsync<T>(
            T entity,
            Func<Task> hardDeleteOperation,
            Func<Task> softDeleteOperation,
            CancellationToken cancellationToken = default)
            where T : class, IEntityBase
        {
            await hardDeleteOperation();
            return false;
        }
    }
}

