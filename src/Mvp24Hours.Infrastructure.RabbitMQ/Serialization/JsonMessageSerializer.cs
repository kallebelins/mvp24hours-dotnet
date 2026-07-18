//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Serialization;

/// <summary>
/// JSON message serializer using System.Text.Json.
/// </summary>
/// <remarks>
/// Creates a new JSON message serializer with custom options.
/// </remarks>
/// <param name="options">The JSON serializer options.</param>
public class JsonMessageSerializer(JsonSerializerOptions options) : IMessageSerializer
{
    private readonly JsonSerializerOptions _options = options ?? CreateDefaultOptions();

    /// <summary>
    /// Creates a new JSON message serializer with default options.
    /// </summary>
    public JsonMessageSerializer() : this(CreateDefaultOptions())
    {
    }

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public byte[] Serialize<T>(T value)
    {
        string json = JsonSerializer.Serialize(value, _options);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <inheritdoc />
    public byte[] Serialize(object value, Type type)
    {
        string json = JsonSerializer.Serialize(value, type, _options);
        return Encoding.UTF8.GetBytes(json);
    }

    /// <inheritdoc />
    public T? Deserialize<T>(byte[] data)
    {
        string json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<T>(json, _options);
    }

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type)
    {
        string json = Encoding.UTF8.GetString(data);
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

