//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Infrastructure.Cqrs.Queries
{
    /// <summary>
    /// Defines the sort direction.
    /// </summary>
    public enum SortDirection
    {
        /// <summary>
        /// Ascending order (A to Z, 0 to 9).
        /// </summary>
        Ascending,

        /// <summary>
        /// Descending order (Z to A, 9 to 0).
        /// </summary>
        Descending
    }

    /// <summary>
    /// Represents a single sort criterion.
    /// </summary>
    public sealed class SortCriteria
    {
        /// <summary>
        /// Gets or sets the property name to sort by.
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        public SortDirection Direction { get; set; } = SortDirection.Ascending;

        /// <summary>
        /// Creates a new ascending sort criteria.
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <returns>A new SortCriteria</returns>
        public static SortCriteria Asc(string propertyName) =>
            new() { PropertyName = propertyName, Direction = SortDirection.Ascending };

        /// <summary>
        /// Creates a new descending sort criteria.
        /// </summary>
        /// <param name="propertyName">The property name</param>
        /// <returns>A new SortCriteria</returns>
        public static SortCriteria Desc(string propertyName) =>
            new() { PropertyName = propertyName, Direction = SortDirection.Descending };
    }

    /// <summary>
    /// Represents a strongly-typed sort criterion.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    public sealed class SortCriteria<TEntity>
        where TEntity : class
    {
        /// <summary>
        /// Gets or sets the property selector expression.
        /// </summary>
        public Expression<Func<TEntity, object>> PropertySelector { get; set; }

        /// <summary>
        /// Gets or sets the sort direction.
        /// </summary>
        public SortDirection Direction { get; set; } = SortDirection.Ascending;

        /// <summary>
        /// Creates a new ascending sort criteria.
        /// </summary>
        /// <param name="propertySelector">The property selector expression</param>
        /// <returns>A new SortCriteria</returns>
        public static SortCriteria<TEntity> Asc(Expression<Func<TEntity, object>> propertySelector) =>
            new() { PropertySelector = propertySelector, Direction = SortDirection.Ascending };

        /// <summary>
        /// Creates a new descending sort criteria.
        /// </summary>
        /// <param name="propertySelector">The property selector expression</param>
        /// <returns>A new SortCriteria</returns>
        public static SortCriteria<TEntity> Desc(Expression<Func<TEntity, object>> propertySelector) =>
            new() { PropertySelector = propertySelector, Direction = SortDirection.Descending };
    }

    /// <summary>
    /// Base class for sorted queries without pagination.
    /// </summary>
    /// <typeparam name="TResponse">The response type</typeparam>
    /// <remarks>
    /// Use this as a base class for queries that need sorting but not pagination.
    /// 
    /// <example>
    /// <code>
    /// public class GetActiveCustomersQuery : SortedQuery&lt;IEnumerable&lt;CustomerDto&gt;&gt;
    /// {
    ///     public GetActiveCustomersQuery()
    ///     {
    ///         AddSort("Name", SortDirection.Ascending);
    ///         AddSort("CreatedDate", SortDirection.Descending);
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract class SortedQuery<TResponse> : IMediatorQuery<TResponse>
    {
        #region [ Fields ]

        private readonly List<SortCriteria> _sortCriteria = [];

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the sort criteria for this query.
        /// </summary>
        public IReadOnlyList<SortCriteria> SortCriteria => _sortCriteria.AsReadOnly();

        /// <summary>
        /// Gets or sets whether to include related entities (navigation properties).
        /// </summary>
        public IReadOnlyCollection<string> Includes { get; set; }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Adds a sort criterion.
        /// </summary>
        /// <param name="propertyName">The property to sort by</param>
        /// <param name="direction">The sort direction</param>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TResponse> AddSort(string propertyName, SortDirection direction = SortDirection.Ascending)
        {
            _sortCriteria.Add(new SortCriteria { PropertyName = propertyName, Direction = direction });
            return this;
        }

        /// <summary>
        /// Adds an ascending sort criterion.
        /// </summary>
        /// <param name="propertyName">The property to sort by</param>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TResponse> SortByAsc(string propertyName)
        {
            return AddSort(propertyName, SortDirection.Ascending);
        }

        /// <summary>
        /// Adds a descending sort criterion.
        /// </summary>
        /// <param name="propertyName">The property to sort by</param>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TResponse> SortByDesc(string propertyName)
        {
            return AddSort(propertyName, SortDirection.Descending);
        }

        /// <summary>
        /// Sets the includes (navigation properties) for this query.
        /// </summary>
        /// <param name="includes">The navigation property names</param>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TResponse> WithIncludes(params string[] includes)
        {
            Includes = includes;
            return this;
        }

        /// <summary>
        /// Clears all sort criteria.
        /// </summary>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TResponse> ClearSort()
        {
            _sortCriteria.Clear();
            return this;
        }

        #endregion
    }

    /// <summary>
    /// Base class for sorted queries with strongly-typed expression support.
    /// </summary>
    /// <typeparam name="TEntity">The entity type</typeparam>
    /// <typeparam name="TResponse">The response type</typeparam>
    public abstract class SortedQuery<TEntity, TResponse> : SortedQuery<TResponse>
        where TEntity : class
    {
        #region [ Fields ]

        private readonly List<SortCriteria<TEntity>> _sortExpressions = [];
        private readonly List<Expression<Func<TEntity, object>>> _includeExpressions = [];

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the strongly-typed sort expressions for this query.
        /// </summary>
        public IReadOnlyList<SortCriteria<TEntity>> SortExpressions => _sortExpressions.AsReadOnly();

        /// <summary>
        /// Gets the include expressions for eager loading.
        /// </summary>
        public IReadOnlyList<Expression<Func<TEntity, object>>> IncludeExpressions => _includeExpressions.AsReadOnly();

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Adds a strongly-typed sort expression.
        /// </summary>
        /// <param name="propertySelector">The property selector expression</param>
        /// <param name="direction">The sort direction</param>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TEntity, TResponse> AddSort(
            Expression<Func<TEntity, object>> propertySelector,
            SortDirection direction = SortDirection.Ascending)
        {
            _sortExpressions.Add(new SortCriteria<TEntity>
            {
                PropertySelector = propertySelector,
                Direction = direction
            });
            return this;
        }

        /// <summary>
        /// Adds an ascending sort expression.
        /// </summary>
        /// <param name="propertySelector">The property selector expression</param>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TEntity, TResponse> SortByAsc(Expression<Func<TEntity, object>> propertySelector)
        {
            return AddSort(propertySelector, SortDirection.Ascending);
        }

        /// <summary>
        /// Adds a descending sort expression.
        /// </summary>
        /// <param name="propertySelector">The property selector expression</param>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TEntity, TResponse> SortByDesc(Expression<Func<TEntity, object>> propertySelector)
        {
            return AddSort(propertySelector, SortDirection.Descending);
        }

        /// <summary>
        /// Adds an include expression for eager loading.
        /// </summary>
        /// <param name="includeExpression">The navigation property expression</param>
        /// <returns>This query for fluent chaining</returns>
        public SortedQuery<TEntity, TResponse> Include(Expression<Func<TEntity, object>> includeExpression)
        {
            _includeExpressions.Add(includeExpression);
            return this;
        }

        /// <summary>
        /// Clears all strongly-typed sort expressions.
        /// </summary>
        /// <returns>This query for fluent chaining</returns>
        public new SortedQuery<TEntity, TResponse> ClearSort()
        {
            _sortExpressions.Clear();
            base.ClearSort();
            return this;
        }

        #endregion
    }
}

