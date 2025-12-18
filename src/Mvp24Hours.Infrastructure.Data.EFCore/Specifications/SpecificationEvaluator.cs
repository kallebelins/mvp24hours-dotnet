//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.EntityFrameworkCore;
using Mvp24Hours.Core.Contract.Domain.Entity;
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Data.EFCore.Specifications
{
    /// <summary>
    /// Evaluates specifications and applies them to IQueryable for Entity Framework Core.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    /// <remarks>
    /// <para>
    /// This evaluator supports:
    /// <list type="bullet">
    /// <item>Criteria filtering via Where clause</item>
    /// <item>Include statements for eager loading (expression and string-based)</item>
    /// <item>OrderBy/OrderByDescending for sorting</item>
    /// <item>Skip/Take for pagination</item>
    /// </list>
    /// </para>
    /// <para>
    /// This class implements <see cref="ISpecificationEvaluator{T}"/> to enable dependency injection
    /// and allow swapping evaluator implementations in different contexts.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Register in DI
    /// services.AddScoped(typeof(ISpecificationEvaluator&lt;&gt;), typeof(SpecificationEvaluator&lt;&gt;));
    /// 
    /// // Use in service
    /// public class CustomerService
    /// {
    ///     private readonly ISpecificationEvaluator&lt;Customer&gt; _evaluator;
    ///     
    ///     public CustomerService(ISpecificationEvaluator&lt;Customer&gt; evaluator)
    ///     {
    ///         _evaluator = evaluator;
    ///     }
    ///     
    ///     public IList&lt;Customer&gt; GetActiveCustomers(DbSet&lt;Customer&gt; customers)
    ///     {
    ///         var spec = new ActiveCustomerSpecification();
    ///         return _evaluator.GetQuery(customers, spec).ToList();
    ///     }
    /// }
    /// </code>
    /// </example>
    public class SpecificationEvaluator<T> : ISpecificationEvaluator<T>
        where T : class, IEntityBase
    {
        /// <summary>
        /// Default evaluator instance.
        /// </summary>
        public static SpecificationEvaluator<T> Default { get; } = new();

        /// <summary>
        /// Applies the specification to the query source.
        /// </summary>
        /// <param name="inputQuery">The source IQueryable</param>
        /// <param name="specification">The specification to apply</param>
        /// <returns>The modified IQueryable with specification applied</returns>
        public virtual IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecificationQuery<T> specification)
        {
            var query = inputQuery;

            // Apply criteria (Where clause)
            if (specification.IsSatisfiedByExpression != null)
            {
                query = query.Where(specification.IsSatisfiedByExpression);
            }

            // Handle enhanced specifications with includes, ordering, and paging
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
        /// Applies enhanced specification features (includes, ordering, paging).
        /// </summary>
        private IQueryable<T> ApplyEnhancedFeatures(IQueryable<T> query, ISpecificationQueryEnhanced<T> specification)
        {
            // Apply includes (expression-based)
            if (specification.Includes != null)
            {
                foreach (var include in specification.Includes)
                {
                    query = query.Include(include);
                }
            }

            // Apply includes (string-based for multi-level)
            if (specification.IncludeStrings != null)
            {
                foreach (var includeString in specification.IncludeStrings)
                {
                    query = query.Include(includeString);
                }
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
    /// Non-generic specification evaluator for general use.
    /// </summary>
    public static class SpecificationEvaluator
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
            return SpecificationEvaluator<T>.Default.GetQuery(inputQuery, specification);
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
            return SpecificationEvaluator<T>.Default.GetQuery(inputQuery, specification);
        }
    }
}

