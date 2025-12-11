//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Domain.Specifications;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for integrating Specification pattern with IPagingCriteria.
    /// </summary>
    public static class SpecificationPagingExtensions
    {
        #region [ Specification to PagingCriteria ]

        /// <summary>
        /// Converts a Specification to IPagingCriteriaExpression.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specification">The specification to convert</param>
        /// <param name="limit">Maximum number of records per page (default: 100)</param>
        /// <param name="offset">Page offset/index (default: 0)</param>
        /// <returns>A PagingCriteriaExpression with the specification's ordering and navigation</returns>
        public static IPagingCriteriaExpression<T> ToPagingCriteria<T>(
            this Specification<T> specification,
            int limit = 100,
            int offset = 0)
            where T : class
        {
            var paging = new PagingCriteriaExpression<T>(
                limit: specification.Take ?? limit,
                offset: specification.Skip.HasValue ? specification.Skip.Value / Math.Max(1, specification.Take ?? limit) : offset
            );

            // Add ordering expressions
            foreach (var (keySelector, descending) in specification.OrderBy)
            {
                if (descending)
                {
                    paging.OrderByDescendingExpr.Add(keySelector);
                }
                else
                {
                    paging.OrderByAscendingExpr.Add(keySelector);
                }
            }

            // Add navigation expressions
            foreach (var include in specification.Includes)
            {
                paging.NavigationExpr.Add(include);
            }

            return paging;
        }

        /// <summary>
        /// Converts a Specification to IPagingCriteriaExpression using specification's built-in paging.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specification">The specification to convert</param>
        /// <returns>A PagingCriteriaExpression with the specification's settings</returns>
        public static IPagingCriteriaExpression<T> ToPagingCriteria<T>(this Specification<T> specification)
            where T : class
        {
            return specification.ToPagingCriteria(100, 0);
        }

        #endregion

        #region [ PagingCriteria to Specification ]

        /// <summary>
        /// Creates a Specification from IPagingCriteria with ordering and navigation.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="criteria">The paging criteria</param>
        /// <param name="filter">Optional filter expression</param>
        /// <returns>A Specification with the criteria's settings applied</returns>
        public static Specification<T> ToSpecification<T>(
            this IPagingCriteria criteria,
            Expression<Func<T, bool>> filter = null)
            where T : class
        {
            var spec = new PagingSpecification<T>(
                filter ?? (x => true),
                criteria
            );

            return spec;
        }

        /// <summary>
        /// Creates a Specification from IPagingCriteriaExpression with full expression support.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="criteria">The paging criteria expression</param>
        /// <param name="filter">Optional filter expression</param>
        /// <returns>A Specification with the criteria's settings applied</returns>
        public static Specification<T> ToSpecification<T>(
            this IPagingCriteriaExpression<T> criteria,
            Expression<Func<T, bool>> filter = null)
            where T : class
        {
            var spec = new PagingExpressionSpecification<T>(
                filter ?? (x => true),
                criteria
            );

            return spec;
        }

        #endregion
    }

    /// <summary>
    /// Internal specification that wraps IPagingCriteria settings.
    /// </summary>
    internal sealed class PagingSpecification<T> : Specification<T>
        where T : class
    {
        private readonly Expression<Func<T, bool>> _criteria;

        public PagingSpecification(Expression<Func<T, bool>> criteria, IPagingCriteria pagingCriteria)
        {
            _criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));

            // Apply paging
            var limit = pagingCriteria.Limit > 0 ? pagingCriteria.Limit : 100;
            ApplyPaging(pagingCriteria.Offset * limit, limit);

            // Apply string-based navigation
            if (pagingCriteria.Navigation != null)
            {
                foreach (var nav in pagingCriteria.Navigation)
                {
                    AddInclude(nav);
                }
            }
        }

        protected override Expression<Func<T, bool>> Criteria => _criteria;
    }

    /// <summary>
    /// Internal specification that wraps IPagingCriteriaExpression settings with full expression support.
    /// </summary>
    internal sealed class PagingExpressionSpecification<T> : Specification<T>
        where T : class
    {
        private readonly Expression<Func<T, bool>> _criteria;

        public PagingExpressionSpecification(
            Expression<Func<T, bool>> criteria,
            IPagingCriteriaExpression<T> pagingCriteria)
        {
            _criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));

            // Apply paging
            var limit = pagingCriteria.Limit > 0 ? pagingCriteria.Limit : 100;
            ApplyPaging(pagingCriteria.Offset * limit, limit);

            // Apply expression-based ordering (ascending)
            if (pagingCriteria.OrderByAscendingExpr != null)
            {
                foreach (var order in pagingCriteria.OrderByAscendingExpr)
                {
                    AddOrderBy(order);
                }
            }

            // Apply expression-based ordering (descending)
            if (pagingCriteria.OrderByDescendingExpr != null)
            {
                foreach (var order in pagingCriteria.OrderByDescendingExpr)
                {
                    AddOrderByDescending(order);
                }
            }

            // Apply expression-based navigation
            if (pagingCriteria.NavigationExpr != null)
            {
                foreach (var nav in pagingCriteria.NavigationExpr)
                {
                    AddInclude(nav);
                }
            }

            // Apply string-based navigation
            if (pagingCriteria.Navigation != null)
            {
                foreach (var nav in pagingCriteria.Navigation)
                {
                    AddInclude(nav);
                }
            }
        }

        protected override Expression<Func<T, bool>> Criteria => _criteria;
    }
}

