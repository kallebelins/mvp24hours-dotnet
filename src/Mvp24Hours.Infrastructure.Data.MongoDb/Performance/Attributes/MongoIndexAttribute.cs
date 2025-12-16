//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes
{
    /// <summary>
    /// Specifies that a MongoDB index should be created for the property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This attribute can be applied to properties to automatically create MongoDB indexes
    /// when the collection is first accessed or during application startup.
    /// </para>
    /// <para>
    /// Supported index types:
    /// <list type="bullet">
    ///   <item><see cref="MongoIndexType.Ascending"/> - Standard ascending index (default)</item>
    ///   <item><see cref="MongoIndexType.Descending"/> - Standard descending index</item>
    ///   <item><see cref="MongoIndexType.Hashed"/> - Hashed index for sharding</item>
    ///   <item><see cref="MongoIndexType.Text"/> - Full-text search index</item>
    ///   <item><see cref="MongoIndexType.Geo2d"/> - 2D geospatial index</item>
    ///   <item><see cref="MongoIndexType.Geo2dSphere"/> - 2DSphere geospatial index</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class Customer : EntityBase&lt;Guid&gt;
    /// {
    ///     [MongoIndex(Unique = true)]
    ///     public string Email { get; set; }
    ///     
    ///     [MongoIndex(IndexType = MongoIndexType.Text)]
    ///     public string Name { get; set; }
    ///     
    ///     [MongoIndex(IndexType = MongoIndexType.Geo2dSphere)]
    ///     public GeoJsonPoint&lt;GeoJson2DGeographicCoordinates&gt; Location { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class MongoIndexAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the index type.
        /// </summary>
        public MongoIndexType IndexType { get; set; } = MongoIndexType.Ascending;

        /// <summary>
        /// Gets or sets whether the index enforces uniqueness.
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Gets or sets whether the index is sparse (only includes documents with the indexed field).
        /// </summary>
        public bool Sparse { get; set; }

        /// <summary>
        /// Gets or sets whether the index should be built in the background.
        /// </summary>
        /// <remarks>
        /// Background index builds allow database operations to continue during index creation.
        /// Note: In MongoDB 4.2+, all index builds use an optimized build process.
        /// </remarks>
        public bool Background { get; set; } = true;

        /// <summary>
        /// Gets or sets the custom name for the index.
        /// </summary>
        /// <remarks>
        /// If not specified, MongoDB generates a name based on the indexed fields.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the order of this field in a compound index.
        /// </summary>
        /// <remarks>
        /// When multiple properties have the same <see cref="CompoundIndexGroup"/>,
        /// they are combined into a single compound index ordered by this value.
        /// </remarks>
        public int Order { get; set; }

        /// <summary>
        /// Gets or sets the compound index group name.
        /// </summary>
        /// <remarks>
        /// Properties with the same group name will be combined into a compound index.
        /// Use this with <see cref="Order"/> to control field order in the compound index.
        /// </remarks>
        public string CompoundIndexGroup { get; set; }

        /// <summary>
        /// Gets or sets the partial filter expression as a JSON string.
        /// </summary>
        /// <remarks>
        /// Allows creating a partial index that only includes documents matching the filter.
        /// </remarks>
        /// <example>
        /// <code>
        /// [MongoIndex(PartialFilterExpression = "{ \"status\": \"active\" }")]
        /// public string Email { get; set; }
        /// </code>
        /// </example>
        public string PartialFilterExpression { get; set; }

        /// <summary>
        /// Gets or sets the collation locale for string comparisons.
        /// </summary>
        /// <example>"en", "pt", "fr"</example>
        public string CollationLocale { get; set; }

        /// <summary>
        /// Gets or sets whether the collation should be case-insensitive.
        /// </summary>
        public bool CollationCaseInsensitive { get; set; }
    }
}

