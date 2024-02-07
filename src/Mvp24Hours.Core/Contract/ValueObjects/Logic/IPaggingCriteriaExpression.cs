//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Mvp24Hours.Core.Contract.ValueObjects.Logic
{
    /// <summary>
    /// Represents a definition for search criteria on a page
    /// </summary>
    public interface IPagingCriteriaExpression<T> : IPagingCriteria
    {
        /// <summary>
        /// Expression for sorting by ascending field
        /// </summary>
        IList<Expression<Func<T, dynamic>>> OrderByAscendingExpr { get; }
        /// <summary>
        /// Expression for sorting by descending field
        /// </summary>
        IList<Expression<Func<T, dynamic>>> OrderByDescendingExpr { get; }
        /// <summary>
        /// Expression for loading related objects
        /// </summary>
        IList<Expression<Func<T, dynamic>>> NavigationExpr { get; }
    }
}
