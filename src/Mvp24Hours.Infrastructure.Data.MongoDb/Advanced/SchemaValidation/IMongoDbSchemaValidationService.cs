//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.SchemaValidation
{
    /// <summary>
    /// Interface for MongoDB Schema Validation operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Schema validation allows you to enforce data integrity at the database level.
    /// MongoDB supports JSON Schema validation (draft-04).
    /// </para>
    /// </remarks>
    public interface IMongoDbSchemaValidationService
    {
        /// <summary>
        /// Creates a collection with schema validation.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="jsonSchema">The JSON Schema validator.</param>
        /// <param name="options">Validation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CreateCollectionWithValidationAsync(
            string collectionName,
            BsonDocument jsonSchema,
            MongoDbSchemaValidationOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets or updates schema validation on an existing collection.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="jsonSchema">The JSON Schema validator.</param>
        /// <param name="options">Validation options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task SetValidationAsync(
            string collectionName,
            BsonDocument jsonSchema,
            MongoDbSchemaValidationOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes schema validation from a collection.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task RemoveValidationAsync(
            string collectionName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current validation rules for a collection.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The validation rules, or null if no validation is set.</returns>
        Task<BsonDocument> GetValidationAsync(
            string collectionName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates a document against the collection's schema.
        /// </summary>
        /// <typeparam name="TDocument">The document type.</typeparam>
        /// <param name="collectionName">The collection name.</param>
        /// <param name="document">The document to validate.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Validation result with any errors.</returns>
        Task<SchemaValidationResult> ValidateDocumentAsync<TDocument>(
            string collectionName,
            TDocument document,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a JSON Schema from a .NET type.
        /// </summary>
        /// <typeparam name="T">The type to generate schema for.</typeparam>
        /// <param name="includeRequired">Whether to include required properties.</param>
        /// <returns>The generated JSON Schema.</returns>
        BsonDocument GenerateSchemaFromType<T>(bool includeRequired = true);
    }

    /// <summary>
    /// Result of schema validation.
    /// </summary>
    public class SchemaValidationResult
    {
        /// <summary>
        /// Gets or sets whether the document is valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the validation errors.
        /// </summary>
        public string[] Errors { get; set; }

        /// <summary>
        /// Creates a successful validation result.
        /// </summary>
        public static SchemaValidationResult Success()
        {
            return new SchemaValidationResult { IsValid = true, Errors = System.Array.Empty<string>() };
        }

        /// <summary>
        /// Creates a failed validation result.
        /// </summary>
        /// <param name="errors">The validation errors.</param>
        public static SchemaValidationResult Failure(params string[] errors)
        {
            return new SchemaValidationResult { IsValid = false, Errors = errors };
        }
    }
}

