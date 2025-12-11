//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Domain.Specifications
{
    /// <summary>
    /// Represents a specification that combines two specifications using logical AND.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public sealed class AndSpecification<T> : Specification<T>
        where T : class
    {
        private readonly Specification<T> _left;
        private readonly Specification<T> _right;
        private Expression<Func<T, bool>> _combinedExpression;

        /// <summary>
        /// Creates a new AND specification combining two specifications.
        /// </summary>
        /// <param name="left">The left specification</param>
        /// <param name="right">The right specification</param>
        public AndSpecification(Specification<T> left, Specification<T> right)
        {
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right ?? throw new ArgumentNullException(nameof(right));
        }

        /// <summary>
        /// Gets the combined criteria expression using logical AND.
        /// </summary>
        protected override Expression<Func<T, bool>> Criteria
        {
            get
            {
                _combinedExpression ??= ExpressionCombiner.And(_left.IsSatisfiedByExpression, _right.IsSatisfiedByExpression);
                return _combinedExpression;
            }
        }
    }

    /// <summary>
    /// Represents a specification that combines two specifications using logical OR.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public sealed class OrSpecification<T> : Specification<T>
        where T : class
    {
        private readonly Specification<T> _left;
        private readonly Specification<T> _right;
        private Expression<Func<T, bool>> _combinedExpression;

        /// <summary>
        /// Creates a new OR specification combining two specifications.
        /// </summary>
        /// <param name="left">The left specification</param>
        /// <param name="right">The right specification</param>
        public OrSpecification(Specification<T> left, Specification<T> right)
        {
            _left = left ?? throw new ArgumentNullException(nameof(left));
            _right = right ?? throw new ArgumentNullException(nameof(right));
        }

        /// <summary>
        /// Gets the combined criteria expression using logical OR.
        /// </summary>
        protected override Expression<Func<T, bool>> Criteria
        {
            get
            {
                _combinedExpression ??= ExpressionCombiner.Or(_left.IsSatisfiedByExpression, _right.IsSatisfiedByExpression);
                return _combinedExpression;
            }
        }
    }

    /// <summary>
    /// Represents a specification that negates another specification using logical NOT.
    /// </summary>
    /// <typeparam name="T">The entity type</typeparam>
    public sealed class NotSpecification<T> : Specification<T>
        where T : class
    {
        private readonly Specification<T> _specification;
        private Expression<Func<T, bool>> _negatedExpression;

        /// <summary>
        /// Creates a new NOT specification negating the given specification.
        /// </summary>
        /// <param name="specification">The specification to negate</param>
        public NotSpecification(Specification<T> specification)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
        }

        /// <summary>
        /// Gets the negated criteria expression.
        /// </summary>
        protected override Expression<Func<T, bool>> Criteria
        {
            get
            {
                _negatedExpression ??= ExpressionCombiner.Not(_specification.IsSatisfiedByExpression);
                return _negatedExpression;
            }
        }
    }

    /// <summary>
    /// Internal helper class for combining expressions.
    /// </summary>
    internal static class ExpressionCombiner
    {
        /// <summary>
        /// Combines two expressions using logical AND.
        /// </summary>
        public static Expression<Func<T, bool>> And<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Compose(first, second, Expression.AndAlso);
        }

        /// <summary>
        /// Combines two expressions using logical OR.
        /// </summary>
        public static Expression<Func<T, bool>> Or<T>(Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return Compose(first, second, Expression.OrElse);
        }

        /// <summary>
        /// Negates an expression.
        /// </summary>
        public static Expression<Func<T, bool>> Not<T>(Expression<Func<T, bool>> expression)
        {
            var negated = Expression.Not(expression.Body);
            return Expression.Lambda<Func<T, bool>>(negated, expression.Parameters);
        }

        /// <summary>
        /// Composes two expressions using the specified merge function.
        /// </summary>
        private static Expression<TDelegate> Compose<TDelegate>(
            Expression<TDelegate> first,
            Expression<TDelegate> second,
            Func<Expression, Expression, Expression> merge)
        {
            // Build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters
                .Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);

            // Replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);

            // Create a merged lambda expression with parameters from the first expression
            return Expression.Lambda<TDelegate>(merge(first.Body, secondBody), first.Parameters);
        }

        /// <summary>
        /// Helper class for replacing parameters in expressions.
        /// </summary>
        private sealed class ParameterRebinder : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

            private ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                _map = map ?? [];
            }

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                if (_map.TryGetValue(node, out ParameterExpression replacement))
                {
                    node = replacement;
                }

                return base.VisitParameter(node);
            }
        }
    }
}

