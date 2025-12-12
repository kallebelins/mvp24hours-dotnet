//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.ValueObjects;
using Newtonsoft.Json;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Mvp24Hours.Core.Converters
{
    /// <summary>
    /// JSON converter for Guid-based strongly-typed entity IDs (Newtonsoft.Json).
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    /// <example>
    /// <code>
    /// // Register globally:
    /// JsonConvert.DefaultSettings = () => new JsonSerializerSettings
    /// {
    ///     Converters = new List&lt;JsonConverter&gt;
    ///     {
    ///         new GuidEntityIdNewtonsoftConverter&lt;CustomerId&gt;()
    ///     }
    /// };
    /// 
    /// // Or use as attribute:
    /// [JsonConverter(typeof(GuidEntityIdNewtonsoftConverter&lt;CustomerId&gt;))]
    /// public CustomerId Id { get; set; }
    /// </code>
    /// </example>
    public class GuidEntityIdNewtonsoftConverter<TId> : JsonConverter<TId>
        where TId : GuidEntityId<TId>
    {
        private static readonly Func<Guid, TId> _createInstance = CreateFactory();

        /// <inheritdoc />
        public override TId ReadJson(JsonReader reader, Type objectType, TId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                var stringValue = reader.Value?.ToString();
                if (Guid.TryParse(stringValue, out var guid))
                {
                    return _createInstance(guid);
                }
            }

            throw new JsonSerializationException($"Cannot convert value to {typeof(TId).Name}");
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, TId value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.Value.ToString());
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
    /// JSON converter for int-based strongly-typed entity IDs (Newtonsoft.Json).
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    public class IntEntityIdNewtonsoftConverter<TId> : JsonConverter<TId>
        where TId : IntEntityId<TId>
    {
        private static readonly Func<int, TId> _createInstance = CreateFactory();

        /// <inheritdoc />
        public override TId ReadJson(JsonReader reader, Type objectType, TId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                return _createInstance(Convert.ToInt32(reader.Value));
            }

            if (reader.TokenType == JsonToken.String)
            {
                var stringValue = reader.Value?.ToString();
                if (int.TryParse(stringValue, out var intValue))
                {
                    return _createInstance(intValue);
                }
            }

            throw new JsonSerializationException($"Cannot convert value to {typeof(TId).Name}");
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, TId value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.Value);
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
    /// JSON converter for long-based strongly-typed entity IDs (Newtonsoft.Json).
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    public class LongEntityIdNewtonsoftConverter<TId> : JsonConverter<TId>
        where TId : LongEntityId<TId>
    {
        private static readonly Func<long, TId> _createInstance = CreateFactory();

        /// <inheritdoc />
        public override TId ReadJson(JsonReader reader, Type objectType, TId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.Integer)
            {
                return _createInstance(Convert.ToInt64(reader.Value));
            }

            if (reader.TokenType == JsonToken.String)
            {
                var stringValue = reader.Value?.ToString();
                if (long.TryParse(stringValue, out var longValue))
                {
                    return _createInstance(longValue);
                }
            }

            throw new JsonSerializationException($"Cannot convert value to {typeof(TId).Name}");
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, TId value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.Value);
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
    /// JSON converter for string-based strongly-typed entity IDs (Newtonsoft.Json).
    /// </summary>
    /// <typeparam name="TId">The strongly-typed ID type.</typeparam>
    public class StringEntityIdNewtonsoftConverter<TId> : JsonConverter<TId>
        where TId : StringEntityId<TId>
    {
        private static readonly Func<string, TId> _createInstance = CreateFactory();

        /// <inheritdoc />
        public override TId ReadJson(JsonReader reader, Type objectType, TId existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonToken.String)
            {
                var stringValue = reader.Value?.ToString();
                return _createInstance(stringValue);
            }

            throw new JsonSerializationException($"Cannot convert value to {typeof(TId).Name}");
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, TId value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteValue(value.Value);
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
    /// Generic JSON converter that handles all strongly-typed entity IDs (Newtonsoft.Json).
    /// </summary>
    /// <remarks>
    /// This converter automatically detects the type of entity ID and converts it appropriately.
    /// Use this converter when you want to handle multiple types of entity IDs without
    /// registering individual converters.
    /// </remarks>
    /// <example>
    /// <code>
    /// JsonConvert.DefaultSettings = () => new JsonSerializerSettings
    /// {
    ///     Converters = new List&lt;JsonConverter&gt;
    ///     {
    ///         new EntityIdNewtonsoftConverter()
    ///     }
    /// };
    /// </code>
    /// </example>
    public class EntityIdNewtonsoftConverter : JsonConverter
    {
        /// <inheritdoc />
        public override bool CanConvert(Type objectType)
        {
            if (objectType == null) return false;

            return IsGuidEntityId(objectType) ||
                   IsIntEntityId(objectType) ||
                   IsLongEntityId(objectType) ||
                   IsStringEntityId(objectType);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            if (IsGuidEntityId(objectType))
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var stringValue = reader.Value?.ToString();
                    if (Guid.TryParse(stringValue, out var guid))
                    {
                        return CreateInstance(objectType, guid);
                    }
                }
            }
            else if (IsIntEntityId(objectType))
            {
                if (reader.TokenType == JsonToken.Integer)
                {
                    return CreateInstance(objectType, Convert.ToInt32(reader.Value));
                }
                if (reader.TokenType == JsonToken.String && int.TryParse(reader.Value?.ToString(), out var intValue))
                {
                    return CreateInstance(objectType, intValue);
                }
            }
            else if (IsLongEntityId(objectType))
            {
                if (reader.TokenType == JsonToken.Integer)
                {
                    return CreateInstance(objectType, Convert.ToInt64(reader.Value));
                }
                if (reader.TokenType == JsonToken.String && long.TryParse(reader.Value?.ToString(), out var longValue))
                {
                    return CreateInstance(objectType, longValue);
                }
            }
            else if (IsStringEntityId(objectType))
            {
                if (reader.TokenType == JsonToken.String)
                {
                    return CreateInstance(objectType, reader.Value?.ToString());
                }
            }

            throw new JsonSerializationException($"Cannot convert value to {objectType.Name}");
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var type = value.GetType();
            var valueProperty = GetValueProperty(type);
            var underlyingValue = valueProperty?.GetValue(value);

            if (underlyingValue == null)
            {
                writer.WriteNull();
            }
            else if (underlyingValue is Guid guid)
            {
                writer.WriteValue(guid.ToString());
            }
            else if (underlyingValue is int intValue)
            {
                writer.WriteValue(intValue);
            }
            else if (underlyingValue is long longValue)
            {
                writer.WriteValue(longValue);
            }
            else if (underlyingValue is string stringValue)
            {
                writer.WriteValue(stringValue);
            }
            else
            {
                writer.WriteValue(underlyingValue.ToString());
            }
        }

        private static object CreateInstance(Type type, object value)
        {
            var valueType = value.GetType();
            var ctor = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                new[] { valueType },
                null);

            if (ctor == null)
            {
                throw new InvalidOperationException(
                    $"Type {type.Name} must have a constructor that accepts a {valueType.Name} parameter.");
            }

            return ctor.Invoke(new[] { value });
        }

        private static PropertyInfo GetValueProperty(Type type)
        {
            return type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
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

