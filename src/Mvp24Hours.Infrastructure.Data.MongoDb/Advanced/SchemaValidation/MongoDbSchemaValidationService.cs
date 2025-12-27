//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.SchemaValidation
{
    /// <summary>
    /// Service for MongoDB Schema Validation operations.
    /// </summary>
    /// <example>
    /// <code>
    /// // Create a schema using the fluent builder
    /// var schema = new JsonSchemaBuilder()
    ///     .WithBsonType("object")
    ///     .WithRequired("name", "email")
    ///     .WithProperty("name", p => p.WithBsonType("string").WithMinLength(1))
    ///     .WithProperty("email", p => p.WithBsonType("string").WithPattern(@"^[\w-\.]+@"))
    ///     .Build();
    /// 
    /// // Create collection with validation
    /// await schemaService.CreateCollectionWithValidationAsync("users", schema);
    /// 
    /// // Or generate schema from a .NET type
    /// var autoSchema = schemaService.GenerateSchemaFromType&lt;User&gt;();
    /// await schemaService.SetValidationAsync("users", autoSchema);
    /// </code>
    /// </example>
    public class MongoDbSchemaValidationService : IMongoDbSchemaValidationService
    {
        private readonly IMongoDatabase _database;
        private readonly ILogger<MongoDbSchemaValidationService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDbSchemaValidationService"/> class.
        /// </summary>
        /// <param name="database">The MongoDB database.</param>
        /// <param name="logger">Optional logger.</param>
        public MongoDbSchemaValidationService(
            IMongoDatabase database,
            ILogger<MongoDbSchemaValidationService> logger = null)
        {
            _database = database ?? throw new ArgumentNullException(nameof(database));
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task CreateCollectionWithValidationAsync(
            string collectionName,
            BsonDocument jsonSchema,
            MongoDbSchemaValidationOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException("Collection name is required.", nameof(collectionName));
            }

            if (jsonSchema == null)
            {
                throw new ArgumentNullException(nameof(jsonSchema));
            }

            options ??= new MongoDbSchemaValidationOptions();

            // Use the command approach for validation as CreateCollectionOptions doesn't expose Validator directly
            var command = new BsonDocument
            {
                { "create", collectionName },
                { "validator", new BsonDocument("$jsonSchema", jsonSchema) },
                { "validationLevel", GetValidationLevelString(options.ValidationLevel) },
                { "validationAction", GetValidationActionString(options.ValidationAction) }
            };

            await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            _logger?.LogInformation("Collection '{CollectionName}' created with schema validation.", collectionName);
        }

        /// <inheritdoc/>
        public async Task SetValidationAsync(
            string collectionName,
            BsonDocument jsonSchema,
            MongoDbSchemaValidationOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException("Collection name is required.", nameof(collectionName));
            }

            if (jsonSchema == null)
            {
                throw new ArgumentNullException(nameof(jsonSchema));
            }

            options ??= new MongoDbSchemaValidationOptions();

            var command = new BsonDocument
            {
                { "collMod", collectionName },
                { "validator", new BsonDocument("$jsonSchema", jsonSchema) },
                { "validationLevel", GetValidationLevelString(options.ValidationLevel) },
                { "validationAction", GetValidationActionString(options.ValidationAction) }
            };

            await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            _logger?.LogInformation("Schema validation updated for collection '{CollectionName}'.", collectionName);
        }

        /// <inheritdoc/>
        public async Task RemoveValidationAsync(
            string collectionName,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                throw new ArgumentException("Collection name is required.", nameof(collectionName));
            }

            var command = new BsonDocument
            {
                { "collMod", collectionName },
                { "validator", new BsonDocument() },
                { "validationLevel", "off" }
            };

            await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            _logger?.LogInformation("Schema validation removed from collection '{CollectionName}'.", collectionName);
        }

        /// <inheritdoc/>
        public async Task<BsonDocument> GetValidationAsync(
            string collectionName,
            CancellationToken cancellationToken = default)
        {
            var command = new BsonDocument
            {
                { "listCollections", 1 },
                { "filter", new BsonDocument("name", collectionName) }
            };

            var result = await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

            if (result.Contains("cursor"))
            {
                var cursor = result["cursor"].AsBsonDocument;
                var firstBatch = cursor["firstBatch"].AsBsonArray;

                if (firstBatch.Count > 0)
                {
                    var collectionInfo = firstBatch[0].AsBsonDocument;
                    if (collectionInfo.Contains("options"))
                    {
                        var options = collectionInfo["options"].AsBsonDocument;
                        if (options.Contains("validator"))
                        {
                            return options["validator"].AsBsonDocument;
                        }
                    }
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<SchemaValidationResult> ValidateDocumentAsync<TDocument>(
            string collectionName,
            TDocument document,
            CancellationToken cancellationToken = default)
        {
            var schema = await GetValidationAsync(collectionName, cancellationToken);

            if (schema == null)
            {
                return SchemaValidationResult.Success();
            }

            try
            {
                // Attempt to insert into a temporary validation collection
                var tempCollectionName = $"_validation_temp_{Guid.NewGuid():N}";

                var command = new BsonDocument
                {
                    { "create", tempCollectionName },
                    { "validator", schema }
                };

                await _database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken);

                try
                {
                    var collection = _database.GetCollection<TDocument>(tempCollectionName);
                    await collection.InsertOneAsync(document, cancellationToken: cancellationToken);
                    return SchemaValidationResult.Success();
                }
                catch (MongoWriteException ex)
                {
                    return SchemaValidationResult.Failure(ex.WriteError?.Message ?? ex.Message);
                }
                finally
                {
                    await _database.DropCollectionAsync(tempCollectionName, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                return SchemaValidationResult.Failure(ex.Message);
            }
        }

        /// <inheritdoc/>
        public BsonDocument GenerateSchemaFromType<T>(bool includeRequired = true)
        {
            var type = typeof(T);
            var builder = new JsonSchemaBuilder().WithBsonType("object");
            var requiredFields = new List<string>();

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var propertyName = GetBsonPropertyName(property);
                var propertySchema = GeneratePropertySchema(property);

                builder.WithProperty(propertyName, propertySchema);

                if (includeRequired && IsRequired(property))
                {
                    requiredFields.Add(propertyName);
                }
            }

            if (requiredFields.Count > 0)
            {
                builder.WithRequired(requiredFields.ToArray());
            }

            return builder.Build();
        }

        private static string GetBsonPropertyName(PropertyInfo property)
        {
            var bsonElement = property.GetCustomAttribute<MongoDB.Bson.Serialization.Attributes.BsonElementAttribute>();
            return bsonElement?.ElementName ?? property.Name;
        }

        private static bool IsRequired(PropertyInfo property)
        {
            // Check for Required attribute
            if (property.GetCustomAttribute<RequiredAttribute>() != null)
            {
                return true;
            }

            // Check for non-nullable reference types (C# 8+)
            var nullabilityContext = new NullabilityInfoContext();
            var nullabilityInfo = nullabilityContext.Create(property);
            return nullabilityInfo.WriteState == NullabilityState.NotNull;
        }

        private static BsonDocument GeneratePropertySchema(PropertyInfo property)
        {
            var schema = new BsonDocument();
            var type = property.PropertyType;
            var underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType != null)
            {
                // Nullable type
                var bsonTypes = new BsonArray { GetBsonType(underlyingType), "null" };
                schema["bsonType"] = bsonTypes;
            }
            else
            {
                schema["bsonType"] = GetBsonType(type);
            }

            // Add validation from attributes
            AddAttributeValidation(property, schema);

            return schema;
        }

        private static string GetBsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(short) || type == typeof(byte)) return "int";
            if (type == typeof(long)) return "long";
            if (type == typeof(double) || type == typeof(float)) return "double";
            if (type == typeof(decimal)) return "decimal";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return "date";
            if (type == typeof(Guid)) return "string";
            if (type == typeof(ObjectId)) return "objectId";
            if (type == typeof(byte[])) return "binData";
            if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))) return "array";
            if (type.IsClass && type != typeof(string)) return "object";
            if (type.IsEnum) return "string";

            return "string";
        }

        private static void AddAttributeValidation(PropertyInfo property, BsonDocument schema)
        {
            var stringLength = property.GetCustomAttribute<StringLengthAttribute>();
            if (stringLength != null)
            {
                if (stringLength.MinimumLength > 0)
                    schema["minLength"] = stringLength.MinimumLength;
                if (stringLength.MaximumLength > 0)
                    schema["maxLength"] = stringLength.MaximumLength;
            }

            var maxLength = property.GetCustomAttribute<MaxLengthAttribute>();
            if (maxLength != null)
            {
                schema["maxLength"] = maxLength.Length;
            }

            var minLength = property.GetCustomAttribute<MinLengthAttribute>();
            if (minLength != null)
            {
                schema["minLength"] = minLength.Length;
            }

            var range = property.GetCustomAttribute<RangeAttribute>();
            if (range != null)
            {
                if (range.Minimum != null)
                    schema["minimum"] = Convert.ToDouble(range.Minimum);
                if (range.Maximum != null)
                    schema["maximum"] = Convert.ToDouble(range.Maximum);
            }

            var regex = property.GetCustomAttribute<RegularExpressionAttribute>();
            if (regex != null)
            {
                schema["pattern"] = regex.Pattern;
            }

            var email = property.GetCustomAttribute<EmailAddressAttribute>();
            if (email != null)
            {
                schema["pattern"] = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
            }
        }

        private static DocumentValidationLevel GetValidationLevel(SchemaValidationLevel level)
        {
            return level switch
            {
                SchemaValidationLevel.Off => DocumentValidationLevel.Off,
                SchemaValidationLevel.Strict => DocumentValidationLevel.Strict,
                SchemaValidationLevel.Moderate => DocumentValidationLevel.Moderate,
                _ => DocumentValidationLevel.Strict
            };
        }

        private static DocumentValidationAction GetValidationAction(SchemaValidationAction action)
        {
            return action switch
            {
                SchemaValidationAction.Error => DocumentValidationAction.Error,
                SchemaValidationAction.Warn => DocumentValidationAction.Warn,
                _ => DocumentValidationAction.Error
            };
        }

        private static string GetValidationLevelString(SchemaValidationLevel level)
        {
            return level switch
            {
                SchemaValidationLevel.Off => "off",
                SchemaValidationLevel.Strict => "strict",
                SchemaValidationLevel.Moderate => "moderate",
                _ => "strict"
            };
        }

        private static string GetValidationActionString(SchemaValidationAction action)
        {
            return action switch
            {
                SchemaValidationAction.Error => "error",
                SchemaValidationAction.Warn => "warn",
                _ => "error"
            };
        }
    }
}

