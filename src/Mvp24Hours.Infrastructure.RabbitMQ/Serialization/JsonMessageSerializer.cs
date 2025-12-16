//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Serialization
{
    /// <summary>
    /// JSON message serializer using System.Text.Json.
    /// </summary>
    public class JsonMessageSerializer : IMessageSerializer
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Creates a new JSON message serializer with default options.
        /// </summary>
        public JsonMessageSerializer() : this(CreateDefaultOptions())
        {
        }

        /// <summary>
        /// Creates a new JSON message serializer with custom options.
        /// </summary>
        /// <param name="options">The JSON serializer options.</param>
        public JsonMessageSerializer(JsonSerializerOptions options)
        {
            _options = options ?? CreateDefaultOptions();
        }

        /// <inheritdoc />
        public string ContentType => "application/json";

        /// <inheritdoc />
        public byte[] Serialize<T>(T value)
        {
            var json = JsonSerializer.Serialize(value, _options);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <inheritdoc />
        public byte[] Serialize(object value, Type type)
        {
            var json = JsonSerializer.Serialize(value, type, _options);
            return Encoding.UTF8.GetBytes(json);
        }

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(json, _options);
        }

        /// <inheritdoc />
        public object? Deserialize(byte[] data, Type type)
        {
            var json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize(json, type, _options);
        }

        private static JsonSerializerOptions CreateDefaultOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Converters = { new JsonStringEnumConverter() }
            };
        }
    }
}

