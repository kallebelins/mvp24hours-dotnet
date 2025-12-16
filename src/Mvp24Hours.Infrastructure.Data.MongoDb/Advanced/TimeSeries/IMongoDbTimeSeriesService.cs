//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TimeSeries
{
    /// <summary>
    /// Interface for MongoDB Time Series collection operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Time Series collections are optimized for storing and querying
    /// time-stamped data such as:
    /// <list type="bullet">
    ///   <item>IoT sensor readings</item>
    ///   <item>Application metrics</item>
    ///   <item>Financial data</item>
    ///   <item>Log entries</item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IMongoDbTimeSeriesService<TDocument>
    {
        /// <summary>
        /// Gets the time series collection.
        /// </summary>
        IMongoCollection<TDocument> Collection { get; }

        /// <summary>
        /// Creates a time series collection.
        /// </summary>
        /// <param name="collectionName">The name of the collection.</param>
        /// <param name="options">Time series options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CreateTimeSeriesCollectionAsync(
            string collectionName,
            TimeSeriesOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts a single measurement.
        /// </summary>
        /// <param name="document">The document to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InsertMeasurementAsync(
            TDocument document,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Inserts multiple measurements.
        /// </summary>
        /// <param name="documents">The documents to insert.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task InsertMeasurementsAsync(
            IEnumerable<TDocument> documents,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries measurements within a time range.
        /// </summary>
        /// <param name="start">Start time (inclusive).</param>
        /// <param name="end">End time (exclusive).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of measurements.</returns>
        Task<IList<TDocument>> QueryByTimeRangeAsync(
            DateTime start,
            DateTime end,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Queries measurements within a time range with additional filter.
        /// </summary>
        /// <param name="start">Start time (inclusive).</param>
        /// <param name="end">End time (exclusive).</param>
        /// <param name="filter">Additional filter.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A list of measurements.</returns>
        Task<IList<TDocument>> QueryByTimeRangeAsync(
            DateTime start,
            DateTime end,
            FilterDefinition<TDocument> filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Aggregates measurements by time window.
        /// </summary>
        /// <param name="start">Start time.</param>
        /// <param name="end">End time.</param>
        /// <param name="windowSize">The size of each time window.</param>
        /// <param name="aggregationField">The field to aggregate.</param>
        /// <param name="aggregationType">The type of aggregation (sum, avg, min, max, count).</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Aggregated results by time window.</returns>
        Task<IList<TimeWindowAggregation>> AggregateByTimeWindowAsync(
            DateTime start,
            DateTime end,
            TimeSpan windowSize,
            string aggregationField,
            TimeSeriesAggregationType aggregationType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest measurement.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The latest measurement, or null if none exist.</returns>
        Task<TDocument> GetLatestMeasurementAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the latest measurement for a specific metadata value.
        /// </summary>
        /// <param name="metaFieldValue">The metadata field value to filter by.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The latest measurement, or null if none exist.</returns>
        Task<TDocument> GetLatestMeasurementByMetaAsync(
            object metaFieldValue,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes measurements older than the specified time.
        /// </summary>
        /// <param name="olderThan">Delete measurements older than this time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The number of deleted documents.</returns>
        Task<long> DeleteMeasurementsOlderThanAsync(
            DateTime olderThan,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets collection statistics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Collection statistics.</returns>
        Task<BsonDocument> GetCollectionStatsAsync(
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents an aggregated time window result.
    /// </summary>
    public class TimeWindowAggregation
    {
        /// <summary>
        /// Gets or sets the start time of the window.
        /// </summary>
        public DateTime WindowStart { get; set; }

        /// <summary>
        /// Gets or sets the end time of the window.
        /// </summary>
        public DateTime WindowEnd { get; set; }

        /// <summary>
        /// Gets or sets the aggregated value.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the count of documents in the window.
        /// </summary>
        public long Count { get; set; }
    }

    /// <summary>
    /// Types of aggregations for time series data.
    /// </summary>
    public enum TimeSeriesAggregationType
    {
        /// <summary>
        /// Sum of values.
        /// </summary>
        Sum,

        /// <summary>
        /// Average of values.
        /// </summary>
        Average,

        /// <summary>
        /// Minimum value.
        /// </summary>
        Min,

        /// <summary>
        /// Maximum value.
        /// </summary>
        Max,

        /// <summary>
        /// Count of documents.
        /// </summary>
        Count,

        /// <summary>
        /// Standard deviation of values.
        /// </summary>
        StdDev,

        /// <summary>
        /// First value in the window.
        /// </summary>
        First,

        /// <summary>
        /// Last value in the window.
        /// </summary>
        Last
    }
}

