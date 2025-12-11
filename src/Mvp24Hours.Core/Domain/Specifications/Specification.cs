//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Domain.Specifications
{
    /// <summary>
    /// Base class for implementing the Specification pattern with Expression support.
    /// Combines both <see cref="ISpecificationQuery{T}"/> for queries and <see cref="ISpecificationModel{T}"/> for in-memory validation.
    /// </summary>
    /// <typeparam name="T">The entity type this specification applies to</typeparam>
    /// <remarks>
    /// This class provides a foundation for creating composable, reusable business rules.
    /// It supports:
    /// - Expression-based queries for EF Core
    /// - In-memory validation via compiled expressions
    /// - Composition using And, Or, Not operators
    /// - Include (navigation) specifications for eager loading
    /// - Ordering specifications
    /// </remarks>
    public abstract class Specification<T> : ISpecificationQueryEnhanced<T>, ISpecificationModel<T>
        where T : class
    {
        #region [ Fields ]

        private readonly List<Expression<Func<T, object>>> _includes = [];
        private readonly List<string> _includeStrings = [];
        private readonly List<(Expression<Func<T, object>> KeySelector, bool Descending)> _orderBy = [];
        private Func<T, bool> _compiledExpression;

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the criteria expression for this specification.
        /// </summary>
        protected abstract Expression<Func<T, bool>> Criteria { get; }

        /// <summary>
        /// Gets the expression that represents this specification's criteria.
        /// Used for building database queries.
        /// </summary>
        public Expression<Func<T, bool>> IsSatisfiedByExpression => Criteria;

        /// <summary>
        /// Gets the list of include expressions for eager loading related entities.
        /// </summary>
        public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes.AsReadOnly();

        /// <summary>
        /// Gets the list of include strings for eager loading related entities using string paths.
        /// </summary>
        public IReadOnlyList<string> IncludeStrings => _includeStrings.AsReadOnly();

        /// <summary>
        /// Gets the ordering specifications.
        /// Each tuple contains the key selector and a boolean indicating descending order.
        /// </summary>
        public IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderBy => _orderBy.AsReadOnly();

        /// <summary>
        /// Gets or sets the number of items to take.
        /// </summary>
        public int? Take { get; protected set; }

        /// <summary>
        /// Gets or sets the number of items to skip.
        /// </summary>
        public int? Skip { get; protected set; }

        /// <summary>
        /// Gets whether pagination is enabled for this specification.
        /// </summary>
        public bool IsPagingEnabled => Take.HasValue || Skip.HasValue;

        #endregion

        #region [ Methods - ISpecificationModel ]

        /// <summary>
        /// Checks whether the given entity satisfies this specification.
        /// Uses a compiled version of the criteria expression for performance.
        /// </summary>
        /// <param name="entity">The entity to evaluate</param>
        /// <returns>True if the entity satisfies the specification; otherwise, false</returns>
        public bool IsSatisfiedBy(T entity)
        {
            if (entity == null) return false;

            _compiledExpression ??= Criteria.Compile();
            return _compiledExpression(entity);
        }

        #endregion

        #region [ Methods - Include ]

        /// <summary>
        /// Adds an include expression for eager loading a related entity.
        /// </summary>
        /// <param name="includeExpression">The navigation property expression</param>
        /// <returns>This specification for fluent chaining</returns>
        protected Specification<T> AddInclude(Expression<Func<T, object>> includeExpression)
        {
            _includes.Add(includeExpression);
            return this;
        }

        /// <summary>
        /// Adds an include string for eager loading a related entity using a string path.
        /// Useful for multi-level includes like "Orders.OrderItems".
        /// </summary>
        /// <param name="includeString">The navigation property path</param>
        /// <returns>This specification for fluent chaining</returns>
        protected Specification<T> AddInclude(string includeString)
        {
            _includeStrings.Add(includeString);
            return this;
        }

        #endregion

        #region [ Methods - Ordering ]

        /// <summary>
        /// Adds an ascending order specification.
        /// </summary>
        /// <param name="orderByExpression">The property to order by</param>
        /// <returns>This specification for fluent chaining</returns>
        protected Specification<T> AddOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            _orderBy.Add((orderByExpression, false));
            return this;
        }

        /// <summary>
        /// Adds a descending order specification.
        /// </summary>
        /// <param name="orderByDescExpression">The property to order by descending</param>
        /// <returns>This specification for fluent chaining</returns>
        protected Specification<T> AddOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
        {
            _orderBy.Add((orderByDescExpression, true));
            return this;
        }

        #endregion

        #region [ Methods - Paging ]

        /// <summary>
        /// Applies pagination to this specification.
        /// </summary>
        /// <param name="skip">Number of items to skip</param>
        /// <param name="take">Number of items to take</param>
        /// <returns>This specification for fluent chaining</returns>
        protected Specification<T> ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            return this;
        }

        #endregion

        #region [ Static Factory Methods ]

        /// <summary>
        /// Creates a specification from an expression.
        /// </summary>
        /// <param name="expression">The criteria expression</param>
        /// <returns>A new specification based on the expression</returns>
        public static Specification<T> Create(Expression<Func<T, bool>> expression)
        {
            return new ExpressionSpecification<T>(expression);
        }

        /// <summary>
        /// Creates a specification that always returns true.
        /// </summary>
        /// <returns>A specification that matches all entities</returns>
        public static Specification<T> All()
        {
            return new ExpressionSpecification<T>(x => true);
        }

        /// <summary>
        /// Creates a specification that always returns false.
        /// </summary>
        /// <returns>A specification that matches no entities</returns>
        public static Specification<T> None()
        {
            return new ExpressionSpecification<T>(x => false);
        }

        #endregion

        #region [ Operators ]

        /// <summary>
        /// Combines two specifications using logical AND.
        /// </summary>
        public static Specification<T> operator &(Specification<T> left, Specification<T> right)
        {
            return new AndSpecification<T>(left, right);
        }

        /// <summary>
        /// Combines two specifications using logical OR.
        /// </summary>
        public static Specification<T> operator |(Specification<T> left, Specification<T> right)
        {
            return new OrSpecification<T>(left, right);
        }

        /// <summary>
        /// Negates a specification using logical NOT.
        /// </summary>
        public static Specification<T> operator !(Specification<T> specification)
        {
            return new NotSpecification<T>(specification);
        }

        #endregion
    }

    /// <summary>
    /// Internal implementation for creating specifications from expressions.
    /// </summary>
    internal sealed class ExpressionSpecification<T> : Specification<T>
        where T : class
    {
        private readonly Expression<Func<T, bool>> _expression;

        public ExpressionSpecification(Expression<Func<T, bool>> expression)
        {
            _expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        protected override Expression<Func<T, bool>> Criteria => _expression;
    }
}

