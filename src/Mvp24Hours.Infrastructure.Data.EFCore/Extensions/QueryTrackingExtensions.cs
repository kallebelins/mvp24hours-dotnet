//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Enums.Infrastructure;
using Mvp24Hours.Helpers;
using System;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Extensions
{
    /// <summary>
    /// Extension methods for configuring query tracking behavior in EF Core.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Query tracking determines whether EF Core keeps track of entity changes.
    /// Disabling tracking improves performance for read-only scenarios.
    /// </para>
    /// <para>
    /// <list type="bullet">
    /// <item><see cref="AsNoTracking{T}"/> - Best for simple read-only queries</item>
    /// <item><see cref="AsNoTrackingWithIdentityResolution{T}"/> - Best for complex graphs with repeated entities</item>
    /// <item><see cref="AsTracking{T}"/> - Required when entities will be modified</item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class QueryTrackingExtensions
    {
        /// <summary>
        /// Applies NoTracking to the query. Entities returned will not be tracked by the DbContext.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <returns>A query with NoTracking applied.</returns>
        /// <remarks>
        /// <para>
        /// Use this for read-only scenarios where you don't need to modify entities.
        /// This provides the best performance for simple queries.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// var customers = await dbContext.Customers
        ///     .AsNoTracking()
        ///     .Where(c => c.IsActive)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> query)
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-querytracking-asnotracking");
            return EntityFrameworkQueryableExtensions.AsNoTracking(query);
        }

        /// <summary>
        /// Applies NoTracking with identity resolution to the query.
        /// Entities are not tracked, but EF Core resolves entity identity within the query result.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <returns>A query with NoTrackingWithIdentityResolution applied.</returns>
        /// <remarks>
        /// <para>
        /// Use this when loading complex entity graphs where the same entity may appear multiple times.
        /// EF Core will ensure the same entity instance is used throughout the graph.
        /// </para>
        /// <para>
        /// This has slightly more overhead than pure NoTracking but ensures consistency.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Orders may reference the same Customer multiple times
        /// var orders = await dbContext.Orders
        ///     .AsNoTrackingWithIdentityResolution()
        ///     .Include(o => o.Customer)
        ///     .Include(o => o.ShippingAddress)
        ///     .ThenInclude(a => a.Customer) // Same customer as above
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> AsNoTrackingWithIdentityResolution<T>(this IQueryable<T> query)
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-querytracking-asnotrackingwithidentityresolution");
            return EntityFrameworkQueryableExtensions.AsNoTrackingWithIdentityResolution(query);
        }

        /// <summary>
        /// Explicitly enables tracking for the query. Useful when the default behavior is NoTracking.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <returns>A query with tracking enabled.</returns>
        /// <remarks>
        /// <para>
        /// Use this when you need to modify entities in a context where NoTracking is the default.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// // Repository configured with NoTracking by default
        /// var customer = await repository
        ///     .GetQuery(null)
        ///     .AsTracking() // Enable tracking for this specific query
        ///     .FirstOrDefaultAsync(c => c.Id == customerId);
        /// 
        /// customer.Name = "Updated Name";
        /// await unitOfWork.SaveChangesAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> AsTracking<T>(this IQueryable<T> query)
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, "efcore-querytracking-astracking");
            return EntityFrameworkQueryableExtensions.AsTracking(query);
        }

        /// <summary>
        /// Applies tracking behavior based on the specified option.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="trackingBehavior">The tracking behavior to apply.</param>
        /// <returns>A query with the specified tracking behavior.</returns>
        /// <example>
        /// <code>
        /// var query = dbContext.Customers.AsQueryable();
        /// query = query.WithTracking(QueryTrackingBehavior.NoTracking);
        /// </code>
        /// </example>
        public static IQueryable<T> WithTracking<T>(this IQueryable<T> query, QueryTrackingBehavior trackingBehavior)
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, $"efcore-querytracking-withtracking-{trackingBehavior}");

            return trackingBehavior switch
            {
                QueryTrackingBehavior.NoTracking => EntityFrameworkQueryableExtensions.AsNoTracking(query),
                QueryTrackingBehavior.NoTrackingWithIdentityResolution => EntityFrameworkQueryableExtensions.AsNoTrackingWithIdentityResolution(query),
                QueryTrackingBehavior.TrackAll => EntityFrameworkQueryableExtensions.AsTracking(query),
                _ => query
            };
        }

        /// <summary>
        /// Conditionally applies NoTracking based on a predicate.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="condition">When true, applies NoTracking.</param>
        /// <returns>The query with conditional tracking applied.</returns>
        /// <example>
        /// <code>
        /// var customers = await repository
        ///     .GetQuery(null)
        ///     .AsNoTrackingIf(isReadOnly) // Only apply NoTracking when reading
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> AsNoTrackingIf<T>(this IQueryable<T> query, bool condition)
            where T : class, IEntityBase
        {
            return condition ? AsNoTracking(query) : query;
        }

        /// <summary>
        /// Conditionally applies NoTrackingWithIdentityResolution based on a predicate.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="condition">When true, applies NoTrackingWithIdentityResolution.</param>
        /// <returns>The query with conditional tracking applied.</returns>
        public static IQueryable<T> AsNoTrackingWithIdentityResolutionIf<T>(this IQueryable<T> query, bool condition)
            where T : class, IEntityBase
        {
            return condition ? AsNoTrackingWithIdentityResolution(query) : query;
        }

        /// <summary>
        /// Applies tracking behavior optimized for read-only operations.
        /// Uses NoTrackingWithIdentityResolution when includes are present, otherwise NoTracking.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="hasIncludes">Whether the query has Include operations.</param>
        /// <returns>A query optimized for reading.</returns>
        /// <example>
        /// <code>
        /// var hasIncludes = criteria?.Navigation.AnySafe() == true;
        /// var customers = await repository
        ///     .GetQuery(criteria)
        ///     .OptimizeForReading(hasIncludes)
        ///     .ToListAsync();
        /// </code>
        /// </example>
        public static IQueryable<T> OptimizeForReading<T>(this IQueryable<T> query, bool hasIncludes = false)
            where T : class, IEntityBase
        {
            TelemetryHelper.Execute(TelemetryLevels.Verbose, $"efcore-querytracking-optimizeforreading-hasincludes:{hasIncludes}");

            // Use identity resolution when includes might create multiple references to same entity
            return hasIncludes
                ? EntityFrameworkQueryableExtensions.AsNoTrackingWithIdentityResolution(query)
                : EntityFrameworkQueryableExtensions.AsNoTracking(query);
        }
    }
}

