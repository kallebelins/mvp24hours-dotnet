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
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TimeSeries
{
    /// <summary>
    /// Service for MongoDB Time Series collection operations.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <example>
    /// <code>
    /// // Create a time series collection
    /// await timeSeriesService.CreateTimeSeriesCollectionAsync("sensor_readings", new TimeSeriesOptions
    /// {
    ///     TimeField = "timestamp",
    ///     MetaField = "sensorId",
    ///     Granularity = TimeSeriesGranularity.Seconds,
    ///     ExpireAfter = TimeSpan.FromDays(30) // Auto-delete after 30 days
    /// });
    /// 
    /// // Insert measurements
    /// await timeSeriesService.InsertMeasurementAsync(new SensorReading
    /// {
    ///     Timestamp = DateTime.UtcNow,
    ///     SensorId = "sensor-001",
    ///     Temperature = 23.5,
    ///     Humidity = 45.2
    /// });
    /// 
    /// // Query by time range
    /// var readings = await timeSeriesService.QueryByTimeRangeAsync(
    ///     DateTime.UtcNow.AddHours(-1),
    ///     DateTime.UtcNow);
    /// 
    /// // Aggregate by 5-minute windows
    /// var hourlyAvg = await timeSeriesService.AggregateByTimeWindowAsync(
    ///     DateTime.UtcNow.AddHours(-24),
    ///     DateTime.UtcNow,
    ///     TimeSpan.FromMinutes(5),
    ///     "temperature",
    ///     TimeSeriesAggregationType.Average);
    /// </code>
    /// </example>
    public class MongoDbTimeSeriesService<TDocument> : IMongoDbTimeSeriesService<TDocument>
    {
        private readonly IMongoDatabase _database;
        private readonly string _collectionName;
        private readonly string _timeField;
        private readonly string _metaField;
        private readonly ILogger<MongoDbTimeSeriesService<TDocument>> _logger;
        private IMongoCollection<TDocument> _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbTimeSeriesService{TDocument}"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="timeField">The time field name.</param>
        /// <param name="metaField">The metadata field name (optional).</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbTimeSeriesService(
            IMongoDatabase database,
            string collectionName,
            string timeField,
            string metaField = null,
            ILogger<MongoDbTimeSeriesService<TDocument>> logger = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _collectionName = collectionName ?? throw new ArgumentNullException(nameof(collectionName));
            _timeField = timeField ?? throw new ArgumentNullException(nameof(timeField));
            _metaField = metaField;
            _logger = logger;
            _collection = database.GetCollection<TDocument>(collectionName);
        }

        /// <inheritdoc/>
        public IMongoCollection<TDocument> Collection => _collection;

        /// <inheritdoc/>
        public async Task CreateTimeSeriesCollectionAsync(
            string collectionName,
            TimeSeriesOptions options,
            CancellationToken cancellationToken = default)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (string.IsNullOrWhiteSpace(options.TimeField))
            {
                throw new ArgumentException("TimeField is required for time series collections.", nameof(options));
            }

            var granularity = ConvertGranularity(options.Granularity);
            
            var timeSeriesOptions = !string.IsNullOrWhiteSpace(options.MetaField)
                ? new MongoDB.Driver.TimeSeriesOptions(options.TimeField, options.MetaField, granularity)
                : new MongoDB.Driver.TimeSeriesOptions(options.TimeField);

            var createCollectionOptions = new CreateCollectionOptions
            {
                TimeSeriesOptions = timeSeriesOptions
            };

            if (options.ExpireAfter.HasValue)
            {
                createCollectionOptions.ExpireAfter = options.ExpireAfter;
            }

            await _database.CreateCollectionAsync(collectionName, createCollectionOptions, cancellationToken);

            _collection = _database.GetCollection<TDocument>(collectionName);

            _logger?.LogInformation("Time series collection '{CollectionName}' created with time field '{TimeField}'.",
                collectionName, options.TimeField);
        }

        /// <inheritdoc/>
        public async Task InsertMeasurementAsync(
            TDocument document,
            CancellationToken cancellationToken = default)
        {
            await _collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task InsertMeasurementsAsync(
            IEnumerable<TDocument> documents,
            CancellationToken cancellationToken = default)
        {
            await _collection.InsertManyAsync(documents, cancellationToken: cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> QueryByTimeRangeAsync(
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken = default)
        {
            return await QueryByTimeRangeAsync(start, end, FilterDefinition<TDocument>.Empty, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> QueryByTimeRangeAsync(
            DateTime start,
            DateTime end,
            FilterDefinition<TDocument> filter,
            CancellationToken cancellationToken = default)
        {
            var timeFilter = Builders<TDocument>.Filter.And(
                Builders<TDocument>.Filter.Gte(_timeField, start),
                Builders<TDocument>.Filter.Lt(_timeField, end));

            var combinedFilter = filter != null && filter != FilterDefinition<TDocument>.Empty
                ? Builders<TDocument>.Filter.And(timeFilter, filter)
                : timeFilter;

            var result = await _collection
                .Find(combinedFilter)
                .Sort(new BsonDocumentSortDefinition<TDocument>(new BsonDocument(_timeField, 1)))
                .ToListAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IList<TimeWindowAggregation>> AggregateByTimeWindowAsync(
            DateTime start,
            DateTime end,
            TimeSpan windowSize,
            string aggregationField,
            TimeSeriesAggregationType aggregationType,
            CancellationToken cancellationToken = default)
        {
            var windowSizeMs = (long)windowSize.TotalMilliseconds;
            var aggregationOperator = GetAggregationOperator(aggregationType, aggregationField);

            var pipeline = new BsonDocument[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { _timeField, new BsonDocument
                        {
                            { "$gte", start },
                            { "$lt", end }
                        }
                    }
                }),
                new BsonDocument("$group", new BsonDocument
                {
                    { "_id", new BsonDocument("$dateTrunc", new BsonDocument
                        {
                            { "date", "$" + _timeField },
                            { "unit", "millisecond" },
                            { "binSize", windowSizeMs }
                        })
                    },
                    { "value", aggregationOperator },
                    { "count", new BsonDocument("$sum", 1) }
                }),
                new BsonDocument("$sort", new BsonDocument("_id", 1)),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "windowStart", "$_id" },
                    { "windowEnd", new BsonDocument("$dateAdd", new BsonDocument
                        {
                            { "startDate", "$_id" },
                            { "unit", "millisecond" },
                            { "amount", windowSizeMs }
                        })
                    },
                    { "value", 1 },
                    { "count", 1 }
                })
            };

            var results = await _collection.Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken)
                .ToListAsync(cancellationToken);

            var aggregations = new List<TimeWindowAggregation>();
            foreach (var result in results)
            {
                aggregations.Add(new TimeWindowAggregation
                {
                    WindowStart = result["windowStart"].ToUniversalTime(),
                    WindowEnd = result["windowEnd"].ToUniversalTime(),
                    Value = result["value"].ToDouble(),
                    Count = result["count"].ToInt64()
                });
            }

            return aggregations;
        }

        /// <inheritdoc/>
        public async Task<TDocument> GetLatestMeasurementAsync(
            CancellationToken cancellationToken = default)
        {
            return await _collection
                .Find(FilterDefinition<TDocument>.Empty)
                .Sort(new BsonDocumentSortDefinition<TDocument>(new BsonDocument(_timeField, -1)))
                .Limit(1)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<TDocument> GetLatestMeasurementByMetaAsync(
            object metaFieldValue,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_metaField))
            {
                throw new InvalidOperationException("MetaField is not configured for this time series service.");
            }

            var filter = Builders<TDocument>.Filter.Eq(_metaField, metaFieldValue);

            return await _collection
                .Find(filter)
                .Sort(new BsonDocumentSortDefinition<TDocument>(new BsonDocument(_timeField, -1)))
                .Limit(1)
                .FirstOrDefaultAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<long> DeleteMeasurementsOlderThanAsync(
            DateTime olderThan,
            CancellationToken cancellationToken = default)
        {
            var filter = Builders<TDocument>.Filter.Lt(_timeField, olderThan);
            var result = await _collection.DeleteManyAsync(filter, cancellationToken);

            _logger?.LogInformation("Deleted {Count} measurements older than {OlderThan}.",
                result.DeletedCount, olderThan);

            return result.DeletedCount;
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> GetCollectionStatsAsync(
            CancellationToken cancellationToken = default)
        {
            var command = new BsonDocument("collStats", _collectionName);
            return await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);
        }

        private static MongoDB.Driver.TimeSeriesGranularity? ConvertGranularity(string granularity)
        {
            return granularity?.ToLower() switch
            {
                "seconds" => MongoDB.Driver.TimeSeriesGranularity.Seconds,
                "minutes" => MongoDB.Driver.TimeSeriesGranularity.Minutes,
                "hours" => MongoDB.Driver.TimeSeriesGranularity.Hours,
                _ => null
            };
        }

        private static BsonDocument GetAggregationOperator(TimeSeriesAggregationType aggregationType, string field)
        {
            return aggregationType switch
            {
                TimeSeriesAggregationType.Sum => new BsonDocument("$sum", "$" + field),
                TimeSeriesAggregationType.Average => new BsonDocument("$avg", "$" + field),
                TimeSeriesAggregationType.Min => new BsonDocument("$min", "$" + field),
                TimeSeriesAggregationType.Max => new BsonDocument("$max", "$" + field),
                TimeSeriesAggregationType.Count => new BsonDocument("$sum", 1),
                TimeSeriesAggregationType.StdDev => new BsonDocument("$stdDevPop", "$" + field),
                TimeSeriesAggregationType.First => new BsonDocument("$first", "$" + field),
                TimeSeriesAggregationType.Last => new BsonDocument("$last", "$" + field),
                _ => new BsonDocument("$avg", "$" + field)
            };
        }
    }

}

