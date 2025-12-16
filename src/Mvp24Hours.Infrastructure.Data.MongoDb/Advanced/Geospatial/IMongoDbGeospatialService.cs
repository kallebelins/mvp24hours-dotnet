//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.Geospatial
{
    /// <summary>
    /// Interface for MongoDB Geospatial operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Supports geospatial queries including:
    /// <list type="bullet">
    ///   <item>$near / $nearSphere - Find documents near a point</item>
    ///   <item>$geoWithin - Find documents within a shape</item>
    ///   <item>$geoIntersects - Find documents that intersect with a geometry</item>
    ///   <item>$geoNear - Aggregation pipeline stage for distance calculation</item>
    /// </list>
    /// </para>
    /// <para>
    /// Requires a 2dsphere index on the location field.
    /// </para>
    /// </remarks>
    public interface IMongoDbGeospatialService<TDocument>
    {
        /// <summary>
        /// Creates a 2dsphere index on a location field.
        /// </summary>
        /// <param name="locationField">The field containing GeoJSON location.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The name of the created index.</returns>
        Task<string> Create2dSphereIndexAsync(
            string locationField,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a compound 2dsphere index with additional fields.
        /// </summary>
        /// <param name="locationField">The field containing GeoJSON location.</param>
        /// <param name="additionalFields">Additional fields to include in the index.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The name of the created index.</returns>
        Task<string> Create2dSphereIndexAsync(
            string locationField,
            IDictionary<string, int> additionalFields,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds documents near a point.
        /// </summary>
        /// <param name="locationField">The location field name.</param>
        /// <param name="point">The center point.</param>
        /// <param name="maxDistanceMeters">Maximum distance in meters.</param>
        /// <param name="minDistanceMeters">Minimum distance in meters (optional).</param>
        /// <param name="limit">Maximum number of results.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Documents near the point, sorted by distance.</returns>
        Task<IList<TDocument>> FindNearAsync(
            string locationField,
            GeoPoint point,
            double maxDistanceMeters,
            double? minDistanceMeters = null,
            int? limit = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds documents near a point with additional filter.
        /// </summary>
        /// <param name="locationField">The location field name.</param>
        /// <param name="point">The center point.</param>
        /// <param name="maxDistanceMeters">Maximum distance in meters.</param>
        /// <param name="filter">Additional filter.</param>
        /// <param name="limit">Maximum number of results.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Documents near the point, sorted by distance.</returns>
        Task<IList<TDocument>> FindNearAsync(
            string locationField,
            GeoPoint point,
            double maxDistanceMeters,
            FilterDefinition<TDocument> filter,
            int? limit = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds documents near a point with distance information.
        /// </summary>
        /// <param name="locationField">The location field name.</param>
        /// <param name="point">The center point.</param>
        /// <param name="maxDistanceMeters">Maximum distance in meters.</param>
        /// <param name="limit">Maximum number of results.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Documents with distance information.</returns>
        Task<IList<GeoNearResult<TDocument>>> FindNearWithDistanceAsync(
            string locationField,
            GeoPoint point,
            double maxDistanceMeters,
            int? limit = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds documents within a polygon.
        /// </summary>
        /// <param name="locationField">The location field name.</param>
        /// <param name="polygon">The polygon to search within.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Documents within the polygon.</returns>
        Task<IList<TDocument>> FindWithinPolygonAsync(
            string locationField,
            GeoPolygon polygon,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds documents within a circle (center sphere).
        /// </summary>
        /// <param name="locationField">The location field name.</param>
        /// <param name="center">The center point.</param>
        /// <param name="radiusMeters">The radius in meters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Documents within the circle.</returns>
        Task<IList<TDocument>> FindWithinCircleAsync(
            string locationField,
            GeoPoint center,
            double radiusMeters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds documents within a bounding box.
        /// </summary>
        /// <param name="locationField">The location field name.</param>
        /// <param name="southWest">The south-west corner.</param>
        /// <param name="northEast">The north-east corner.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Documents within the bounding box.</returns>
        Task<IList<TDocument>> FindWithinBoxAsync(
            string locationField,
            GeoPoint southWest,
            GeoPoint northEast,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Finds documents that intersect with a geometry.
        /// </summary>
        /// <param name="locationField">The location field name.</param>
        /// <param name="polygon">The polygon to test intersection.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Documents that intersect with the polygon.</returns>
        Task<IList<TDocument>> FindIntersectsAsync(
            string locationField,
            GeoPolygon polygon,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Counts documents within a radius.
        /// </summary>
        /// <param name="locationField">The location field name.</param>
        /// <param name="center">The center point.</param>
        /// <param name="radiusMeters">The radius in meters.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The count of documents within the radius.</returns>
        Task<long> CountWithinRadiusAsync(
            string locationField,
            GeoPoint center,
            double radiusMeters,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a $geoNear query including distance information.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    public class GeoNearResult<TDocument>
    {
        /// <summary>
        /// Gets or sets the document.
        /// </summary>
        public TDocument Document { get; set; }

        /// <summary>
        /// Gets or sets the distance from the search point in meters.
        /// </summary>
        public double DistanceMeters { get; set; }
    }
}

