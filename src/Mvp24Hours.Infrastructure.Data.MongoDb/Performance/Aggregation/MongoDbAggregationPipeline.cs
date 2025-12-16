//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Aggregation
{
    /// <summary>
    /// Fluent builder for MongoDB aggregation pipelines.
    /// </summary>
    /// <typeparam name="T">The document type.</typeparam>
    /// <remarks>
    /// <para>
    /// Provides a fluent API for building complex aggregation pipelines with type safety.
    /// Supports all standard aggregation stages: $match, $project, $group, $sort, $lookup,
    /// $unwind, $limit, $skip, $facet, and more.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var result = await MongoDbAggregationPipeline&lt;Order&gt;
    ///     .Create(collection)
    ///     .Match(o => o.Status == "Completed")
    ///     .Group(
    ///         o => o.CustomerId,
    ///         g => new { CustomerId = g.Key, TotalAmount = g.Sum(o => o.Amount) })
    ///     .Sort(x => x.TotalAmount, descending: true)
    ///     .Limit(10)
    ///     .ToListAsync();
    /// </code>
    /// </example>
    public class MongoDbAggregationPipeline<T>
    {
        private readonly IMongoCollection<T> _collection;
        private readonly List<BsonDocument> _stages = new();

        private MongoDbAggregationPipeline(IMongoCollection<T> collection)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        /// <summary>
        /// Creates a new aggregation pipeline for the specified collection.
        /// </summary>
        /// <param name="collection">The MongoDB collection.</param>
        /// <returns>A new aggregation pipeline builder.</returns>
        public static MongoDbAggregationPipeline<T> Create(IMongoCollection<T> collection)
        {
            return new MongoDbAggregationPipeline<T>(collection);
        }

        /// <summary>
        /// Adds a $match stage to filter documents.
        /// </summary>
        /// <param name="filter">The filter expression.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Match(Expression<Func<T, bool>> filter)
        {
            var filterDef = Builders<T>.Filter.Where(filter);
            return Match(filterDef);
        }

        /// <summary>
        /// Adds a $match stage with a filter definition.
        /// </summary>
        /// <param name="filter">The filter definition.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Match(FilterDefinition<T> filter)
        {
            var bsonFilter = filter.Render(
                _collection.DocumentSerializer,
                _collection.Settings.SerializerRegistry);

            _stages.Add(new BsonDocument("$match", bsonFilter));
            return this;
        }

        /// <summary>
        /// Adds a $project stage to shape output documents.
        /// </summary>
        /// <param name="projection">The projection definition.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Project(ProjectionDefinition<T> projection)
        {
            var bsonProjection = projection.Render(
                _collection.DocumentSerializer,
                _collection.Settings.SerializerRegistry);

            // The render result is already a BsonDocument
            _stages.Add(new BsonDocument("$project", bsonProjection));
            return this;
        }

        /// <summary>
        /// Adds a $project stage with a projection document.
        /// </summary>
        /// <param name="projectionDocument">The BSON projection document.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Project(BsonDocument projectionDocument)
        {
            _stages.Add(new BsonDocument("$project", projectionDocument));
            return this;
        }

        /// <summary>
        /// Adds a $group stage.
        /// </summary>
        /// <param name="idField">The field to group by.</param>
        /// <param name="groupDocument">The group operations document.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        /// <example>
        /// <code>
        /// pipeline.Group("$status", new BsonDocument
        /// {
        ///     { "count", new BsonDocument("$sum", 1) },
        ///     { "totalAmount", new BsonDocument("$sum", "$amount") }
        /// });
        /// </code>
        /// </example>
        public MongoDbAggregationPipeline<T> Group(string idField, BsonDocument groupDocument)
        {
            var groupStage = new BsonDocument("_id", idField);
            groupStage.AddRange(groupDocument);
            _stages.Add(new BsonDocument("$group", groupStage));
            return this;
        }

        /// <summary>
        /// Adds a $group stage with multiple grouping fields.
        /// </summary>
        /// <param name="idDocument">The _id specification document.</param>
        /// <param name="groupDocument">The group operations document.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Group(BsonDocument idDocument, BsonDocument groupDocument)
        {
            var groupStage = new BsonDocument("_id", idDocument);
            groupStage.AddRange(groupDocument);
            _stages.Add(new BsonDocument("$group", groupStage));
            return this;
        }

        /// <summary>
        /// Adds a $sort stage.
        /// </summary>
        /// <param name="sortDocument">The sort specification document.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Sort(BsonDocument sortDocument)
        {
            _stages.Add(new BsonDocument("$sort", sortDocument));
            return this;
        }

        /// <summary>
        /// Adds a $sort stage with a sort definition.
        /// </summary>
        /// <param name="sort">The sort definition.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Sort(SortDefinition<T> sort)
        {
            var bsonSort = sort.Render(
                _collection.DocumentSerializer,
                _collection.Settings.SerializerRegistry);

            _stages.Add(new BsonDocument("$sort", bsonSort));
            return this;
        }

        /// <summary>
        /// Adds a $sort stage for a single field.
        /// </summary>
        /// <param name="fieldName">The field name to sort by.</param>
        /// <param name="descending">Whether to sort in descending order.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Sort(string fieldName, bool descending = false)
        {
            _stages.Add(new BsonDocument("$sort", new BsonDocument(fieldName, descending ? -1 : 1)));
            return this;
        }

        /// <summary>
        /// Adds a $limit stage.
        /// </summary>
        /// <param name="limit">The maximum number of documents to return.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Limit(int limit)
        {
            _stages.Add(new BsonDocument("$limit", limit));
            return this;
        }

        /// <summary>
        /// Adds a $skip stage.
        /// </summary>
        /// <param name="skip">The number of documents to skip.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Skip(int skip)
        {
            _stages.Add(new BsonDocument("$skip", skip));
            return this;
        }

        /// <summary>
        /// Adds a $lookup stage for joining collections.
        /// </summary>
        /// <param name="fromCollection">The foreign collection name.</param>
        /// <param name="localField">The local field to match.</param>
        /// <param name="foreignField">The foreign field to match.</param>
        /// <param name="asField">The output array field name.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Lookup(
            string fromCollection,
            string localField,
            string foreignField,
            string asField)
        {
            _stages.Add(new BsonDocument("$lookup", new BsonDocument
            {
                { "from", fromCollection },
                { "localField", localField },
                { "foreignField", foreignField },
                { "as", asField }
            }));
            return this;
        }

        /// <summary>
        /// Adds a $lookup stage with pipeline.
        /// </summary>
        /// <param name="fromCollection">The foreign collection name.</param>
        /// <param name="let">The let variables document.</param>
        /// <param name="pipeline">The sub-pipeline stages.</param>
        /// <param name="asField">The output array field name.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Lookup(
            string fromCollection,
            BsonDocument let,
            BsonArray pipeline,
            string asField)
        {
            _stages.Add(new BsonDocument("$lookup", new BsonDocument
            {
                { "from", fromCollection },
                { "let", let },
                { "pipeline", pipeline },
                { "as", asField }
            }));
            return this;
        }

        /// <summary>
        /// Adds an $unwind stage to deconstruct an array field.
        /// </summary>
        /// <param name="fieldPath">The array field path (with or without $).</param>
        /// <param name="preserveNullAndEmptyArrays">Whether to preserve documents when array is null or empty.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Unwind(string fieldPath, bool preserveNullAndEmptyArrays = false)
        {
            var path = fieldPath.StartsWith("$") ? fieldPath : $"${fieldPath}";

            if (preserveNullAndEmptyArrays)
            {
                _stages.Add(new BsonDocument("$unwind", new BsonDocument
                {
                    { "path", path },
                    { "preserveNullAndEmptyArrays", true }
                }));
            }
            else
            {
                _stages.Add(new BsonDocument("$unwind", path));
            }
            return this;
        }

        /// <summary>
        /// Adds a $count stage.
        /// </summary>
        /// <param name="fieldName">The field name for the count result.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Count(string fieldName = "count")
        {
            _stages.Add(new BsonDocument("$count", fieldName));
            return this;
        }

        /// <summary>
        /// Adds a $facet stage for multi-faceted aggregations.
        /// </summary>
        /// <param name="facets">The facets as key-value pairs of name and pipeline stages.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Facet(Dictionary<string, BsonArray> facets)
        {
            var facetDocument = new BsonDocument();
            foreach (var facet in facets)
            {
                facetDocument.Add(facet.Key, facet.Value);
            }
            _stages.Add(new BsonDocument("$facet", facetDocument));
            return this;
        }

        /// <summary>
        /// Adds a $addFields stage.
        /// </summary>
        /// <param name="fields">The fields to add.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> AddFields(BsonDocument fields)
        {
            _stages.Add(new BsonDocument("$addFields", fields));
            return this;
        }

        /// <summary>
        /// Adds a $set stage (alias for $addFields).
        /// </summary>
        /// <param name="fields">The fields to set.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Set(BsonDocument fields)
        {
            _stages.Add(new BsonDocument("$set", fields));
            return this;
        }

        /// <summary>
        /// Adds an $unset stage to remove fields.
        /// </summary>
        /// <param name="fields">The field names to remove.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Unset(params string[] fields)
        {
            if (fields.Length == 1)
            {
                _stages.Add(new BsonDocument("$unset", fields[0]));
            }
            else
            {
                _stages.Add(new BsonDocument("$unset", new BsonArray(fields)));
            }
            return this;
        }

        /// <summary>
        /// Adds a $replaceRoot stage.
        /// </summary>
        /// <param name="newRootExpression">The new root expression.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> ReplaceRoot(string newRootExpression)
        {
            _stages.Add(new BsonDocument("$replaceRoot",
                new BsonDocument("newRoot", newRootExpression)));
            return this;
        }

        /// <summary>
        /// Adds a $sample stage for random sampling.
        /// </summary>
        /// <param name="size">The number of random documents to return.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> Sample(int size)
        {
            _stages.Add(new BsonDocument("$sample", new BsonDocument("size", size)));
            return this;
        }

        /// <summary>
        /// Adds a custom stage.
        /// </summary>
        /// <param name="stage">The custom stage document.</param>
        /// <returns>The pipeline builder for chaining.</returns>
        public MongoDbAggregationPipeline<T> AddStage(BsonDocument stage)
        {
            _stages.Add(stage);
            return this;
        }

        /// <summary>
        /// Builds the pipeline definition.
        /// </summary>
        /// <returns>The pipeline definition.</returns>
        public PipelineDefinition<T, BsonDocument> Build()
        {
            return PipelineDefinition<T, BsonDocument>.Create(_stages);
        }

        /// <summary>
        /// Builds the pipeline definition with a specific output type.
        /// </summary>
        /// <typeparam name="TResult">The result document type.</typeparam>
        /// <returns>The pipeline definition.</returns>
        public PipelineDefinition<T, TResult> Build<TResult>()
        {
            return PipelineDefinition<T, TResult>.Create(_stages);
        }

        /// <summary>
        /// Gets the pipeline stages as BSON documents.
        /// </summary>
        /// <returns>The list of pipeline stages.</returns>
        public IReadOnlyList<BsonDocument> GetStages()
        {
            return _stages.AsReadOnly();
        }

        /// <summary>
        /// Executes the pipeline and returns results as BSON documents.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The aggregation results.</returns>
        public async Task<List<BsonDocument>> ToListAsync(CancellationToken cancellationToken = default)
        {
            var pipeline = Build();
            var cursor = await _collection.AggregateAsync(pipeline, cancellationToken: cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Executes the pipeline and returns typed results.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The aggregation results.</returns>
        public async Task<List<TResult>> ToListAsync<TResult>(CancellationToken cancellationToken = default)
        {
            var pipeline = Build<TResult>();
            var cursor = await _collection.AggregateAsync(pipeline, cancellationToken: cancellationToken);
            return await cursor.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Executes the pipeline and returns the first result or default.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The first result or default.</returns>
        public async Task<TResult> FirstOrDefaultAsync<TResult>(CancellationToken cancellationToken = default)
        {
            var pipeline = Build<TResult>().Limit(1);
            var cursor = await _collection.AggregateAsync(pipeline, cancellationToken: cancellationToken);
            return await cursor.FirstOrDefaultAsync(cancellationToken);
        }

        /// <summary>
        /// Executes the pipeline and returns a cursor for streaming results.
        /// </summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The cursor for iterating results.</returns>
        public async Task<IAsyncCursor<TResult>> ToCursorAsync<TResult>(CancellationToken cancellationToken = default)
        {
            var pipeline = Build<TResult>();
            return await _collection.AggregateAsync(pipeline, cancellationToken: cancellationToken);
        }
    }
}

