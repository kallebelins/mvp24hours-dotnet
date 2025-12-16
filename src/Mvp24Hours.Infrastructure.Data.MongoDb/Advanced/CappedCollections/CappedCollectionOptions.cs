//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.CappedCollections
{
    /// <summary>
    /// Options for creating a MongoDB capped collection.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Capped collections are fixed-size collections that support high-throughput
    /// operations. They work like circular buffers: once a collection fills its
    /// allocated space, it makes room for new documents by overwriting the oldest.
    /// </para>
    /// <para>
    /// Use cases:
    /// <list type="bullet">
    ///   <item>Logging systems</item>
    ///   <item>Cache data</item>
    ///   <item>Real-time analytics</item>
    ///   <item>Event streaming (with tailable cursors)</item>
    /// </list>
    /// </para>
    /// <para>
    /// Limitations:
    /// <list type="bullet">
    ///   <item>Cannot delete individual documents</item>
    ///   <item>Cannot update documents to increase size</item>
    ///   <item>Cannot shard capped collections</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class CappedCollectionOptions
    {
        /// <summary>
        /// Gets or sets the maximum size of the collection in bytes.
        /// This is required.
        /// </summary>
        public long MaxSizeBytes { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of documents in the collection.
        /// Optional. If specified, the collection is capped by both size and document count.
        /// </summary>
        public long? MaxDocuments { get; set; }

        /// <summary>
        /// Gets or sets whether to create an index on the _id field.
        /// Default is true.
        /// </summary>
        /// <remarks>
        /// Set to false if _id queries are not needed and you want to optimize storage.
        /// However, this prevents using findById operations.
        /// </remarks>
        public bool AutoIndexId { get; set; } = true;
    }
}

