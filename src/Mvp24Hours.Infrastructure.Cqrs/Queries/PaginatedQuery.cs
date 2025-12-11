//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Cqrs.Queries
{
    /// <summary>
    /// Base class for paginated queries.
    /// Provides built-in support for limit, offset, ordering, and navigation.
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <remarks>
    /// Use this as a base class for queries that return paginated results.
    /// It automatically integrates with the Mvp24Hours paging infrastructure.
    /// 
    /// <example>
    /// <code>
    /// public class GetCustomersQuery : PaginatedQuery&lt;IEnumerable&lt;CustomerDto&gt;&gt;
    /// {
    ///     public string NameFilter { get; set; }
    ///     
    ///     public GetCustomersQuery(int page, int pageSize) : base(page, pageSize)
    ///     {
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract class PaginatedQuery<TResponse> : IMediatorQuery<TResponse>, IPagingCriteria
    {
        #region [ Constructors ]

        /// <summary>
        /// Creates a new paginated query with default values.
        /// </summary>
        protected PaginatedQuery() : this(0, 20)
        {
        }

        /// <summary>
        /// Creates a new paginated query with the specified page and page size.
        /// </summary>
        /// <param name="page">The page number (0-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        protected PaginatedQuery(int page, int pageSize)
        {
            Page = Math.Max(0, page);
            PageSize = Math.Max(1, Math.Min(pageSize, MaxPageSize));
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the current page number (0-based).
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// Gets or sets the maximum page size allowed.
        /// Override this property to change the maximum limit.
        /// </summary>
        public virtual int MaxPageSize { get; } = 100;

        /// <summary>
        /// Gets or sets the ordering fields.
        /// Format: "PropertyName" for ascending, "-PropertyName" for descending.
        /// </summary>
        public IReadOnlyCollection<string> OrderBy { get; set; }

        /// <summary>
        /// Gets or sets the navigation properties to include.
        /// </summary>
        public IReadOnlyCollection<string> Navigation { get; set; }

        #endregion

        #region [ IPagingCriteria ]

        /// <summary>
        /// Gets the limit (items per page) for IPagingCriteria.
        /// </summary>
        int IPagingCriteria.Limit => PageSize;

        /// <summary>
        /// Gets the offset (page number) for IPagingCriteria.
        /// </summary>
        int IPagingCriteria.Offset => Page;

        /// <summary>
        /// Gets the order by collection for IPagingCriteria.
        /// </summary>
        IReadOnlyCollection<string> IPagingCriteria.OrderBy => OrderBy;

        /// <summary>
        /// Gets the navigation collection for IPagingCriteria.
        /// </summary>
        IReadOnlyCollection<string> IPagingCriteria.Navigation => Navigation;

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Sets the ordering for this query.
        /// </summary>
        /// <param name="orderBy">The ordering fields</param>
        /// <returns>This query for fluent chaining</returns>
        public PaginatedQuery<TResponse> WithOrderBy(params string[] orderBy)
        {
            OrderBy = orderBy;
            return this;
        }

        /// <summary>
        /// Sets the navigation properties to include.
        /// </summary>
        /// <param name="navigation">The navigation properties</param>
        /// <returns>This query for fluent chaining</returns>
        public PaginatedQuery<TResponse> WithNavigation(params string[] navigation)
        {
            Navigation = navigation;
            return this;
        }

        #endregion
    }

    /// <summary>
    /// Base class for paginated queries with strongly-typed expression support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    public abstract class PaginatedQuery<TEntity, TResponse> : PaginatedQuery<TResponse>, IPagingCriteriaExpression<TEntity>
        where TEntity : class
    {
        #region [ Fields ]

        private IList<Expression<Func<TEntity, dynamic>>> _orderByAscendingExpr;
        private IList<Expression<Func<TEntity, dynamic>>> _orderByDescendingExpr;
        private IList<Expression<Func<TEntity, dynamic>>> _navigationExpr;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new paginated query with default values.
        /// </summary>
        protected PaginatedQuery() : base()
        {
        }

        /// <summary>
        /// Creates a new paginated query with the specified page and page size.
        /// </summary>
        /// <param name="page">The page number (0-based)</param>
        /// <param name="pageSize">The number of items per page</param>
        protected PaginatedQuery(int page, int pageSize) : base(page, pageSize)
        {
        }

        #endregion

        #region [ IPagingCriteriaExpression ]

        /// <summary>
        /// Gets the ascending order expressions.
        /// </summary>
        public IList<Expression<Func<TEntity, dynamic>>> OrderByAscendingExpr
        {
            get => _orderByAscendingExpr ??= new List<Expression<Func<TEntity, dynamic>>>();
        }

        /// <summary>
        /// Gets the descending order expressions.
        /// </summary>
        public IList<Expression<Func<TEntity, dynamic>>> OrderByDescendingExpr
        {
            get => _orderByDescendingExpr ??= new List<Expression<Func<TEntity, dynamic>>>();
        }

        /// <summary>
        /// Gets the navigation expressions for eager loading.
        /// </summary>
        public IList<Expression<Func<TEntity, dynamic>>> NavigationExpr
        {
            get => _navigationExpr ??= new List<Expression<Func<TEntity, dynamic>>>();
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Adds an ascending order expression.
        /// </summary>
        /// <param name="orderByExpression">The order expression</param>
        /// <returns>This query for fluent chaining</returns>
        public PaginatedQuery<TEntity, TResponse> OrderByAsc(Expression<Func<TEntity, dynamic>> orderByExpression)
        {
            OrderByAscendingExpr.Add(orderByExpression);
            return this;
        }

        /// <summary>
        /// Adds a descending order expression.
        /// </summary>
        /// <param name="orderByExpression">The order expression</param>
        /// <returns>This query for fluent chaining</returns>
        public PaginatedQuery<TEntity, TResponse> OrderByDesc(Expression<Func<TEntity, dynamic>> orderByExpression)
        {
            OrderByDescendingExpr.Add(orderByExpression);
            return this;
        }

        /// <summary>
        /// Adds a navigation expression for eager loading.
        /// </summary>
        /// <param name="navigationExpression">The navigation expression</param>
        /// <returns>This query for fluent chaining</returns>
        public PaginatedQuery<TEntity, TResponse> Include(Expression<Func<TEntity, dynamic>> navigationExpression)
        {
            NavigationExpr.Add(navigationExpression);
            return this;
        }

        #endregion
    }
}

