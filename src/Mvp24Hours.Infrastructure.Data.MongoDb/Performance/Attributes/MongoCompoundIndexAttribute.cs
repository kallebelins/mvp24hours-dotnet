//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Performance.Attributes
{
    /// <summary>
    /// Specifies that a MongoDB compound index should be created for multiple fields.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Apply this attribute at the class level to define compound indexes that span multiple fields.
    /// </para>
    /// <para>
    /// Compound indexes follow the ESR (Equality, Sort, Range) rule for optimal performance:
    /// <list type="number">
    ///   <item>Fields used in equality conditions should come first</item>
    ///   <item>Fields used for sorting should come next</item>
    ///   <item>Fields used in range queries should come last</item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Single compound index
    /// [MongoCompoundIndex(Fields = "Status:1,CreatedAt:-1", Name = "idx_status_date")]
    /// public class Order : EntityBase&lt;Guid&gt;
    /// {
    ///     public string Status { get; set; }
    ///     public DateTime CreatedAt { get; set; }
    ///     public decimal TotalAmount { get; set; }
    /// }
    /// 
    /// // Multiple compound indexes
    /// [MongoCompoundIndex(Fields = "CustomerId:1,Status:1", Name = "idx_customer_status")]
    /// [MongoCompoundIndex(Fields = "Status:1,TotalAmount:-1", Name = "idx_status_amount")]
    /// public class Order : EntityBase&lt;Guid&gt;
    /// {
    ///     public Guid CustomerId { get; set; }
    ///     public string Status { get; set; }
    ///     public decimal TotalAmount { get; set; }
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class MongoCompoundIndexAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the fields specification for the compound index.
        /// </summary>
        /// <remarks>
        /// Format: "Field1:Direction,Field2:Direction,..."
        /// Direction: 1 for ascending, -1 for descending, "text" for text, "hashed" for hashed.
        /// </remarks>
        /// <example>
        /// "Status:1,CreatedAt:-1" creates an ascending index on Status and descending on CreatedAt.
        /// </example>
        public string Fields { get; set; }

        /// <summary>
        /// Gets or sets the custom name for the index.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets whether the index enforces uniqueness.
        /// </summary>
        public bool Unique { get; set; }

        /// <summary>
        /// Gets or sets whether the index is sparse.
        /// </summary>
        public bool Sparse { get; set; }

        /// <summary>
        /// Gets or sets whether the index should be built in the background.
        /// </summary>
        public bool Background { get; set; } = true;

        /// <summary>
        /// Gets or sets the partial filter expression as a JSON string.
        /// </summary>
        /// <example>
        /// <code>
        /// [MongoCompoundIndex(Fields = "Email:1", PartialFilterExpression = "{ \"IsActive\": true }")]
        /// </code>
        /// </example>
        public string PartialFilterExpression { get; set; }

        /// <summary>
        /// Gets or sets the collation locale for string comparisons.
        /// </summary>
        public string CollationLocale { get; set; }

        /// <summary>
        /// Gets or sets whether the collation should be case-insensitive.
        /// </summary>
        public bool CollationCaseInsensitive { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoCompoundIndexAttribute"/> class.
        /// </summary>
        public MongoCompoundIndexAttribute() { }

        /// <summary>
        /// Initializes a new instance with the specified fields.
        /// </summary>
        /// <param name="fields">The fields specification (e.g., "Field1:1,Field2:-1").</param>
        public MongoCompoundIndexAttribute(string fields)
        {
            Fields = fields;
        }
    }
}

