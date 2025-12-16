//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.SchemaValidation
{
    /// <summary>
    /// Options for MongoDB schema validation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MongoDB supports document validation via JSON Schema (draft-04) and
    /// query expression validators. Schema validation helps ensure data
    /// consistency and integrity at the database level.
    /// </para>
    /// </remarks>
    public class MongoDbSchemaValidationOptions
    {
        /// <summary>
        /// Gets or sets the validation level.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><c>off</c> - No validation</item>
        ///   <item><c>strict</c> - Validates all inserts and updates (default)</item>
        ///   <item><c>moderate</c> - Validates inserts and updates to existing valid documents</item>
        /// </list>
        /// </remarks>
        public SchemaValidationLevel ValidationLevel { get; set; } = SchemaValidationLevel.Strict;

        /// <summary>
        /// Gets or sets the validation action.
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        ///   <item><c>error</c> - Reject documents that fail validation (default)</item>
        ///   <item><c>warn</c> - Log warning but allow invalid documents</item>
        /// </list>
        /// </remarks>
        public SchemaValidationAction ValidationAction { get; set; } = SchemaValidationAction.Error;

        /// <summary>
        /// Gets or sets the JSON Schema validator.
        /// </summary>
        public BsonDocument JsonSchema { get; set; }
    }

    /// <summary>
    /// Schema validation levels.
    /// </summary>
    public enum SchemaValidationLevel
    {
        /// <summary>
        /// No validation is performed.
        /// </summary>
        Off,

        /// <summary>
        /// Validates all inserts and updates.
        /// </summary>
        Strict,

        /// <summary>
        /// Validates inserts and updates only for documents that already pass validation.
        /// </summary>
        Moderate
    }

    /// <summary>
    /// Schema validation actions.
    /// </summary>
    public enum SchemaValidationAction
    {
        /// <summary>
        /// Reject documents that fail validation.
        /// </summary>
        Error,

        /// <summary>
        /// Log a warning but allow invalid documents.
        /// </summary>
        Warn
    }
}

