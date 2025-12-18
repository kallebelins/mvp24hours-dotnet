//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Http.Contract;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Serializers
{
    /// <summary>
    /// JSON serializer for HTTP content using System.Text.Json.
    /// </summary>
    public class JsonHttpClientSerializer : IHttpContentSerializer
    {
        private readonly JsonSerializerOptions _options;

        /// <summary>
        /// Initializes a new instance with default JSON options.
        /// </summary>
        public JsonHttpClientSerializer()
            : this(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false
            })
        {
        }

        /// <summary>
        /// Initializes a new instance with custom JSON options.
        /// </summary>
        public JsonHttpClientSerializer(JsonSerializerOptions options)
        {
            _options = options ?? new JsonSerializerOptions();
        }

        /// <inheritdoc />
        public string MediaType => "application/json";

        /// <inheritdoc />
        public HttpContent Serialize(object? value)
        {
            if (value == null)
            {
                return new StringContent(string.Empty, Encoding.UTF8, MediaType);
            }

            var json = JsonSerializer.Serialize(value, _options);
            return new StringContent(json, Encoding.UTF8, MediaType);
        }

        /// <inheritdoc />
        public async Task<T?> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken = default) where T : class
        {
            var json = await content.ReadAsStringAsync(cancellationToken);
            return Deserialize<T>(json);
        }

        /// <inheritdoc />
        public T? Deserialize<T>(string content) where T : class
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(content, _options);
        }
    }
}

