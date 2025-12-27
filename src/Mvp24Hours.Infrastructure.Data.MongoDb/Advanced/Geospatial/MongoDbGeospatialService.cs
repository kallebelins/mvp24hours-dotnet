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

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Geospatial
{
    /// <summary>
    /// Service for MongoDB Geospatial operations.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <example>
    /// <code>
    /// // Create a 2dsphere index
    /// await geoService.Create2dSphereIndexAsync("location");
    /// 
    /// // Find places near a point
    /// var nearbyPlaces = await geoService.FindNearAsync(
    ///     "location",
    ///     new GeoPoint(-46.6333, -23.5505), // São Paulo
    ///     maxDistanceMeters: 5000); // 5km radius
    /// 
    /// // Find places within a polygon
    /// var polygon = GeoPolygon.FromPoints(
    ///     new GeoPoint(-46.70, -23.60),
    ///     new GeoPoint(-46.60, -23.60),
    ///     new GeoPoint(-46.60, -23.50),
    ///     new GeoPoint(-46.70, -23.50),
    ///     new GeoPoint(-46.70, -23.60) // Close the polygon
    /// );
    /// var withinPolygon = await geoService.FindWithinPolygonAsync("location", polygon);
    /// 
    /// // Find with distance information
    /// var results = await geoService.FindNearWithDistanceAsync(
    ///     "location",
    ///     new GeoPoint(-46.6333, -23.5505),
    ///     maxDistanceMeters: 10000);
    /// 
    /// foreach (var result in results)
    /// {
    ///     Console.WriteLine($"Distance: {result.DistanceMeters:N0}m");
    /// }
    /// </code>
    /// </example>
    public class MongoDbGeospatialService<TDocument> : IMongoDbGeospatialService<TDocument>
    {
        private readonly IMongoCollection<TDocument> _collection;
        private readonly ILogger<MongoDbGeospatialService<TDocument>> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbGeospatialService{TDocument}"/> class.
        /// </summary>
        /// <param name="collection">The MongoDB collection.</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbGeospatialService(
            IMongoCollection<TDocument> collection,
            ILogger<MongoDbGeospatialService<TDocument>> logger = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string> Create2dSphereIndexAsync(
            string locationField,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(locationField))
            {
                throw new ArgumentException("Location field is required.", nameof(locationField));
            }

            var indexKeys = Builders<TDocument>.IndexKeys.Geo2DSphere(locationField);
            var indexName = await _collection.Indexes.CreateOneAsync(
                new CreateIndexModel<TDocument>(indexKeys),
                cancellationToken: cancellationToken);

            _logger?.LogInformation("2dsphere index '{IndexName}' created on field '{Field}'.",
                indexName, locationField);

            return indexName;
        }

        /// <inheritdoc/>
        public async Task<string> Create2dSphereIndexAsync(
            string locationField,
            IDictionary<string, int> additionalFields,
            CancellationToken cancellationToken = default)
        {
            var indexKeysDocument = new BsonDocument { { locationField, "2dsphere" } };

            foreach (var field in additionalFields)
            {
                indexKeysDocument.Add(field.Key, field.Value);
            }

            var indexModel = new CreateIndexModel<TDocument>(
                new BsonDocumentIndexKeysDefinition<TDocument>(indexKeysDocument));

            var indexName = await _collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);

            _logger?.LogInformation("Compound 2dsphere index '{IndexName}' created.", indexName);

            return indexName;
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> FindNearAsync(
            string locationField,
            GeoPoint point,
            double maxDistanceMeters,
            double? minDistanceMeters = null,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            return await FindNearAsync(locationField, point, maxDistanceMeters,
                FilterDefinition<TDocument>.Empty, limit, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> FindNearAsync(
            string locationField,
            GeoPoint point,
            double maxDistanceMeters,
            FilterDefinition<TDocument> filter,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            ValidateLocation(point);

            var nearFilter = Builders<TDocument>.Filter.NearSphere(
                locationField,
                point.Longitude,
                point.Latitude,
                maxDistanceMeters);

            var combinedFilter = filter != null && filter != FilterDefinition<TDocument>.Empty
                ? Builders<TDocument>.Filter.And(nearFilter, filter)
                : nearFilter;

            var findFluent = _collection.Find(combinedFilter);

            if (limit.HasValue)
            {
                findFluent = findFluent.Limit(limit.Value);
            }

            var result = await findFluent.ToListAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IList<GeoNearResult<TDocument>>> FindNearWithDistanceAsync(
            string locationField,
            GeoPoint point,
            double maxDistanceMeters,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            ValidateLocation(point);

            var geoNearStage = new BsonDocument("$geoNear", new BsonDocument
            {
                { "near", point.ToBsonDocument() },
                { "distanceField", "distance" },
                { "maxDistance", maxDistanceMeters },
                { "spherical", true }
            });

            var pipeline = new List<BsonDocument> { geoNearStage };

            if (limit.HasValue)
            {
                pipeline.Add(new BsonDocument("$limit", limit.Value));
            }

            var results = await _collection.Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken)
                .ToListAsync(cancellationToken);

            var geoNearResults = new List<GeoNearResult<TDocument>>();

            foreach (var result in results)
            {
                var distance = result["distance"].AsDouble;
                result.Remove("distance");

                geoNearResults.Add(new GeoNearResult<TDocument>
                {
                    Document = MongoDB.Bson.Serialization.BsonSerializer.Deserialize<TDocument>(result),
                    DistanceMeters = distance
                });
            }

            return geoNearResults;
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> FindWithinPolygonAsync(
            string locationField,
            GeoPolygon polygon,
            CancellationToken cancellationToken = default)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon));
            }

            var filter = new BsonDocumentFilterDefinition<TDocument>(
                new BsonDocument(locationField, new BsonDocument("$geoWithin",
                    new BsonDocument("$geometry", polygon.ToBsonDocument()))));

            var result = await _collection.Find(filter).ToListAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> FindWithinCircleAsync(
            string locationField,
            GeoPoint center,
            double radiusMeters,
            CancellationToken cancellationToken = default)
        {
            ValidateLocation(center);

            // Convert meters to radians (Earth's radius ≈ 6371000 meters)
            const double EarthRadiusMeters = 6371000;
            var radiusRadians = radiusMeters / EarthRadiusMeters;

            var filter = Builders<TDocument>.Filter.GeoWithinCenterSphere(
                locationField,
                center.Longitude,
                center.Latitude,
                radiusRadians);

            var result = await _collection.Find(filter).ToListAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> FindWithinBoxAsync(
            string locationField,
            GeoPoint southWest,
            GeoPoint northEast,
            CancellationToken cancellationToken = default)
        {
            ValidateLocation(southWest);
            ValidateLocation(northEast);

            var filter = Builders<TDocument>.Filter.GeoWithinBox(
                locationField,
                southWest.Longitude,
                southWest.Latitude,
                northEast.Longitude,
                northEast.Latitude);

            var result = await _collection.Find(filter).ToListAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<IList<TDocument>> FindIntersectsAsync(
            string locationField,
            GeoPolygon polygon,
            CancellationToken cancellationToken = default)
        {
            if (polygon == null)
            {
                throw new ArgumentNullException(nameof(polygon));
            }

            var filter = new BsonDocumentFilterDefinition<TDocument>(
                new BsonDocument(locationField, new BsonDocument("$geoIntersects",
                    new BsonDocument("$geometry", polygon.ToBsonDocument()))));

            var result = await _collection.Find(filter).ToListAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc/>
        public async Task<long> CountWithinRadiusAsync(
            string locationField,
            GeoPoint center,
            double radiusMeters,
            CancellationToken cancellationToken = default)
        {
            ValidateLocation(center);

            const double EarthRadiusMeters = 6371000;
            var radiusRadians = radiusMeters / EarthRadiusMeters;

            var filter = Builders<TDocument>.Filter.GeoWithinCenterSphere(
                locationField,
                center.Longitude,
                center.Latitude,
                radiusRadians);

            return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        }

        private static void ValidateLocation(GeoPoint point)
        {
            if (point == null)
            {
                throw new ArgumentNullException(nameof(point));
            }

            if (point.Longitude < -180 || point.Longitude > 180)
            {
                throw new ArgumentOutOfRangeException(nameof(point), "Longitude must be between -180 and 180.");
            }

            if (point.Latitude < -90 || point.Latitude > 90)
            {
                throw new ArgumentOutOfRangeException(nameof(point), "Latitude must be between -90 and 90.");
            }
        }
    }
}

