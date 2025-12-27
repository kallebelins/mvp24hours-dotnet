//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TextSearch
{
    /// <summary>
    /// Service for MongoDB full-text search operations.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <example>
    /// <code>
    /// // Create a text index
    /// await textSearchService.CreateTextIndexAsync(new[] { "title", "description" });
    /// 
    /// // Create weighted text index
    /// await textSearchService.CreateTextIndexAsync(new Dictionary&lt;string, int&gt;
    /// {
    ///     { "title", 10 },      // Title matches are 10x more important
    ///     { "description", 5 }, // Description matches are 5x more important
    ///     { "tags", 2 }         // Tag matches are 2x more important
    /// });
    /// 
    /// // Search for documents
    /// var results = await textSearchService.SearchAsync("mongodb tutorial");
    /// foreach (var result in results)
    /// {
    ///     Console.WriteLine($"Score: {result.Score}, Title: {result.Document.Title}");
    /// }
    /// 
    /// // Search with exclusions
    /// var results = await textSearchService.SearchWithExclusionsAsync(
    ///     includeTerms: new[] { "mongodb", "tutorial" },
    ///     excludeTerms: new[] { "beginner" });
    /// </code>
    /// </example>
    public class MongoDbTextSearchService<TDocument> : IMongoDbTextSearchService<TDocument>
    {
        private readonly IMongoCollection<TDocument> _collection;
        private readonly ILogger<MongoDbTextSearchService<TDocument>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbTextSearchService{TDocument}"/> class.
        /// </summary>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbTextSearchService(
            IMongoCollection<TDocument> collection,
            ILogger<MongoDbTextSearchService<TDocument>> logger = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string> CreateTextIndexAsync(
            IEnumerable<string> fields,
            CreateIndexOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (fields == null || !fields.Any())
            {
                throw new ArgumentException("At least one field must be specified.", nameof(fields));
            }

            var indexKeys = new BsonDocument();
            foreach (var field in fields)
            {
                indexKeys.Add(field, "text");
            }

            var indexModel = new CreateIndexModel<TDocument>(
                new BsonDocumentIndexKeysDefinition<TDocument>(indexKeys),
                options ?? new CreateIndexOptions());

            var indexName = await _collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);

            _logger?.LogInformation("Text index '{IndexName}' created on fields: {Fields}", indexName, string.Join(", ", fields));

            return indexName;
        }

        /// <inheritdoc/>
        public async Task<string> CreateTextIndexAsync(
            IDictionary<string, int> fieldWeights,
            CreateIndexOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (fieldWeights == null || !fieldWeights.Any())
            {
                throw new ArgumentException("At least one field must be specified.", nameof(fieldWeights));
            }

            var indexKeys = new BsonDocument();
            var weights = new BsonDocument();

            foreach (var kvp in fieldWeights)
            {
                indexKeys.Add(kvp.Key, "text");
                weights.Add(kvp.Key, kvp.Value);
            }

            options ??= new CreateIndexOptions();
            options.Weights = weights;

            var indexModel = new CreateIndexModel<TDocument>(
                new BsonDocumentIndexKeysDefinition<TDocument>(indexKeys),
                options);

            var indexName = await _collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);

            _logger?.LogInformation("Weighted text index '{IndexName}' created.", indexName);

            return indexName;
        }

        /// <inheritdoc/>
        public async Task<string> CreateWildcardTextIndexAsync(
            CreateIndexOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var indexKeys = new BsonDocument("$**", "text");

            var indexModel = new CreateIndexModel<TDocument>(
                new BsonDocumentIndexKeysDefinition<TDocument>(indexKeys),
                options ?? new CreateIndexOptions());

            var indexName = await _collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);

            _logger?.LogInformation("Wildcard text index '{IndexName}' created.", indexName);

            return indexName;
        }

        /// <inheritdoc/>
        public async Task<IList<TextSearchResult<TDocument>>> SearchAsync(
            string searchText,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            return await SearchAsync(searchText, FilterDefinition<TDocument>.Empty, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<TextSearchResult<TDocument>>> SearchAsync(
            string searchText,
            FilterDefinition<TDocument> filter,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException("Search text cannot be empty.", nameof(searchText));
            }

            options ??= new MongoDbTextSearchOptions();

            var textSearch = CreateTextSearchFilter(searchText, options);
            var combinedFilter = filter != null && filter != FilterDefinition<TDocument>.Empty
                ? Builders<TDocument>.Filter.And(textSearch, filter)
                : textSearch;

            var findFluent = _collection.Find(combinedFilter);

            if (options.IncludeScore)
            {
                var projection = Builders<TDocument>.Projection
                    .MetaTextScore("score")
                    .Include(d => d);

                var pipeline = _collection.Aggregate()
                    .Match(combinedFilter)
                    .Project(new BsonDocument
                    {
                        { "document", "$$ROOT" },
                        { "score", new BsonDocument("$meta", "textScore") }
                    });

                if (options.MinScore.HasValue)
                {
                    pipeline = pipeline.Match(new BsonDocument("score", new BsonDocument("$gte", options.MinScore.Value)));
                }

                if (options.SortByScore)
                {
                    pipeline = pipeline.Sort(new BsonDocument("score", -1));
                }

                if (options.Skip.HasValue)
                {
                    pipeline = pipeline.Skip(options.Skip.Value);
                }

                if (options.Limit.HasValue)
                {
                    pipeline = pipeline.Limit(options.Limit.Value);
                }

                var results = await pipeline.ToListAsync(cancellationToken);

                return results.Select(r => new TextSearchResult<TDocument>
                {
                    Document = BsonSerializer.Deserialize<TDocument>(r["document"].AsBsonDocument),
                    Score = r["score"].AsDouble
                }).ToList();
            }
            else
            {
                if (options.Skip.HasValue)
                {
                    findFluent = findFluent.Skip(options.Skip.Value);
                }

                if (options.Limit.HasValue)
                {
                    findFluent = findFluent.Limit(options.Limit.Value);
                }

                var documents = await findFluent.ToListAsync(cancellationToken);

                return documents.Select(d => new TextSearchResult<TDocument>
                {
                    Document = d,
                    Score = 0
                }).ToList();
            }
        }

        /// <inheritdoc/>
        public async Task<IList<TextSearchResult<TDocument>>> SearchAsync(
            string searchText,
            Expression<Func<TDocument, bool>> filter,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            var filterDefinition = Builders<TDocument>.Filter.Where(filter);
            return await SearchAsync(searchText, filterDefinition, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<TextSearchResult<TDocument>>> SearchPhraseAsync(
            string phrase,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phrase))
            {
                throw new ArgumentException("Phrase cannot be empty.", nameof(phrase));
            }

            // Wrap in quotes for exact phrase match
            var phraseSearch = $"\"{phrase}\"";
            return await SearchAsync(phraseSearch, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<TextSearchResult<TDocument>>> SearchWithExclusionsAsync(
            IEnumerable<string> includeTerms,
            IEnumerable<string> excludeTerms,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (includeTerms == null || !includeTerms.Any())
            {
                throw new ArgumentException("At least one include term must be specified.", nameof(includeTerms));
            }

            var searchParts = includeTerms.ToList();

            if (excludeTerms != null)
            {
                foreach (var term in excludeTerms)
                {
                    searchParts.Add($"-{term}");
                }
            }

            var searchText = string.Join(" ", searchParts);
            return await SearchAsync(searchText, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<long> CountAsync(
            string searchText,
            MongoDbTextSearchOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                throw new ArgumentException("Search text cannot be empty.", nameof(searchText));
            }

            options ??= new MongoDbTextSearchOptions();
            var textSearch = CreateTextSearchFilter(searchText, options);

            return await _collection.CountDocumentsAsync(textSearch, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<BsonDocument>> GetTextIndexesAsync(CancellationToken cancellationToken = default)
        {
            var indexes = new List<BsonDocument>();

            using var cursor = await _collection.Indexes.ListAsync(cancellationToken);

            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (var index in cursor.Current)
                {
                    var key = index["key"].AsBsonDocument;
                    if (key.Elements.Any(e => e.Value.AsString == "text"))
                    {
                        indexes.Add(index);
                    }
                }
            }

            return indexes;
        }

        /// <inheritdoc/>
        public async Task DropTextIndexesAsync(CancellationToken cancellationToken = default)
        {
            var textIndexes = await GetTextIndexesAsync(cancellationToken);

            foreach (var index in textIndexes)
            {
                var indexName = index["name"].AsString;
                await _collection.Indexes.DropOneAsync(indexName, cancellationToken);

                _logger?.LogInformation("Text index '{IndexName}' dropped.", indexName);
            }

        }

        private FilterDefinition<TDocument> CreateTextSearchFilter(string searchText, MongoDbTextSearchOptions options)
        {
            var textSearchOptions = new TextSearchOptions
            {
                CaseSensitive = options.CaseSensitive,
                DiacriticSensitive = options.DiacriticSensitive
            };

            if (!string.IsNullOrEmpty(options.Language))
            {
                textSearchOptions.Language = options.Language;
            }

            return Builders<TDocument>.Filter.Text(searchText, textSearchOptions);
        }
    }

    /// <summary>
    /// Helper class for BSON deserialization.
    /// </summary>
    internal static class BsonSerializer
    {
        public static T Deserialize<T>(BsonDocument document)
        {
            return MongoDB.Bson.Serialization.BsonSerializer.Deserialize<T>(document);
        }
    }
}

