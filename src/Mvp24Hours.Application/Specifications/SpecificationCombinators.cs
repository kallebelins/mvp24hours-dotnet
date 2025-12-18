//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Mvp24Hours.Application.Specifications
{
    /// <summary>
    /// Helper class for combining and composing specifications.
    /// Provides static methods for creating combined specifications using And, Or, and Not operators.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class provides functional-style helpers for composing specifications,
    /// complementing the operator-based composition available on <see cref="Specification{T}"/>.
    /// </para>
    /// <para>
    /// <strong>Usage Examples:</strong>
    /// <code>
    /// // Combine multiple specifications
    /// var combined = SpecificationCombinators.And(
    ///     new ActiveCustomerSpecification(),
    ///     new PremiumCustomerSpecification()
    /// );
    /// 
    /// // Create from expressions
    /// var spec = SpecificationCombinators.FromExpression&lt;Customer&gt;(c => c.IsActive);
    /// 
    /// // Combine all specifications in a list
    /// var specs = new List&lt;Specification&lt;Customer&gt;&gt;
    /// {
    ///     new ActiveSpec(), new PremiumSpec(), new RecentSpec()
    /// };
    /// var allMustMatch = SpecificationCombinators.AndAll(specs);
    /// </code>
    /// </para>
    /// </remarks>
    public static class SpecificationCombinators
    {
        #region [ And Combinators ]

        /// <summary>
        /// Combines two specifications using logical AND.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="left">The first specification</param>
        /// <param name="right">The second specification</param>
        /// <returns>A new specification that is satisfied when both specifications are satisfied</returns>
        /// <example>
        /// <code>
        /// var combined = SpecificationCombinators.And(
        ///     new ActiveCustomerSpecification(),
        ///     new PremiumCustomerSpecification()
        /// );
        /// </code>
        /// </example>
        public static Specification<T> And<T>(Specification<T> left, Specification<T> right)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return new AndSpecification<T>(left, right);
        }

        /// <summary>
        /// Combines a specification with an expression using logical AND.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specification">The specification</param>
        /// <param name="expression">The expression to combine with</param>
        /// <returns>A new specification that is satisfied when both are satisfied</returns>
        public static Specification<T> And<T>(Specification<T> specification, Expression<Func<T, bool>> expression)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(specification);
            ArgumentNullException.ThrowIfNull(expression);

            return new AndSpecification<T>(specification, Specification<T>.Create(expression));
        }

        /// <summary>
        /// Combines all specifications in a collection using logical AND.
        /// Returns a specification that is satisfied only when ALL specifications are satisfied.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specifications">The specifications to combine</param>
        /// <returns>A combined specification, or All() if the collection is empty</returns>
        /// <example>
        /// <code>
        /// var specs = new List&lt;Specification&lt;Customer&gt;&gt;
        /// {
        ///     new ActiveSpec(), new PremiumSpec(), new RecentSpec()
        /// };
        /// var allMustMatch = SpecificationCombinators.AndAll(specs);
        /// </code>
        /// </example>
        public static Specification<T> AndAll<T>(IEnumerable<Specification<T>> specifications)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(specifications);

            var specList = specifications.ToList();

            if (specList.Count == 0)
            {
                return Specification<T>.All();
            }

            var result = specList[0];
            for (int i = 1; i < specList.Count; i++)
            {
                result = new AndSpecification<T>(result, specList[i]);
            }

            return result;
        }

        /// <summary>
        /// Combines all specifications using logical AND.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specifications">The specifications to combine</param>
        /// <returns>A combined specification</returns>
        public static Specification<T> AndAll<T>(params Specification<T>[] specifications)
            where T : class
        {
            return AndAll(specifications.AsEnumerable());
        }

        #endregion

        #region [ Or Combinators ]

        /// <summary>
        /// Combines two specifications using logical OR.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="left">The first specification</param>
        /// <param name="right">The second specification</param>
        /// <returns>A new specification that is satisfied when either specification is satisfied</returns>
        /// <example>
        /// <code>
        /// var combined = SpecificationCombinators.Or(
        ///     new PremiumCustomerSpecification(),
        ///     new LongTimeCustomerSpecification()
        /// );
        /// </code>
        /// </example>
        public static Specification<T> Or<T>(Specification<T> left, Specification<T> right)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(left);
            ArgumentNullException.ThrowIfNull(right);

            return new OrSpecification<T>(left, right);
        }

        /// <summary>
        /// Combines a specification with an expression using logical OR.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specification">The specification</param>
        /// <param name="expression">The expression to combine with</param>
        /// <returns>A new specification that is satisfied when either is satisfied</returns>
        public static Specification<T> Or<T>(Specification<T> specification, Expression<Func<T, bool>> expression)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(specification);
            ArgumentNullException.ThrowIfNull(expression);

            return new OrSpecification<T>(specification, Specification<T>.Create(expression));
        }

        /// <summary>
        /// Combines all specifications in a collection using logical OR.
        /// Returns a specification that is satisfied when ANY specification is satisfied.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specifications">The specifications to combine</param>
        /// <returns>A combined specification, or None() if the collection is empty</returns>
        /// <example>
        /// <code>
        /// var specs = new List&lt;Specification&lt;Customer&gt;&gt;
        /// {
        ///     new PremiumSpec(), new LongTimeSpec(), new HighValueSpec()
        /// };
        /// var anyMustMatch = SpecificationCombinators.OrAll(specs);
        /// </code>
        /// </example>
        public static Specification<T> OrAll<T>(IEnumerable<Specification<T>> specifications)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(specifications);

            var specList = specifications.ToList();

            if (specList.Count == 0)
            {
                return Specification<T>.None();
            }

            var result = specList[0];
            for (int i = 1; i < specList.Count; i++)
            {
                result = new OrSpecification<T>(result, specList[i]);
            }

            return result;
        }

        /// <summary>
        /// Combines all specifications using logical OR.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specifications">The specifications to combine</param>
        /// <returns>A combined specification</returns>
        public static Specification<T> OrAll<T>(params Specification<T>[] specifications)
            where T : class
        {
            return OrAll(specifications.AsEnumerable());
        }

        #endregion

        #region [ Not Combinator ]

        /// <summary>
        /// Negates a specification using logical NOT.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="specification">The specification to negate</param>
        /// <returns>A new specification that is satisfied when the original is NOT satisfied</returns>
        /// <example>
        /// <code>
        /// var notPremium = SpecificationCombinators.Not(new PremiumCustomerSpecification());
        /// </code>
        /// </example>
        public static Specification<T> Not<T>(Specification<T> specification)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(specification);

            return new NotSpecification<T>(specification);
        }

        /// <summary>
        /// Creates a negated specification from an expression.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="expression">The expression to negate</param>
        /// <returns>A specification that is satisfied when the expression is NOT satisfied</returns>
        public static Specification<T> Not<T>(Expression<Func<T, bool>> expression)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);

            return new NotSpecification<T>(Specification<T>.Create(expression));
        }

        #endregion

        #region [ Factory Methods ]

        /// <summary>
        /// Creates a specification from an expression.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="expression">The filter expression</param>
        /// <returns>A specification based on the expression</returns>
        /// <example>
        /// <code>
        /// var activeSpec = SpecificationCombinators.FromExpression&lt;Customer&gt;(c => c.IsActive);
        /// </code>
        /// </example>
        public static Specification<T> FromExpression<T>(Expression<Func<T, bool>> expression)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);

            return Specification<T>.Create(expression);
        }

        /// <summary>
        /// Creates a specification that matches all entities.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <returns>A specification that always returns true</returns>
        public static Specification<T> All<T>()
            where T : class
        {
            return Specification<T>.All();
        }

        /// <summary>
        /// Creates a specification that matches no entities.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <returns>A specification that always returns false</returns>
        public static Specification<T> None<T>()
            where T : class
        {
            return Specification<T>.None();
        }

        #endregion

        #region [ Conditional Combinators ]

        /// <summary>
        /// Conditionally applies a specification based on a predicate.
        /// If the condition is true, the specification is applied; otherwise, All() is returned.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="condition">The condition to check</param>
        /// <param name="specification">The specification to apply if condition is true</param>
        /// <returns>The specification if condition is true; All() otherwise</returns>
        /// <example>
        /// <code>
        /// var conditionalSpec = SpecificationCombinators.If(
        ///     filterByActive,
        ///     new ActiveCustomerSpecification()
        /// );
        /// </code>
        /// </example>
        public static Specification<T> If<T>(bool condition, Specification<T> specification)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(specification);

            return condition ? specification : Specification<T>.All();
        }

        /// <summary>
        /// Conditionally creates a specification based on a predicate.
        /// If the condition is true, creates a specification from the expression; otherwise, returns All().
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="condition">The condition to check</param>
        /// <param name="expression">The expression to use if condition is true</param>
        /// <returns>A specification from the expression if condition is true; All() otherwise</returns>
        /// <example>
        /// <code>
        /// var spec = SpecificationCombinators.If&lt;Customer&gt;(
        ///     !string.IsNullOrEmpty(searchTerm),
        ///     c => c.Name.Contains(searchTerm)
        /// );
        /// </code>
        /// </example>
        public static Specification<T> If<T>(bool condition, Expression<Func<T, bool>> expression)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(expression);

            return condition ? Specification<T>.Create(expression) : Specification<T>.All();
        }

        /// <summary>
        /// Conditionally applies one of two specifications based on a predicate.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="condition">The condition to check</param>
        /// <param name="ifTrue">The specification to apply if condition is true</param>
        /// <param name="ifFalse">The specification to apply if condition is false</param>
        /// <returns>ifTrue specification if condition is true; ifFalse otherwise</returns>
        /// <example>
        /// <code>
        /// var spec = SpecificationCombinators.IfElse(
        ///     includePremium,
        ///     new PremiumCustomerSpecification(),
        ///     new StandardCustomerSpecification()
        /// );
        /// </code>
        /// </example>
        public static Specification<T> IfElse<T>(
            bool condition,
            Specification<T> ifTrue,
            Specification<T> ifFalse)
            where T : class
        {
            ArgumentNullException.ThrowIfNull(ifTrue);
            ArgumentNullException.ThrowIfNull(ifFalse);

            return condition ? ifTrue : ifFalse;
        }

        #endregion
    }
}

