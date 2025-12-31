//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.DTOs;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.ValueObjects.Logic;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mvp24Hours.Core.Serialization.SourceGeneration;

/// <summary>
/// Source-generated JSON serializer context for AOT-friendly serialization.
/// This context provides high-performance serialization without runtime reflection.
/// </summary>
/// <remarks>
/// <para>
/// Using source-generated serialization provides:
/// <list type="bullet">
/// <item><description>Better performance (no reflection at runtime)</description></item>
/// <item><description>Native AOT compatibility</description></item>
/// <item><description>Smaller application size (trimming friendly)</description></item>
/// <item><description>Faster startup time</description></item>
/// </list>
/// </para>
/// <para>
/// For custom types, create your own JsonSerializerContext and include
/// <c>[JsonSerializable(typeof(YourType))]</c> attributes.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Use the source-generated options
/// var json = JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.Default.BusinessResultOfString);
/// 
/// // Or with the default options
/// var json = JsonSerializer.Serialize(result, Mvp24HoursJsonSerializerContext.Default.Options);
/// 
/// // For DI configuration
/// services.AddMvp24HoursJsonSourceGeneration();
/// </code>
/// </example>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
// Core Value Objects
[JsonSerializable(typeof(MessageResult))]
[JsonSerializable(typeof(IMessageResult))]
[JsonSerializable(typeof(List<MessageResult>))]
[JsonSerializable(typeof(IReadOnlyCollection<IMessageResult>))]
[JsonSerializable(typeof(MessageType))]
[JsonSerializable(typeof(SummaryResult))]
[JsonSerializable(typeof(ISummaryResult))]
[JsonSerializable(typeof(PageResult))]
[JsonSerializable(typeof(IPageResult))]
// BusinessResult variations
[JsonSerializable(typeof(BusinessResult<object>))]
[JsonSerializable(typeof(BusinessResult<string>))]
[JsonSerializable(typeof(BusinessResult<int>))]
[JsonSerializable(typeof(BusinessResult<long>))]
[JsonSerializable(typeof(BusinessResult<bool>))]
[JsonSerializable(typeof(BusinessResult<Guid>))]
[JsonSerializable(typeof(BusinessResult<DateTime>))]
[JsonSerializable(typeof(BusinessResult<DateTimeOffset>))]
[JsonSerializable(typeof(BusinessResult<decimal>))]
[JsonSerializable(typeof(BusinessResult<double>))]
// Paging Results
[JsonSerializable(typeof(PagingResult<object>))]
[JsonSerializable(typeof(PagingResult<string>))]
[JsonSerializable(typeof(IPagingResult<object>))]
// Common primitive types
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(long))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(DateTime))]
[JsonSerializable(typeof(DateTimeOffset))]
[JsonSerializable(typeof(decimal))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
// Collections
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<Guid>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(object[]))]
[JsonSerializable(typeof(string[]))]
// VoidResult
[JsonSerializable(typeof(VoidResult))]
public partial class Mvp24HoursJsonSerializerContext : JsonSerializerContext
{
    /// <summary>
    /// Gets the default serializer options configured for Mvp24Hours framework.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => Default.Options;

    /// <summary>
    /// Creates a new JsonSerializerOptions instance with the source-generated type info.
    /// </summary>
    /// <returns>Configured JsonSerializerOptions for Mvp24Hours types.</returns>
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        options.TypeInfoResolverChain.Add(Default);
        return options;
    }

    /// <summary>
    /// Creates options that combine the source-generated context with custom converters.
    /// </summary>
    /// <param name="additionalConverters">Custom converters to add.</param>
    /// <returns>Configured JsonSerializerOptions.</returns>
    public static JsonSerializerOptions CreateOptionsWithConverters(params JsonConverter[] additionalConverters)
    {
        var options = CreateOptions();
        foreach (var converter in additionalConverters)
        {
            options.Converters.Add(converter);
        }
        return options;
    }
}

