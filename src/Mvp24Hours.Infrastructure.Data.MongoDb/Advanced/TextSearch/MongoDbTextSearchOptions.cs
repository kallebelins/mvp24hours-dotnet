//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TextSearch
{
    /// <summary>
    /// Options for MongoDB text search operations.
    /// </summary>
    public class MongoDbTextSearchOptions
    {
        /// <summary>
        /// Gets or sets the language for text search.
        /// Default is null (uses default language).
        /// </summary>
        /// <remarks>
        /// Common values: "english", "portuguese", "spanish", "french", "german", "none".
        /// Use "none" for language-agnostic searches.
        /// </remarks>
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets whether the search is case-sensitive.
        /// Default is false.
        /// </summary>
        /// <remarks>
        /// Case-sensitive search requires MongoDB 3.2+ with version 3 text indexes.
        /// </remarks>
        public bool CaseSensitive { get; set; }

        /// <summary>
        /// Gets or sets whether the search is diacritic-sensitive.
        /// Default is false.
        /// </summary>
        /// <remarks>
        /// Diacritic-sensitive search requires MongoDB 3.2+ with version 3 text indexes.
        /// When false, "café" and "cafe" are considered equal.
        /// </remarks>
        public bool DiacriticSensitive { get; set; }

        /// <summary>
        /// Gets or sets whether to include text score in results.
        /// Default is true.
        /// </summary>
        public bool IncludeScore { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum text score threshold.
        /// Documents with scores below this value will be excluded.
        /// </summary>
        public double? MinScore { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of results to return.
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets the number of results to skip.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Gets or sets whether to sort by text score (descending).
        /// Default is true.
        /// </summary>
        public bool SortByScore { get; set; } = true;
    }
}

