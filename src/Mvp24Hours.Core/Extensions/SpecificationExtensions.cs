//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.Domain.Specifications;
using Mvp24Hours.Core.Domain.Specifications;
using System;
using System.Linq.Expressions;

namespace Mvp24Hours.Extensions
{
    /// <summary>
    /// Extension methods for working with specifications.
    /// Provides fluent composition operators for combining specifications.
    /// </summary>
    public static class SpecificationExtensions
    {
        #region [ Composition Methods ]

        /// <summary>
        /// Combines this specification with another using logical AND.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The first specification</param>
        /// <param name="other">The specification to combine with</param>
        /// <returns>A new specification representing the logical AND of both specifications</returns>
        public static Specification<T> AndSpec<T>(this Specification<T> spec, Specification<T> other)
            where T : class
        {
            return new AndSpecification<T>(spec, other);
        }

        /// <summary>
        /// Combines this specification with an expression using logical AND.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The first specification</param>
        /// <param name="expression">The expression to combine with</param>
        /// <returns>A new specification representing the logical AND</returns>
        public static Specification<T> AndSpec<T>(this Specification<T> spec, Expression<Func<T, bool>> expression)
            where T : class
        {
            return new AndSpecification<T>(spec, Specification<T>.Create(expression));
        }

        /// <summary>
        /// Combines this specification with another using logical OR.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The first specification</param>
        /// <param name="other">The specification to combine with</param>
        /// <returns>A new specification representing the logical OR of both specifications</returns>
        public static Specification<T> OrSpec<T>(this Specification<T> spec, Specification<T> other)
            where T : class
        {
            return new OrSpecification<T>(spec, other);
        }

        /// <summary>
        /// Combines this specification with an expression using logical OR.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The first specification</param>
        /// <param name="expression">The expression to combine with</param>
        /// <returns>A new specification representing the logical OR</returns>
        public static Specification<T> OrSpec<T>(this Specification<T> spec, Expression<Func<T, bool>> expression)
            where T : class
        {
            return new OrSpecification<T>(spec, Specification<T>.Create(expression));
        }

        /// <summary>
        /// Negates this specification using logical NOT.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The specification to negate</param>
        /// <returns>A new specification representing the logical NOT</returns>
        public static Specification<T> NotSpec<T>(this Specification<T> spec)
            where T : class
        {
            return new NotSpecification<T>(spec);
        }

        #endregion

        #region [ Conversion Methods ]

        /// <summary>
        /// Converts an ISpecificationQuery to a Specification for composition support.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The specification query to convert</param>
        /// <returns>A Specification wrapping the query</returns>
        public static Specification<T> ToSpecification<T>(this ISpecificationQuery<T> spec)
            where T : class
        {
            if (spec is Specification<T> specification)
            {
                return specification;
            }

            return Specification<T>.Create(spec.IsSatisfiedByExpression);
        }

        /// <summary>
        /// Converts an expression to a Specification.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="expression">The expression to convert</param>
        /// <returns>A Specification wrapping the expression</returns>
        public static Specification<T> ToSpecification<T>(this Expression<Func<T, bool>> expression)
            where T : class
        {
            return Specification<T>.Create(expression);
        }

        #endregion

        #region [ ISpecificationQuery Composition ]

        /// <summary>
        /// Combines an ISpecificationQuery with another using logical AND.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The first specification</param>
        /// <param name="other">The specification to combine with</param>
        /// <returns>A new specification representing the logical AND</returns>
        public static Specification<T> AndSpec<T>(this ISpecificationQuery<T> spec, ISpecificationQuery<T> other)
            where T : class
        {
            return new AndSpecification<T>(spec.ToSpecification(), other.ToSpecification());
        }

        /// <summary>
        /// Combines an ISpecificationQuery with another using logical OR.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The first specification</param>
        /// <param name="other">The specification to combine with</param>
        /// <returns>A new specification representing the logical OR</returns>
        public static Specification<T> OrSpec<T>(this ISpecificationQuery<T> spec, ISpecificationQuery<T> other)
            where T : class
        {
            return new OrSpecification<T>(spec.ToSpecification(), other.ToSpecification());
        }

        /// <summary>
        /// Negates an ISpecificationQuery using logical NOT.
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="spec">The specification to negate</param>
        /// <returns>A new specification representing the logical NOT</returns>
        public static Specification<T> NotSpec<T>(this ISpecificationQuery<T> spec)
            where T : class
        {
            return new NotSpecification<T>(spec.ToSpecification());
        }

        #endregion
    }
}

