//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.Http.Contract;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mvp24Hours.Infrastructure.Http.Serializers
{
    /// <summary>
    /// JSON serializer for HTTP content using Newtonsoft.Json.
    /// </summary>
    public class NewtonsoftHttpClientSerializer : IHttpClientSerializer
    {
        private readonly JsonSerializerSettings _settings;

        /// <summary>
        /// Initializes a new instance with default JSON settings.
        /// </summary>
        public NewtonsoftHttpClientSerializer()
            : this(new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            })
        {
        }

        /// <summary>
        /// Initializes a new instance with custom JSON settings.
        /// </summary>
        public NewtonsoftHttpClientSerializer(JsonSerializerSettings settings)
        {
            _settings = settings ?? new JsonSerializerSettings();
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

            var json = JsonConvert.SerializeObject(value, _settings);
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

            return JsonConvert.DeserializeObject<T>(content, _settings);
        }
    }
}

