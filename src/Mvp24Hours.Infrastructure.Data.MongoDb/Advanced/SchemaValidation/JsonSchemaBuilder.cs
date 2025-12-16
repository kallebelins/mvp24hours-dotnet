//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mvp24Hours.Infrastructure.Data.MongoDb.Advanced.SchemaValidation
{
    /// <summary>
    /// Fluent builder for creating JSON Schema validators for MongoDB.
    /// </summary>
    /// <remarks>
    /// <para>
    /// MongoDB uses JSON Schema draft-04 for document validation.
    /// This builder provides a fluent API for creating schemas.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var schema = new JsonSchemaBuilder()
    ///     .WithBsonType("object")
    ///     .WithRequired("name", "email", "age")
    ///     .WithProperty("name", p => p
    ///         .WithBsonType("string")
    ///         .WithMinLength(1)
    ///         .WithMaxLength(100))
    ///     .WithProperty("email", p => p
    ///         .WithBsonType("string")
    ///         .WithPattern(@"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$"))
    ///     .WithProperty("age", p => p
    ///         .WithBsonType("int")
    ///         .WithMinimum(0)
    ///         .WithMaximum(150))
    ///     .WithProperty("status", p => p
    ///         .WithEnum("active", "inactive", "pending"))
    ///     .Build();
    /// </code>
    /// </example>
    public class JsonSchemaBuilder
    {
        private readonly BsonDocument _schema;
        private readonly BsonDocument _properties;
        private readonly List<string> _required;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSchemaBuilder"/> class.
        /// </summary>
        public JsonSchemaBuilder()
        {
            _schema = new BsonDocument();
            _properties = new BsonDocument();
            _required = new List<string>();
        }

        /// <summary>
        /// Sets the BSON type for the schema.
        /// </summary>
        /// <param name="bsonType">The BSON type (object, array, string, int, etc.).</param>
        public JsonSchemaBuilder WithBsonType(string bsonType)
        {
            _schema["bsonType"] = bsonType;
            return this;
        }

        /// <summary>
        /// Sets the BSON type for the schema with multiple allowed types.
        /// </summary>
        /// <param name="bsonTypes">The allowed BSON types.</param>
        public JsonSchemaBuilder WithBsonTypes(params string[] bsonTypes)
        {
            _schema["bsonType"] = new BsonArray(bsonTypes);
            return this;
        }

        /// <summary>
        /// Sets the required fields.
        /// </summary>
        /// <param name="fields">The required field names.</param>
        public JsonSchemaBuilder WithRequired(params string[] fields)
        {
            _required.AddRange(fields);
            return this;
        }

        /// <summary>
        /// Adds a property definition.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="configure">Configuration action for the property schema.</param>
        public JsonSchemaBuilder WithProperty(string name, Action<PropertySchemaBuilder> configure)
        {
            var propertyBuilder = new PropertySchemaBuilder();
            configure(propertyBuilder);
            _properties[name] = propertyBuilder.Build();
            return this;
        }

        /// <summary>
        /// Adds a property definition with a pre-built schema.
        /// </summary>
        /// <param name="name">The property name.</param>
        /// <param name="schema">The property schema.</param>
        public JsonSchemaBuilder WithProperty(string name, BsonDocument schema)
        {
            _properties[name] = schema;
            return this;
        }

        /// <summary>
        /// Sets the description for the schema.
        /// </summary>
        /// <param name="description">The description.</param>
        public JsonSchemaBuilder WithDescription(string description)
        {
            _schema["description"] = description;
            return this;
        }

        /// <summary>
        /// Sets the title for the schema.
        /// </summary>
        /// <param name="title">The title.</param>
        public JsonSchemaBuilder WithTitle(string title)
        {
            _schema["title"] = title;
            return this;
        }

        /// <summary>
        /// Sets whether additional properties are allowed.
        /// </summary>
        /// <param name="allowed">Whether additional properties are allowed.</param>
        public JsonSchemaBuilder WithAdditionalProperties(bool allowed)
        {
            _schema["additionalProperties"] = allowed;
            return this;
        }

        /// <summary>
        /// Sets the minimum number of properties.
        /// </summary>
        /// <param name="min">Minimum number of properties.</param>
        public JsonSchemaBuilder WithMinProperties(int min)
        {
            _schema["minProperties"] = min;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of properties.
        /// </summary>
        /// <param name="max">Maximum number of properties.</param>
        public JsonSchemaBuilder WithMaxProperties(int max)
        {
            _schema["maxProperties"] = max;
            return this;
        }

        /// <summary>
        /// Builds the JSON Schema as a BsonDocument.
        /// </summary>
        public BsonDocument Build()
        {
            if (_properties.ElementCount > 0)
            {
                _schema["properties"] = _properties;
            }

            if (_required.Count > 0)
            {
                _schema["required"] = new BsonArray(_required.Distinct());
            }

            return _schema;
        }

        /// <summary>
        /// Builds the complete $jsonSchema validator document.
        /// </summary>
        public BsonDocument BuildValidator()
        {
            return new BsonDocument("$jsonSchema", Build());
        }
    }

    /// <summary>
    /// Builder for property schemas within a JSON Schema.
    /// </summary>
    public class PropertySchemaBuilder
    {
        private readonly BsonDocument _schema;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertySchemaBuilder"/> class.
        /// </summary>
        public PropertySchemaBuilder()
        {
            _schema = new BsonDocument();
        }

        /// <summary>
        /// Sets the BSON type for the property.
        /// </summary>
        /// <param name="bsonType">The BSON type.</param>
        public PropertySchemaBuilder WithBsonType(string bsonType)
        {
            _schema["bsonType"] = bsonType;
            return this;
        }

        /// <summary>
        /// Sets multiple allowed BSON types.
        /// </summary>
        /// <param name="bsonTypes">The allowed BSON types.</param>
        public PropertySchemaBuilder WithBsonTypes(params string[] bsonTypes)
        {
            _schema["bsonType"] = new BsonArray(bsonTypes);
            return this;
        }

        /// <summary>
        /// Sets the description for the property.
        /// </summary>
        /// <param name="description">The description.</param>
        public PropertySchemaBuilder WithDescription(string description)
        {
            _schema["description"] = description;
            return this;
        }

        /// <summary>
        /// Sets the minimum value for numeric properties.
        /// </summary>
        /// <param name="minimum">The minimum value.</param>
        public PropertySchemaBuilder WithMinimum(double minimum)
        {
            _schema["minimum"] = minimum;
            return this;
        }

        /// <summary>
        /// Sets the maximum value for numeric properties.
        /// </summary>
        /// <param name="maximum">The maximum value.</param>
        public PropertySchemaBuilder WithMaximum(double maximum)
        {
            _schema["maximum"] = maximum;
            return this;
        }

        /// <summary>
        /// Sets the exclusive minimum value.
        /// </summary>
        /// <param name="exclusiveMinimum">The exclusive minimum.</param>
        public PropertySchemaBuilder WithExclusiveMinimum(double exclusiveMinimum)
        {
            _schema["exclusiveMinimum"] = exclusiveMinimum;
            return this;
        }

        /// <summary>
        /// Sets the exclusive maximum value.
        /// </summary>
        /// <param name="exclusiveMaximum">The exclusive maximum.</param>
        public PropertySchemaBuilder WithExclusiveMaximum(double exclusiveMaximum)
        {
            _schema["exclusiveMaximum"] = exclusiveMaximum;
            return this;
        }

        /// <summary>
        /// Sets the minimum length for string properties.
        /// </summary>
        /// <param name="minLength">The minimum length.</param>
        public PropertySchemaBuilder WithMinLength(int minLength)
        {
            _schema["minLength"] = minLength;
            return this;
        }

        /// <summary>
        /// Sets the maximum length for string properties.
        /// </summary>
        /// <param name="maxLength">The maximum length.</param>
        public PropertySchemaBuilder WithMaxLength(int maxLength)
        {
            _schema["maxLength"] = maxLength;
            return this;
        }

        /// <summary>
        /// Sets the regex pattern for string properties.
        /// </summary>
        /// <param name="pattern">The regex pattern.</param>
        public PropertySchemaBuilder WithPattern(string pattern)
        {
            _schema["pattern"] = pattern;
            return this;
        }

        /// <summary>
        /// Sets the allowed values (enum).
        /// </summary>
        /// <param name="values">The allowed values.</param>
        public PropertySchemaBuilder WithEnum(params BsonValue[] values)
        {
            _schema["enum"] = new BsonArray(values);
            return this;
        }

        /// <summary>
        /// Sets the allowed string values (enum).
        /// </summary>
        /// <param name="values">The allowed string values.</param>
        public PropertySchemaBuilder WithEnum(params string[] values)
        {
            _schema["enum"] = new BsonArray(values.Select(v => (BsonValue)v));
            return this;
        }

        /// <summary>
        /// Sets the minimum number of items for array properties.
        /// </summary>
        /// <param name="minItems">The minimum number of items.</param>
        public PropertySchemaBuilder WithMinItems(int minItems)
        {
            _schema["minItems"] = minItems;
            return this;
        }

        /// <summary>
        /// Sets the maximum number of items for array properties.
        /// </summary>
        /// <param name="maxItems">The maximum number of items.</param>
        public PropertySchemaBuilder WithMaxItems(int maxItems)
        {
            _schema["maxItems"] = maxItems;
            return this;
        }

        /// <summary>
        /// Sets whether array items must be unique.
        /// </summary>
        /// <param name="uniqueItems">Whether items must be unique.</param>
        public PropertySchemaBuilder WithUniqueItems(bool uniqueItems)
        {
            _schema["uniqueItems"] = uniqueItems;
            return this;
        }

        /// <summary>
        /// Sets the schema for array items.
        /// </summary>
        /// <param name="configure">Configuration action for item schema.</param>
        public PropertySchemaBuilder WithItems(Action<PropertySchemaBuilder> configure)
        {
            var itemBuilder = new PropertySchemaBuilder();
            configure(itemBuilder);
            _schema["items"] = itemBuilder.Build();
            return this;
        }

        /// <summary>
        /// Builds the property schema.
        /// </summary>
        public BsonDocument Build()
        {
            return _schema;
        }
    }
}

