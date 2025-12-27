//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using System;
using System.Linq;
using System.Linq.Expressions;

#nullable enable

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Specifications
{
    /// <summary>
    /// Evaluates specifications and applies them to IQueryable for MongoDB Driver.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <remarks>
    /// <para>
    /// This evaluator supports:
    /// <list type="bullet">
    ///   <item>Criteria filtering via Where clause</item>
    ///   <item>OrderBy/OrderByDescending for sorting</item>
    ///   <item>Skip/Take for pagination</item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: MongoDB does not support Include() like EF Core.
    /// For related data, consider embedding documents or using $lookup aggregation.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var spec = new ActiveCustomerSpecification();
    /// var query = MongoDbSpecificationEvaluator&lt;Customer&gt;.Default.GetQuery(collection.AsQueryable(), spec);
    /// var results = await query.ToListAsync();
    /// </code>
    /// </example>
    public class MongoDbSpecificationEvaluator<T>
        where T : class, IEntityBase
    {
        /// <summary>
        /// Default evaluator instance.
        /// </summary>
        public static MongoDbSpecificationEvaluator<T> Default { get; } = new();

        /// <summary>
        /// Applies the specification to the query source.
        /// </summary>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        public virtual IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecificationQuery<T> specification)
        {
            if (specification == null)
            {
                throw new ArgumentNullException(nameof(specification));
            }

            var query = inputQuery;

            // Apply criteria (Where clause)
            if (specification.IsSatisfiedByExpression != null)
            {
                query = query.Where(specification.IsSatisfiedByExpression);
            }

            // Handle enhanced specifications with ordering and paging
            if (specification is ISpecificationQueryEnhanced<T> enhancedSpec)
            {
                query = ApplyEnhancedFeatures(query, enhancedSpec);
            }

            return query;
        }

        /// <summary>
        /// Applies the specification to the query source using the Specification base class.
        /// </summary>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        public virtual IQueryable<T> GetQuery(IQueryable<T> inputQuery, Specification<T> specification)
        {
            return GetQuery(inputQuery, (ISpecificationQuery<T>)specification);
        }

        /// <summary>
        /// Applies enhanced specification features (ordering, paging).
        /// Note: MongoDB does not support Include() - use embedded documents or $lookup.
        /// </summary>
        private static IQueryable<T> ApplyEnhancedFeatures(IQueryable<T> query, ISpecificationQueryEnhanced<T> specification)
        {
            // Log warning if includes are specified (not supported in MongoDB via IQueryable)
            if ((specification.Includes != null && specification.Includes.Count > 0) ||
                (specification.IncludeStrings != null && specification.IncludeStrings.Count > 0))
            {
                // Includes are not supported in MongoDB IQueryable
                // Consider using aggregation pipeline with $lookup instead
                // Note: Logging removed - this is a design limitation, not an error
            }

            // Apply ordering
            if (specification.OrderBy != null && specification.OrderBy.Count > 0)
            {
                IOrderedQueryable<T> orderedQuery = null;

                foreach (var (keySelector, descending) in specification.OrderBy)
                {
                    if (orderedQuery == null)
                    {
                        orderedQuery = descending
                            ? query.OrderByDescending(keySelector)
                            : query.OrderBy(keySelector);
                    }
                    else
                    {
                        orderedQuery = descending
                            ? orderedQuery.ThenByDescending(keySelector)
                            : orderedQuery.ThenBy(keySelector);
                    }
                }

                query = orderedQuery ?? query;
            }

            // Apply paging
            if (specification.Skip.HasValue && specification.Skip.Value > 0)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue && specification.Take.Value > 0)
            {
                query = query.Take(specification.Take.Value);
            }

            return query;
        }
    }

    /// <summary>
    /// Non-generic specification evaluator for general use with MongoDB.
    /// </summary>
    public static class MongoDbSpecificationEvaluator
    {
        /// <summary>
        /// Gets a query with the specification applied.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecificationQuery<T> specification)
            where T : class, IEntityBase
        {
            return MongoDbSpecificationEvaluator<T>.Default.GetQuery(inputQuery, specification);
        }

        /// <summary>
        /// Gets a query with the specification applied.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        public static IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, Specification<T> specification)
            where T : class, IEntityBase
        {
            return MongoDbSpecificationEvaluator<T>.Default.GetQuery(inputQuery, specification);
        }

        /// <summary>
        /// Converts a specification to a MongoDB FilterDefinition.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specification">The specification to convert</param>
        /// <returns>A MongoDB FilterDefinition based on the specification criteria</returns>
        /// <remarks>
        /// This method extracts the filter expression from a specification and converts it
        /// to a MongoDB FilterDefinition, which can be used with the MongoDB Driver API
        /// for more advanced operations like aggregation pipelines.
        /// </remarks>
        /// <example>
        /// <code>
        /// var spec = new ActiveCustomerSpecification();
        /// var filter = MongoDbSpecificationEvaluator.ToFilterDefinition(spec);
        /// 
        /// // Use with aggregation pipeline
        /// var pipeline = collection.Aggregate()
        ///     .Match(filter)
        ///     .SortByDescending(c => c.CreatedAt)
        ///     .Limit(10);
        /// </code>
        /// </example>
        public static FilterDefinition<T> ToFilterDefinition<T>(ISpecificationQuery<T> specification)
            where T : class
        {
            if (specification?.IsSatisfiedByExpression == null)
            {
                return Builders<T>.Filter.Empty;
            }

            return Builders<T>.Filter.Where(specification.IsSatisfiedByExpression);
        }

        /// <summary>
        /// Converts a specification to a MongoDB SortDefinition.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specification">The specification to convert</param>
        /// <returns>A MongoDB SortDefinition based on the specification ordering, or null if no ordering is specified</returns>
        public static SortDefinition<T>? ToSortDefinition<T>(ISpecificationQueryEnhanced<T> specification)
            where T : class
        {
            if (specification?.OrderBy == null || specification.OrderBy.Count == 0)
            {
                return null;
            }

            var sortBuilder = Builders<T>.Sort;
            SortDefinition<T>? sort = null;

            foreach (var (keySelector, descending) in specification.OrderBy)
            {
                var fieldName = GetFieldName(keySelector);
                var fieldSort = descending
                    ? sortBuilder.Descending(fieldName)
                    : sortBuilder.Ascending(fieldName);

                sort = sort == null ? fieldSort : sortBuilder.Combine(sort, fieldSort);
            }

            return sort;
        }

        /// <summary>
        /// Extracts the field name from an expression.
        /// </summary>
        private static string GetFieldName<T>(Expression<Func<T, object>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null && expression.Body is UnaryExpression unaryExpression)
            {
                memberExpression = unaryExpression.Operand as MemberExpression;
            }

            return memberExpression?.Member.Name ?? expression.ToString();
        }
    }
}

