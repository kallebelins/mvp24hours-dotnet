//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.EFCore
{
    /// <summary>
    /// Repository implementation with streaming support using IAsyncEnumerable.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <remarks>
    /// <para>
    /// This repository extends the standard async repository with streaming capabilities.
    /// Streaming queries use <see cref="IAsyncEnumerable{T}"/> to process results one at a time,
    /// avoiding loading entire result sets into memory.
    /// </para>
    /// <para>
    /// Use streaming for:
    /// <list type="bullet">
    /// <item>Large data exports</item>
    /// <item>ETL processes</item>
    /// <item>Background batch processing</item>
    /// <item>Memory-constrained environments</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Inject IStreamingRepositoryAsync&lt;Customer&gt;
    /// public class CustomerExportService
    /// {
    ///     private readonly IStreamingRepositoryAsync&lt;Customer&gt; _repository;
    ///     
    ///     public async Task ExportToCsvAsync(string filePath, CancellationToken ct)
    ///     {
    ///         await using var writer = File.CreateText(filePath);
    ///         
    ///         await foreach (var customer in _repository.StreamAllAsync(ct))
    ///         {
    ///             await writer.WriteLineAsync($"{customer.Id},{customer.Name},{customer.Email}");
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    public class StreamingRepositoryAsync<T>(DbContext _dbContext, IOptions<EFCoreRepositoryOptions> options, ILogger<StreamingRepositoryAsync<T>> logger)
        : RepositoryAsync<T>(_dbContext, options), IStreamingRepositoryAsync<T>
        where T : class, IEntityBase
    {
        private readonly ILogger<StreamingRepositoryAsync<T>> _logger = logger;
        #region IStreamingQueryAsync Implementation

        /// <inheritdoc />
        public async IAsyncEnumerable<T> StreamAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = GetQuery(null, true);

            // Apply default tracking behavior from options
            if (Options.DefaultTrackingBehavior == QueryTrackingBehavior.NoTracking)
            {
                query = query.AsNoTracking();
            }
            else if (Options.DefaultTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            {
                query = query.AsNoTrackingWithIdentityResolution();
            }

            // Apply query tag if enabled
            if (Options.EnableQueryTags)
            {
                query = query.TagWith($"{Options.QueryTagPrefix}: StreamAllAsync");
            }

            await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return entity;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> StreamAllAsync(IPagingCriteria criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = GetQuery(criteria);

            // Apply default tracking behavior from options
            if (Options.DefaultTrackingBehavior == QueryTrackingBehavior.NoTracking)
            {
                query = query.AsNoTracking();
            }
            else if (Options.DefaultTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            {
                query = query.AsNoTrackingWithIdentityResolution();
            }

            // Apply query tag if enabled
            if (Options.EnableQueryTags)
            {
                query = query.TagWith($"{Options.QueryTagPrefix}: StreamAllAsync (with criteria)");
            }

            await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return entity;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> StreamByAsync(Expression<Func<T, bool>> clause, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = dbEntities.AsQueryable();

            if (clause != null)
            {
                query = query.Where(clause);
            }

            query = GetQuery(query, null, true);

            // Apply default tracking behavior from options
            if (Options.DefaultTrackingBehavior == QueryTrackingBehavior.NoTracking)
            {
                query = query.AsNoTracking();
            }
            else if (Options.DefaultTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            {
                query = query.AsNoTrackingWithIdentityResolution();
            }

            // Apply query tag if enabled
            if (Options.EnableQueryTags)
            {
                query = query.TagWith($"{Options.QueryTagPrefix}: StreamByAsync");
            }

            await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return entity;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<T> StreamByAsync(Expression<Func<T, bool>> clause, IPagingCriteria criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = dbEntities.AsQueryable();

            if (clause != null)
            {
                query = query.Where(clause);
            }

            query = GetQuery(query, criteria);

            // Apply default tracking behavior from options
            if (Options.DefaultTrackingBehavior == QueryTrackingBehavior.NoTracking)
            {
                query = query.AsNoTracking();
            }
            else if (Options.DefaultTrackingBehavior == QueryTrackingBehavior.NoTrackingWithIdentityResolution)
            {
                query = query.AsNoTrackingWithIdentityResolution();
            }

            // Apply query tag if enabled
            if (Options.EnableQueryTags)
            {
                query = query.TagWith($"{Options.QueryTagPrefix}: StreamByAsync (with criteria)");
            }

            await foreach (var entity in query.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return entity;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<IList<T>> StreamBatchesAsync(int batchSize, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var batch = new List<T>(batchSize);

            await foreach (var entity in StreamAllAsync(cancellationToken))
            {
                batch.Add(entity);

                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            // Return remaining items
            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<IList<T>> StreamBatchesAsync(Expression<Func<T, bool>> clause, int batchSize, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var batch = new List<T>(batchSize);

            await foreach (var entity in StreamByAsync(clause, cancellationToken))
            {
                batch.Add(entity);

                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            // Return remaining items
            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        #endregion

        #region Additional Streaming Methods

        /// <summary>
        /// Streams entities with projection to a DTO type.
        /// </summary>
        /// <typeparam name="TResult">The result DTO type.</typeparam>
        /// <param name="selector">Projection expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of projected results.</returns>
        /// <remarks>
        /// <para>
        /// Projection streaming only fetches the columns needed for the DTO,
        /// providing both memory and bandwidth savings.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// await foreach (var dto in repository.StreamProjectedAsync(
        ///     c => new CustomerDto { Id = c.Id, Name = c.Name }))
        /// {
        ///     await ProcessDtoAsync(dto);
        /// }
        /// </code>
        /// </example>
        public async IAsyncEnumerable<TResult> StreamProjectedAsync<TResult>(
            Expression<Func<T, TResult>> selector,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = dbEntities.AsNoTracking();

            if (Options.EnableQueryTags)
            {
                query = query.TagWith($"{Options.QueryTagPrefix}: StreamProjectedAsync");
            }

            await foreach (var item in query.Select(selector).AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Streams entities with projection and filtering.
        /// </summary>
        /// <typeparam name="TResult">The result DTO type.</typeparam>
        /// <param name="clause">Filter expression.</param>
        /// <param name="selector">Projection expression.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An async enumerable of projected results.</returns>
        public async IAsyncEnumerable<TResult> StreamProjectedByAsync<TResult>(
            Expression<Func<T, bool>> clause,
            Expression<Func<T, TResult>> selector,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var query = dbEntities.AsNoTracking();

            if (clause != null)
            {
                query = query.Where(clause);
            }

            if (Options.EnableQueryTags)
            {
                query = query.TagWith($"{Options.QueryTagPrefix}: StreamProjectedByAsync");
            }

            await foreach (var item in query.Select(selector).AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }

        /// <summary>
        /// Streams entities with parallel processing callback.
        /// </summary>
        /// <param name="processAsync">Async function to process each entity.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel tasks.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the streaming operation.</returns>
        /// <remarks>
        /// <para>
        /// Combines streaming with parallel processing for high-throughput scenarios.
        /// Entities are streamed from the database and processed concurrently.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// await repository.StreamAndProcessAsync(
        ///     async customer => await SendEmailAsync(customer),
        ///     maxDegreeOfParallelism: 4);
        /// </code>
        /// </example>
        public async Task StreamAndProcessAsync(
            Func<T, CancellationToken, Task> processAsync,
            int maxDegreeOfParallelism = 4,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Stream and process started. MaxDegreeOfParallelism: {MaxDegreeOfParallelism}", maxDegreeOfParallelism);

            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();

            await foreach (var entity in StreamAllAsync(cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await processAsync(entity, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            _logger.LogDebug("Stream and process completed");
        }

        /// <summary>
        /// Streams entities with filtered parallel processing.
        /// </summary>
        /// <param name="clause">Filter expression.</param>
        /// <param name="processAsync">Async function to process each entity.</param>
        /// <param name="maxDegreeOfParallelism">Maximum parallel tasks.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task representing the streaming operation.</returns>
        public async Task StreamAndProcessAsync(
            Expression<Func<T, bool>> clause,
            Func<T, CancellationToken, Task> processAsync,
            int maxDegreeOfParallelism = 4,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Stream and process (with clause) started. MaxDegreeOfParallelism: {MaxDegreeOfParallelism}", maxDegreeOfParallelism);

            using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
            var tasks = new List<Task>();

            await foreach (var entity in StreamByAsync(clause, cancellationToken))
            {
                await semaphore.WaitAsync(cancellationToken);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        await processAsync(entity, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            _logger.LogDebug("Stream and process (with clause) completed");
        }

        #endregion
    }
}

