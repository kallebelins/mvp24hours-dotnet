//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Collections.Generic;

namespace Mvp24Hours.Core.Contract.ValueObjects.Logic
{
    /// <summary>
    /// Represents a definition for search criteria on a page
    /// </summary>
    public interface IPagingCriteria
    {
        /// <summary>
        /// Limit items on the page
        /// </summary>
        int Limit { get; }
        /// <summary>
        /// Item block number or page number
        /// </summary>
        int Offset { get; }
        /// <summary>
        /// Clause for sorting by field
        /// </summary>
        IReadOnlyCollection<string>? OrderBy { get; }
        /// <summary>
        /// Related objects that will be loaded together
        /// </summary>
        IReadOnlyCollection<string>? Navigation { get; }
    }
}
