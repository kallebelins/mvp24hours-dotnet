//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Caching.Serializers
{
    /// <summary>
    /// JSON-based cache serializer using System.Text.Json.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This serializer uses System.Text.Json for high-performance JSON serialization.
    /// It's the default serializer and provides good balance between performance and readability.
    /// </para>
    /// </remarks>
    public class JsonCacheSerializer : ICacheSerializer
    {
        private readonly JsonSerializerOptions _options;
        private readonly ILogger<JsonCacheSerializer>? _logger;

        /// <summary>
        /// Creates a new instance of JsonCacheSerializer.
        /// </summary>
        /// <param name="options">Optional JSON serializer options.</param>
        /// <param name="logger">Optional logger.</param>
        public JsonCacheSerializer(JsonSerializerOptions? options = null, ILogger<JsonCacheSerializer>? logger = null)
        {
            _options = options ?? new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            _logger = logger;
        }

        /// <inheritdoc />
        public Task<byte[]> SerializeAsync<T>(T value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                var json = JsonSerializer.Serialize(value, _options);
                var bytes = Encoding.UTF8.GetBytes(json);
                return Task.FromResult(bytes);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error serializing {Type} to JSON", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<T?> DeserializeAsync<T>(byte[] bytes, CancellationToken cancellationToken = default)
        {
            if (bytes == null || bytes.Length == 0)
                return Task.FromResult<T?>(default);

            try
            {
                var json = Encoding.UTF8.GetString(bytes);
                var value = JsonSerializer.Deserialize<T>(json, _options);
                return Task.FromResult(value);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deserializing JSON to {Type}", typeof(T).Name);
                return Task.FromResult<T?>(default);
            }
        }

        /// <inheritdoc />
        public Task<string> SerializeToStringAsync<T>(T value, CancellationToken cancellationToken = default)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            try
            {
                var json = JsonSerializer.Serialize(value, _options);
                return Task.FromResult(json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error serializing {Type} to JSON string", typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc />
        public Task<T?> DeserializeFromStringAsync<T>(string value, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Task.FromResult<T?>(default);

            try
            {
                var result = JsonSerializer.Deserialize<T>(value, _options);
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deserializing JSON string to {Type}", typeof(T).Name);
                return Task.FromResult<T?>(default);
            }
        }
    }
}

