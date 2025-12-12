//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.ValueObjects;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Core.Serialization.Json
{
    /// <summary>
    /// JSON converter for Guid-based strongly-typed entity IDs (System.Text.Json).
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    /// <example>
    /// <code>
    /// // Register globally:
    /// services.AddControllers()
    ///     .AddJsonOptions(options =>
    ///     {
    ///         options.JsonSerializerOptions.Converters.Add(new GuidEntityIdJsonConverter&lt;CustomerId&gt;());
    ///     });
    /// 
    /// // Or use as attribute:
    /// [JsonConverter(typeof(GuidEntityIdJsonConverter&lt;CustomerId&gt;))]
    /// public CustomerId Id { get; set; }
    /// </code>
    /// </example>
    public class GuidEntityIdJsonConverter<TId> : JsonConverter<TId>
        where TId : GuidEntityId<TId>
    {
        private static readonly Func<Guid, TId> _createInstance = CreateFactory();

        /// <inheritdoc />
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (Guid.TryParse(stringValue, out var guid))
                {
                    return _createInstance(guid);
                }
            }

            throw new JsonException($"Cannot convert value to {typeof(TId).Name}");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value.ToString());
            }
        }

        private static Func<Guid, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(Guid) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts a Guid parameter.");
            }

            var parameter = Expression.Parameter(typeof(Guid), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<Guid, TId>>(body, parameter).Compile();
        }
    }

    /// <summary>
    /// JSON converter for int-based strongly-typed entity IDs (System.Text.Json).
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    public class IntEntityIdJsonConverter<TId> : JsonConverter<TId>
        where TId : IntEntityId<TId>
    {
        private static readonly Func<int, TId> _createInstance = CreateFactory();

        /// <inheritdoc />
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return _createInstance(reader.GetInt32());
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (int.TryParse(stringValue, out var intValue))
                {
                    return _createInstance(intValue);
                }
            }

            throw new JsonException($"Cannot convert value to {typeof(TId).Name}");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value.Value);
            }
        }

        private static Func<int, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(int) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts an int parameter.");
            }

            var parameter = Expression.Parameter(typeof(int), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<int, TId>>(body, parameter).Compile();
        }
    }

    /// <summary>
    /// JSON converter for long-based strongly-typed entity IDs (System.Text.Json).
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    public class LongEntityIdJsonConverter<TId> : JsonConverter<TId>
        where TId : LongEntityId<TId>
    {
        private static readonly Func<long, TId> _createInstance = CreateFactory();

        /// <inheritdoc />
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                return _createInstance(reader.GetInt64());
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (long.TryParse(stringValue, out var longValue))
                {
                    return _createInstance(longValue);
                }
            }

            throw new JsonException($"Cannot convert value to {typeof(TId).Name}");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteNumberValue(value.Value);
            }
        }

        private static Func<long, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(long) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts a long parameter.");
            }

            var parameter = Expression.Parameter(typeof(long), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<long, TId>>(body, parameter).Compile();
        }
    }

    /// <summary>
    /// JSON converter for string-based strongly-typed entity IDs (System.Text.Json).
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    public class StringEntityIdJsonConverter<TId> : JsonConverter<TId>
        where TId : StringEntityId<TId>
    {
        private static readonly Func<string, TId> _createInstance = CreateFactory();

        /// <inheritdoc />
        public override TId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                return _createInstance(stringValue);
            }

            throw new JsonException($"Cannot convert value to {typeof(TId).Name}");
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, TId value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.Value);
            }
        }

        private static Func<string, TId> CreateFactory()
        {
            var ctor = typeof(TId).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(TId).Name} must have a constructor that accepts a string parameter.");
            }

            var parameter = Expression.Parameter(typeof(string), "value");
            var body = Expression.New(ctor, parameter);
            return Expression.Lambda<Func<string, TId>>(body, parameter).Compile();
        }
    }

    /// <summary>
    /// JSON converter factory that automatically creates converters for strongly-typed entity IDs.
    /// </summary>
    /// <example>
    /// <code>
    /// services.AddControllers()
    ///     .AddJsonOptions(options =>
    ///     {
    ///         options.JsonSerializerOptions.Converters.Add(new EntityIdJsonConverterFactory());
    ///     });
    /// </code>
    /// </example>
    public class EntityIdJsonConverterFactory : JsonConverterFactory
    {
        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            if (typeToConvert == null) return false;

            return IsGuidEntityId(typeToConvert) ||
                   IsIntEntityId(typeToConvert) ||
                   IsLongEntityId(typeToConvert) ||
                   IsStringEntityId(typeToConvert);
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (IsGuidEntityId(typeToConvert))
            {
                var converterType = typeof(GuidEntityIdJsonConverter<>).MakeGenericType(typeToConvert);
                return (JsonConverter)Activator.CreateInstance(converterType);
            }

            if (IsIntEntityId(typeToConvert))
            {
                var converterType = typeof(IntEntityIdJsonConverter<>).MakeGenericType(typeToConvert);
                return (JsonConverter)Activator.CreateInstance(converterType);
            }

            if (IsLongEntityId(typeToConvert))
            {
                var converterType = typeof(LongEntityIdJsonConverter<>).MakeGenericType(typeToConvert);
                return (JsonConverter)Activator.CreateInstance(converterType);
            }

            if (IsStringEntityId(typeToConvert))
            {
                var converterType = typeof(StringEntityIdJsonConverter<>).MakeGenericType(typeToConvert);
                return (JsonConverter)Activator.CreateInstance(converterType);
            }

            throw new NotSupportedException($"Cannot create converter for type {typeToConvert}");
        }

        private static bool IsGuidEntityId(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(GuidEntityId<>);
        }

        private static bool IsIntEntityId(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(IntEntityId<>);
        }

        private static bool IsLongEntityId(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(LongEntityId<>);
        }

        private static bool IsStringEntityId(Type type)
        {
            return type.BaseType != null &&
                   type.BaseType.IsGenericType &&
                   type.BaseType.GetGenericTypeDefinition() == typeof(StringEntityId<>);
        }
    }
}

