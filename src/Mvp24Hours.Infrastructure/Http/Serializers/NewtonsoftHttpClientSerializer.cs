//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text;
using Mvp24Hours.Infrastructure.Http.Contract;
using Newtonsoft.Json;

namespace Mvp24Hours.Infrastructure.Http.Serializers;

/// <summary>
/// JSON serializer for HTTP content using Newtonsoft.Json.
/// </summary>
/// <remarks>
/// Initializes a new instance with custom JSON settings.
/// </remarks>
public class NewtonsoftHttpClientSerializer(JsonSerializerSettings settings) : IHttpContentSerializer
{
    private readonly JsonSerializerSettings _settings = settings ?? new JsonSerializerSettings();

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

    /// <inheritdoc />
    public string MediaType => "application/json";

    /// <inheritdoc />
    public HttpContent Serialize(object? value)
    {
        if (value == null)
        {
            return new StringContent(string.Empty, Encoding.UTF8, MediaType);
        }

        string json = JsonConvert.SerializeObject(value, _settings);
        return new StringContent(json, Encoding.UTF8, MediaType);
    }

    /// <inheritdoc />
    public async Task<T?> DeserializeAsync<T>(HttpContent content, CancellationToken cancellationToken = default) where T : class
    {
        string json = await content.ReadAsStringAsync(cancellationToken);
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

