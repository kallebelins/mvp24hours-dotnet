//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Infrastructure.RabbitMQ.Core;
using Mvp24Hours.Infrastructure.RabbitMQ.Core.Contract;
using Mvp24Hours.Infrastructure.RabbitMQ.Observability;
using Mvp24Hours.Infrastructure.RabbitMQ.Pipeline;
using Mvp24Hours.Infrastructure.RabbitMQ.RequestResponse;
using Mvp24Hours.Infrastructure.RabbitMQ.Scheduling;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Infrastructure.RabbitMQ.Serialization;

/// <summary>
/// Source-generated JSON serializer context for RabbitMQ module types.
/// Provides AOT-friendly serialization for messages, requests, and scheduling data.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString)]
// Message types
[JsonSerializable(typeof(MessageEnvelope))]
[JsonSerializable(typeof(Dictionary<string, object>))] // MessageHeaders is Dictionary<string, object>
// Request/Response
[JsonSerializable(typeof(Response<object>))]
[JsonSerializable(typeof(Response<string>))]
[JsonSerializable(typeof(Response<bool>))]
[JsonSerializable(typeof(Response<int>))]
// Scheduling
[JsonSerializable(typeof(ScheduledMessage))]
[JsonSerializable(typeof(List<ScheduledMessage>))]
// Filter contexts
[JsonSerializable(typeof(ConsumeFilterContext<object>))]
[JsonSerializable(typeof(PublishFilterContext<object>))]
// Common types
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(byte[]))]
public partial class RabbitMQJsonSerializerContext : JsonSerializerContext
{
    /// <summary>
    /// Gets the default serializer options configured for RabbitMQ module.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => Default.Options;

    /// <summary>
    /// Creates a new JsonSerializerOptions instance with the source-generated type info.
    /// </summary>
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        options.TypeInfoResolverChain.Add(Default);
        return options;
    }
}

