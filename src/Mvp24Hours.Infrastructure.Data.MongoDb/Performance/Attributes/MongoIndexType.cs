//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes
{
    /// <summary>
    /// Specifies the type of MongoDB index to create.
    /// </summary>
    public enum MongoIndexType
    {
        /// <summary>
        /// Standard ascending index (default). Efficient for equality and range queries.
        /// </summary>
        Ascending = 0,

        /// <summary>
        /// Standard descending index. Useful for queries that sort in descending order.
        /// </summary>
        Descending = 1,

        /// <summary>
        /// Hashed index. Required for hash-based sharding and supports equality queries only.
        /// </summary>
        Hashed = 2,

        /// <summary>
        /// Full-text search index. Supports text search operations on string content.
        /// </summary>
        /// <remarks>
        /// A collection can have at most one text index.
        /// Text indexes support language-specific stemming and stop words.
        /// </remarks>
        Text = 3,

        /// <summary>
        /// 2D planar geospatial index. For legacy coordinate pairs on a flat surface.
        /// </summary>
        /// <remarks>
        /// Use <see cref="Geo2dSphere"/> for spherical (Earth) coordinates.
        /// </remarks>
        Geo2d = 4,

        /// <summary>
        /// 2DSphere geospatial index. For spherical geometry calculations on Earth-like surfaces.
        /// </summary>
        /// <remarks>
        /// Supports GeoJSON objects and legacy coordinate pairs.
        /// Provides accurate distance calculations for Earth coordinates.
        /// </remarks>
        Geo2dSphere = 5,

        /// <summary>
        /// Wildcard index. Indexes all fields or a specified subset of fields.
        /// </summary>
        /// <remarks>
        /// Useful for querying documents with dynamic or unknown schemas.
        /// Available in MongoDB 4.2+.
        /// </remarks>
        Wildcard = 6
    }
}

