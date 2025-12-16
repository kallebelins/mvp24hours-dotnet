//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TextSearch
{
    /// <summary>
    /// Represents a text search result with score.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    public class TextSearchResult<TDocument>
    {
        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        public TDocument Document { get; set; }

        /// <summary>
        /// Gets or sets the text search score.
        /// </summary>
        /// <remarks>
        /// Higher scores indicate better matches. The score is calculated based on:
        /// <list type="bullet">
        ///   <item>Term frequency in the document</item>
        ///   <item>Inverse document frequency across the collection</item>
        ///   <item>Field weights (if specified in the index)</item>
        /// </list>
        /// </remarks>
        public double Score { get; set; }
    }
}

