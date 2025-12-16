//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TextSearch
{
    /// <summary>
    /// Interface for MongoDB full-text search operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provides full-text search capabilities including:
    /// <list type="bullet">
    ///   <item>Text index creation and management</item>
    ///   <item>Simple and phrase text search</item>
    ///   <item>Text search with scoring</item>
    ///   <item>Compound text search with additional filters</item>
    ///   <item>Multi-language support</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMongoDbTextSearchService<TDocument>
    {
        /// <summary>
        /// Creates a text index on the specified fields.
        /// </summary>
        /// <param name="fields">The field names to include in the index.</param>
        /// <param name="options">Optional index options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The name of the created index.</returns>
        Task<string> CreateTextIndexAsync(
            IEnumerable<string> fields,
            CreateIndexOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a text index on the specified fields with weights.
        /// </summary>
        /// <param name="fieldWeights">Dictionary of field names and their weights.</param>
        /// <param name="options">Optional index options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The name of the created index.</returns>
        /// <remarks>
        /// Field weights affect the text score calculation. Higher weights give more importance
        /// to matches in those fields. Default weight is 1.
        /// </remarks>
        Task<string> CreateTextIndexAsync(
            IDictionary<string, int> fieldWeights,
            CreateIndexOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a wildcard text index on all string fields.
        /// </summary>
        /// <param name="options">Optional index options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The name of the created index.</returns>
        Task<string> CreateWildcardTextIndexAsync(
            CreateIndexOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a text search.
        /// </summary>
        /// <param name="searchText">The text to search for.</param>
        /// <param name="options">Optional search options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching documents with scores.</returns>
        Task<IList<TextSearchResult<TDocument>>> SearchAsync(
            string searchText,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a text search with an additional filter.
        /// </summary>
        /// <param name="searchText">The text to search for.</param>
        /// <param name="filter">Additional filter to apply.</param>
        /// <param name="options">Optional search options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching documents with scores.</returns>
        Task<IList<TextSearchResult<TDocument>>> SearchAsync(
            string searchText,
            FilterDefinition<TDocument> filter,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a text search with a LINQ expression filter.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="searchText">The text to search for.</param>
        /// <param name="filter">Additional filter expression.</param>
        /// <param name="options">Optional search options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching documents with scores.</returns>
        Task<IList<TextSearchResult<TDocument>>> SearchAsync(
            string searchText,
            Expression<System.Func<TDocument, bool>> filter,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a phrase search (exact phrase match).
        /// </summary>
        /// <param name="phrase">The exact phrase to search for.</param>
        /// <param name="options">Optional search options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching documents with scores.</returns>
        Task<IList<TextSearchResult<TDocument>>> SearchPhraseAsync(
            string phrase,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs a search excluding specified terms.
        /// </summary>
        /// <param name="includeTerms">Terms to search for.</param>
        /// <param name="excludeTerms">Terms to exclude from results.</param>
        /// <param name="options">Optional search options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of matching documents with scores.</returns>
        Task<IList<TextSearchResult<TDocument>>> SearchWithExclusionsAsync(
            IEnumerable<string> includeTerms,
            IEnumerable<string> excludeTerms,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts documents matching a text search.
        /// </summary>
        /// <param name="searchText">The text to search for.</param>
        /// <param name="options">Optional search options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count of matching documents.</returns>
        Task<long> CountAsync(
            string searchText,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets text index information.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of text index information.</returns>
        Task<IList<BsonDocument>> GetTextIndexesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops all text indexes on the collection.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DropTextIndexesAsync(CancellationToken cancellationToken = default);
    }
}

