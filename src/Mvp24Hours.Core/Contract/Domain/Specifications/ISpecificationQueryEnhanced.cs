//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Contract.Domain.Specifications
{
    /// <summary>
    /// Enhanced specification interface that supports includes, ordering, and paging.
    /// Extends <see cref="ISpecificationQuery{T}"/> with additional query capabilities.
    /// </summary>
    /// <typeparam name="T">The entity type this specification applies to</typeparam>
    public interface ISpecificationQueryEnhanced<T> : ISpecificationQuery<T>
    {
        /// <summary>
        /// Gets the list of include expressions for eager loading related entities.
        /// </summary>
        IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// Gets the list of include strings for eager loading related entities using string paths.
        /// Useful for multi-level includes like "Orders.OrderItems".
        /// </summary>
        IReadOnlyList<string> IncludeStrings { get; }

        /// <summary>
        /// Gets the ordering specifications.
        /// Each tuple contains the key selector and a boolean indicating descending order.
        /// </summary>
        IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderBy { get; }

        /// <summary>
        /// Gets the number of items to take.
        /// </summary>
        int? Take { get; }

        /// <summary>
        /// Gets the number of items to skip.
        /// </summary>
        int? Skip { get; }

        /// <summary>
        /// Gets whether pagination is enabled for this specification.
        /// </summary>
        bool IsPagingEnabled { get; }
    }
}

