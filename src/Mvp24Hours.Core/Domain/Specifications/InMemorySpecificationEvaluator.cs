//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Specifications;
using System.Linq;

namespace Mvp24Hours.Core.Domain.Specifications
{
    /// <summary>
    /// In-memory specification evaluator that applies specifications to IQueryable without database-specific features.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <remarks>
    /// <para>
    /// This evaluator supports:
    /// <list type="bullet">
    /// <item>Criteria filtering via Where clause</item>
    /// <item>OrderBy/OrderByDescending for sorting (if enhanced specification)</item>
    /// <item>Skip/Take for pagination (if enhanced specification)</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Note:</strong> This evaluator does NOT support Include statements as those
    /// are database-specific. Use the EF Core or MongoDB specific evaluators for that.
    /// </para>
    /// </remarks>
    public class InMemorySpecificationEvaluator<T> : ISpecificationEvaluator<T>
        where T : class
    {
        /// <summary>
        /// Default evaluator instance.
        /// </summary>
        public static InMemorySpecificationEvaluator<T> Default { get; } = new();

        /// <inheritdoc />
        public virtual IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecificationQuery<T> specification)
        {
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

        /// <inheritdoc />
        public virtual IQueryable<T> GetQuery(IQueryable<T> inputQuery, Specification<T> specification)
        {
            return GetQuery(inputQuery, (ISpecificationQuery<T>)specification);
        }

        /// <summary>
        /// Applies enhanced specification features (ordering, paging).
        /// </summary>
        private static IQueryable<T> ApplyEnhancedFeatures(IQueryable<T> query, ISpecificationQueryEnhanced<T> specification)
        {
            // Apply ordering
            if (specification.OrderBy != null && specification.OrderBy.Count > 0)
            {
                IOrderedQueryable<T>? orderedQuery = null;

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

            // Apply pagination
            if (specification.Skip.HasValue)
            {
                query = query.Skip(specification.Skip.Value);
            }

            if (specification.Take.HasValue)
            {
                query = query.Take(specification.Take.Value);
            }

            return query;
        }
    }

    /// <summary>
    /// Non-generic in-memory specification evaluator for general use.
    /// </summary>
    public class InMemorySpecificationEvaluator : ISpecificationEvaluator
    {
        /// <summary>
        /// Default instance.
        /// </summary>
        public static InMemorySpecificationEvaluator Default { get; } = new();

        /// <inheritdoc />
        public IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, ISpecificationQuery<T> specification)
            where T : class
        {
            return InMemorySpecificationEvaluator<T>.Default.GetQuery(inputQuery, specification);
        }

        /// <inheritdoc />
        public IQueryable<T> GetQuery<T>(IQueryable<T> inputQuery, Specification<T> specification)
            where T : class
        {
            return InMemorySpecificationEvaluator<T>.Default.GetQuery(inputQuery, specification);
        }
    }
}

