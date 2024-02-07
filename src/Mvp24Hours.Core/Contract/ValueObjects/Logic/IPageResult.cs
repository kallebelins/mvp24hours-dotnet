//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Core.Contract.ValueObjects.Logic
{
    /// <summary>
    /// Represents pagination results
    /// </summary>
    public interface IPageResult
    {
        /// <summary>
        /// Limit items on the page
        /// </summary>
        int Limit { get; }
        /// <summary>
        /// Page number or item block
        /// </summary>
        int Offset { get; }
        /// <summary>
        /// Quantity of items on the current page
        /// </summary>
        int Count { get; }
    }
}
