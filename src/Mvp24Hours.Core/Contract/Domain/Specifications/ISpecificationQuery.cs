//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Contract.Domain.Specifications
{
    /// <summary>
    /// Specification for queries
    ///  <see cref="Mvp24Hours.Core.Contract.Domain.Specifications.ISpecification{T}"/>
    /// </summary>
    /// <typeparam name="T">Represents an entity</typeparam>
    public interface ISpecificationQuery<T> : ISpecification
    {
        /// <summary>
        /// Lambda expression that represents a filter applied to an entity or list
        /// </summary>
        Expression<Func<T, bool>> IsSatisfiedByExpression { get; }
    }
}
