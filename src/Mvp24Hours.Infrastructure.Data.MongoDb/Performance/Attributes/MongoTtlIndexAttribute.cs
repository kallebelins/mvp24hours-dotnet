//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes
{
    /// <summary>
    /// Specifies that a MongoDB TTL (Time-To-Live) index should be created for automatic document expiration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// TTL indexes automatically delete documents after a specified number of seconds.
    /// The indexed field must be a DateTime or DateTimeOffset type.
    /// </para>
    /// <para>
    /// MongoDB's TTL monitor runs every 60 seconds, so actual deletion may be delayed.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// public class SessionToken : EntityBase&lt;Guid&gt;
    /// {
    ///     public string Token { get; set; }
    ///     
    ///     // Documents expire 24 hours after CreatedAt
    ///     [MongoTtlIndex(ExpireAfterSeconds = 86400)]
    ///     public DateTime CreatedAt { get; set; }
    /// }
    /// 
    /// public class TemporaryFile : EntityBase&lt;Guid&gt;
    /// {
    ///     public string FileName { get; set; }
    ///     
    ///     // Documents expire at the ExpiresAt datetime (ExpireAfterSeconds = 0)
    ///     [MongoTtlIndex(ExpireAfterSeconds = 0)]
    ///     public DateTime ExpiresAt { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class MongoTtlIndexAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the number of seconds after the indexed field value when the document expires.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If set to 0, documents expire at the exact time specified in the indexed field.
        /// </para>
        /// <para>
        /// Common values:
        /// <list type="bullet">
        ///   <item>3600 - 1 hour</item>
        ///   <item>86400 - 24 hours</item>
        ///   <item>604800 - 7 days</item>
        ///   <item>2592000 - 30 days</item>
        /// </list>
        /// </para>
        /// </remarks>
        public long ExpireAfterSeconds { get; set; }

        /// <summary>
        /// Gets or sets the custom name for the index.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the index should be built in the background.
        /// </summary>
        public bool Background { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoTtlIndexAttribute"/> class.
        /// </summary>
        public MongoTtlIndexAttribute() { }

        /// <summary>
        /// Initializes a new instance with the specified expiration time.
        /// </summary>
        /// <param name="expireAfterSeconds">Number of seconds until document expiration.</param>
        public MongoTtlIndexAttribute(long expireAfterSeconds)
        {
            ExpireAfterSeconds = expireAfterSeconds;
        }
    }
}

