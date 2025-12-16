//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.TimeSeries
{
    /// <summary>
    /// Options for creating a MongoDB time series collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Time series collections efficiently store sequences of measurements over time.
    /// They are optimized for:
    /// <list type="bullet">
    ///   <item>High-volume inserts</item>
    ///   <item>Time-based queries</item>
    ///   <item>Aggregations over time windows</item>
    ///   <item>Automatic data compression</item>
    /// </list>
    /// </para>
    /// <para>
    /// Available since MongoDB 5.0.
    /// </para>
    /// </remarks>
    public class TimeSeriesOptions
    {
        /// <summary>
        /// Gets or sets the name of the field that contains the timestamp.
        /// This field is required and must be of type DateTime.
        /// </summary>
        public string TimeField { get; set; }

        /// <summary>
        /// Gets or sets the name of the field that contains metadata.
        /// Metadata is used to identify data sources (e.g., sensor ID, device ID).
        /// </summary>
        public string MetaField { get; set; }

        /// <summary>
        /// Gets or sets the granularity of the time series data.
        /// </summary>
        /// <remarks>
        /// Valid values: "seconds", "minutes", "hours".
        /// Choose based on how frequently data is recorded.
        /// </remarks>
        public string Granularity { get; set; } = "seconds";

        /// <summary>
        /// Gets or sets the bucket maximum span in seconds.
        /// Documents with timestamps within this span are grouped together.
        /// </summary>
        public int? BucketMaxSpanSeconds { get; set; }

        /// <summary>
        /// Gets or sets the bucket rounding in seconds.
        /// </summary>
        public int? BucketRoundingSeconds { get; set; }

        /// <summary>
        /// Gets or sets the expiration time for documents.
        /// Documents older than this will be automatically deleted.
        /// </summary>
        public TimeSpan? ExpireAfter { get; set; }
    }

    /// <summary>
    /// Granularity options for time series collections.
    /// </summary>
    public static class TimeSeriesGranularity
    {
        /// <summary>
        /// Granularity for data measured in seconds.
        /// </summary>
        public const string Seconds = "seconds";

        /// <summary>
        /// Granularity for data measured in minutes.
        /// </summary>
        public const string Minutes = "minutes";

        /// <summary>
        /// Granularity for data measured in hours.
        /// </summary>
        public const string Hours = "hours";
    }
}

