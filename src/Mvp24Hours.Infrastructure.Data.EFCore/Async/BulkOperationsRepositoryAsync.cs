//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Data.EFCore.Configuration;
using Mvp24Hours.Infrastructure.Data.EFCore.Extensions;

namespace Mvp24Hours.Infrastructure.Data.EFCore;

/// <summary>
/// Repository with support for high-performance bulk operations.
/// </summary>
/// <typeparam name="T">Entity type.</typeparam>
/// <remarks>
/// <para>
/// This repository extends the standard repository with bulk operations for:
/// <list type="bullet">
/// <item>Bulk Insert - Insert thousands of entities efficiently</item>
/// <item>Bulk Update - Update entities by primary key</item>
/// <item>Bulk Delete - Delete entities by primary key</item>
/// <item>Execute Update - Update entities matching a condition (.NET 7+)</item>
/// <item>Execute Delete - Delete entities matching a condition (.NET 7+)</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in DI
/// services.AddScoped(typeof(IBulkOperationsRepositoryAsync&lt;&gt;), typeof(BulkOperationsRepositoryAsync&lt;&gt;));
/// 
/// // Use in service
/// public class CustomerService
/// {
///     private readonly IBulkOperationsRepositoryAsync&lt;Customer&gt; _repository;
///     
///     public async Task ImportCustomers(IList&lt;Customer&gt; customers)
///     {
///         var result = await _repository.BulkInsertAsync(customers, new BulkOperationOptions
///         {
///             BatchSize = 2000,
///             ProgressCallback = (processed, total) =&gt; Console.WriteLine($"{processed}/{total}")
///         });
///     }
/// }
/// </code>
/// </example>
public class BulkOperationsRepositoryAsync<T>(DbContext _dbContext, IOptions<EFCoreRepositoryOptions> options, ILogger<BulkOperationsRepositoryAsync<T>> logger)
    : RepositoryAsync<T>(_dbContext, options), IBulkOperationsRepositoryAsync<T>
    where T : class, IEntityBase
{
    private readonly ILogger<BulkOperationsRepositoryAsync<T>> _logger = logger;
    #region IBulkOperationsAsync - Bulk Insert

    /// <inheritdoc />
    public Task<BulkOperationResult> BulkInsertAsync(
        IList<T> entities,
        CancellationToken cancellationToken = default)
    {
        return BulkInsertAsync(entities, new BulkOperationOptions(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkInsertAsync(
        IList<T> entities,
        BulkOperationOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!entities.AnySafe())
        {
            return BulkOperationResult.Success(0, TimeSpan.Zero);
        }

        _logger.LogDebug("Bulk insert started. Count: {Count}, BatchSize: {BatchSize}", entities.Count, options.BatchSize);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            int totalCount = entities.Count;
            int processedCount = 0;

            // Process in batches
            foreach (IEnumerable<T> batch in Batch(entities, options.BatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (options.BypassChangeTracking)
                {
                    await dbEntities.AddRangeAsync(batch, cancellationToken);
                }
                else
                {
                    foreach (T? entity in batch)
                    {
                        await AddAsync(entity, cancellationToken);
                    }
                }

                processedCount += batch.Count();
                options.ProgressCallback?.Invoke(processedCount, totalCount);
            }

            // Save changes
            int rowsAffected = await dbContext.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogDebug("Bulk insert completed. Rows affected: {RowsAffected}, Elapsed: {ElapsedMs}ms", rowsAffected, stopwatch.ElapsedMilliseconds);

            return BulkOperationResult.Success(rowsAffected, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Bulk insert failed. Elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    #endregion

    #region IBulkOperationsAsync - Bulk Update

    /// <inheritdoc />
    public Task<BulkOperationResult> BulkUpdateAsync(
        IList<T> entities,
        CancellationToken cancellationToken = default)
    {
        return BulkUpdateAsync(entities, new BulkOperationOptions(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkUpdateAsync(
        IList<T> entities,
        BulkOperationOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!entities.AnySafe())
        {
            return BulkOperationResult.Success(0, TimeSpan.Zero);
        }

        _logger.LogDebug("Bulk update started. Count: {Count}, BatchSize: {BatchSize}", entities.Count, options.BatchSize);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            int totalCount = entities.Count;
            int processedCount = 0;

            // Process in batches
            foreach (IEnumerable<T> batch in Batch(entities, options.BatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (options.BypassChangeTracking)
                {
                    dbEntities.UpdateRange(batch);
                }
                else
                {
                    foreach (T? entity in batch)
                    {
                        EntityEntry<T> entry = dbContext.Entry(entity);
                        if (entry.State == EntityState.Detached)
                        {
                            dbEntities.Attach(entity);
                        }
                        entry.State = EntityState.Modified;
                    }
                }

                processedCount += batch.Count();
                options.ProgressCallback?.Invoke(processedCount, totalCount);
            }

            // Save changes
            int rowsAffected = await dbContext.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogDebug("Bulk update completed. Rows affected: {RowsAffected}, Elapsed: {ElapsedMs}ms", rowsAffected, stopwatch.ElapsedMilliseconds);

            return BulkOperationResult.Success(rowsAffected, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Bulk update failed. Elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    #endregion

    #region IBulkOperationsAsync - Bulk Delete

    /// <inheritdoc />
    public Task<BulkOperationResult> BulkDeleteAsync(
        IList<T> entities,
        CancellationToken cancellationToken = default)
    {
        return BulkDeleteAsync(entities, new BulkOperationOptions(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<BulkOperationResult> BulkDeleteAsync(
        IList<T> entities,
        BulkOperationOptions options,
        CancellationToken cancellationToken = default)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (!entities.AnySafe())
        {
            return BulkOperationResult.Success(0, TimeSpan.Zero);
        }

        _logger.LogDebug("Bulk delete started. Count: {Count}, BatchSize: {BatchSize}", entities.Count, options.BatchSize);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            int totalCount = entities.Count;
            int processedCount = 0;

            // Process in batches
            foreach (IEnumerable<T> batch in Batch(entities, options.BatchSize))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (options.BypassChangeTracking)
                {
                    dbEntities.RemoveRange(batch);
                }
                else
                {
                    foreach (T? entity in batch)
                    {
                        EntityEntry<T> entry = dbContext.Entry(entity);
                        if (entry.State == EntityState.Detached)
                        {
                            dbEntities.Attach(entity);
                        }
                        entry.State = EntityState.Deleted;
                    }
                }

                processedCount += batch.Count();
                options.ProgressCallback?.Invoke(processedCount, totalCount);
            }

            // Save changes
            int rowsAffected = await dbContext.SaveChangesAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogDebug("Bulk delete completed. Rows affected: {RowsAffected}, Elapsed: {ElapsedMs}ms", rowsAffected, stopwatch.ElapsedMilliseconds);

            return BulkOperationResult.Success(rowsAffected, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Bulk delete failed. Elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            return BulkOperationResult.Failure(ex.Message, stopwatch.Elapsed);
        }
    }

    #endregion

    #region IBulkOperationsAsync - Execute Update (.NET 7+)

    /// <inheritdoc />
    public async Task<int> ExecuteUpdateAsync<TProperty>(
        Expression<Func<T, bool>> predicate,
        Expression<Func<T, TProperty>> property,
        TProperty value,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        _logger.LogDebug("Execute update started");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            int rowsAffected = await dbEntities
                .Where(predicate)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(property, value),
                    cancellationToken);

            stopwatch.Stop();

            _logger.LogDebug("Execute update completed. Rows affected: {RowsAffected}, Elapsed: {ElapsedMs}ms", rowsAffected, stopwatch.ElapsedMilliseconds);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Execute update failed. Elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> ExecuteUpdateAsync(
        Expression<Func<T, bool>> predicate,
        Expression<Func<SetPropertyCalls<T>, SetPropertyCalls<T>>> setPropertyCalls,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (setPropertyCalls == null)
        {
            throw new ArgumentNullException(nameof(setPropertyCalls));
        }

        _logger.LogDebug("Execute update (multi-property) started");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Build the Mvp24Hours SetPropertyCalls from the expression
            var ourSetters = new SetPropertyCalls<T>();
            setPropertyCalls.Compile()(ourSetters);

            IReadOnlyList<SetPropertyCall> setters = ourSetters.Setters;
            if (!setters.Any())
            {
                return 0;
            }

            // Build dynamic expression for EF Core's SetPropertyCalls
            int rowsAffected = await ExecuteUpdateWithDynamicSettersAsync(
                predicate,
                setters,
                cancellationToken);

            stopwatch.Stop();

            _logger.LogDebug("Execute update (multi-property) completed. Rows affected: {RowsAffected}, Elapsed: {ElapsedMs}ms", rowsAffected, stopwatch.ElapsedMilliseconds);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Execute update (multi-property) failed. Elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    private async Task<int> ExecuteUpdateWithDynamicSettersAsync(
        Expression<Func<T, bool>> predicate,
        IReadOnlyList<SetPropertyCall> setters,
        CancellationToken cancellationToken)
    {
        IQueryable<T> query = dbEntities.Where(predicate);

        return await query.ExecuteUpdateAsync(builder =>
        {
            foreach (SetPropertyCall setter in setters)
            {
                ApplyDynamicSetter(builder, setter);
            }
        }, cancellationToken);
    }

    private static void ApplyDynamicSetter(
        Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<T> builder,
        SetPropertyCall setter)
    {
        Type propertyType = GetPropertyTypeFromExpression(setter.Property);

        if (setter.ValueExpression != null)
        {
            MethodInfo setPropertyMethod = typeof(Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<T>)
                .GetMethods()
                .First(m => m.Name == "SetProperty" &&
                            m.GetParameters().Length == 2 &&
                            m.GetParameters()[1].ParameterType.IsGenericType &&
                            m.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Expression<>));

            setPropertyMethod.MakeGenericMethod(propertyType)
                .Invoke(builder, [setter.Property, setter.ValueExpression]);
        }
        else
        {
            MethodInfo setPropertyMethod = typeof(Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<T>)
                .GetMethods()
                .First(m => m.Name == "SetProperty" &&
                            m.GetParameters().Length == 2 &&
                            !m.GetParameters()[1].ParameterType.IsGenericType);

            setPropertyMethod.MakeGenericMethod(propertyType)
                .Invoke(builder, [setter.Property, setter.Value]);
        }
    }

    private static Type GetPropertyTypeFromExpression(LambdaExpression expression)
    {
        if (expression.Body is MemberExpression memberExpression)
        {
            return ((System.Reflection.PropertyInfo)memberExpression.Member).PropertyType;
        }
        return expression.ReturnType;
    }

    #endregion

    #region IBulkOperationsAsync - Execute Delete (.NET 7+)

    /// <inheritdoc />
    public async Task<int> ExecuteDeleteAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        _logger.LogDebug("Execute delete started");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            int rowsAffected = await dbEntities
                .Where(predicate)
                .ExecuteDeleteAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogDebug("Execute delete completed. Rows affected: {RowsAffected}, Elapsed: {ElapsedMs}ms", rowsAffected, stopwatch.ElapsedMilliseconds);

            return rowsAffected;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Execute delete failed. Elapsed: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    #endregion

    #region Helpers

    private static IEnumerable<IEnumerable<TItem>> Batch<TItem>(IEnumerable<TItem> source, int batchSize)
    {
        using IEnumerator<TItem> enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return YieldBatchElements(enumerator, batchSize - 1);
        }
    }

    private static IEnumerable<TItem> YieldBatchElements<TItem>(IEnumerator<TItem> source, int batchSize)
    {
        yield return source.Current;
        for (int i = 0; i < batchSize && source.MoveNext(); i++)
        {
            yield return source.Current;
        }
    }

    #endregion
}

