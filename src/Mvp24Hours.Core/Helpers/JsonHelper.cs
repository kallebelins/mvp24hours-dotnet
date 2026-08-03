using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Converters;
using Mvp24Hours.Core.ValueObjects.Logic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Mvp24Hours.Helpers;

/// <summary>
/// 
/// </summary>
public static class JsonHelper
{
    static JsonHelper()
    {
        JsonDefaultSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Converters = [new StringEnumConverter()],
            DateFormatHandling = DateFormatHandling.MicrosoftDateFormat,
            DateFormatString = "yyyy-MM-dd",
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
    }

    /// <summary>
    /// 
    /// </summary>
    public static JsonSerializerSettings JsonDefaultSettings { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public static string Serialize<T>(T? dto, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        return JsonConvert.SerializeObject(dto, jsonSerializerSettings ?? JsonDefaultSettings);
    }

    /// <summary>
    /// 
    /// </summary>
    public static T? Deserialize<T>(string value, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        return JsonConvert.DeserializeObject<T>(value, jsonSerializerSettings ?? JsonDefaultSettings);
    }

    /// <summary>
    /// 
    /// </summary>
    public static object? Deserialize(string value, Type type, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        return JsonConvert.DeserializeObject(value, type, jsonSerializerSettings ?? JsonDefaultSettings);
    }

    /// <summary>
    /// 
    /// </summary>
    public static T? Deserialize<T>(string value, params JsonConverter[] converters)
    {
        return JsonConvert.DeserializeObject<T>(value, converters);
    }

    /// <summary>
    /// 
    /// </summary>
    public static object? Deserialize(string value, Type type, params JsonConverter[] converters)
    {
        return JsonConvert.DeserializeObject(value, type, converters);
    }

    /// <summary>
    /// 
    /// </summary>
    public static T? DeserializeAnonymous<T>(string value, T anonymousType, JsonSerializerSettings? jsonSerializerSettings = null)
    {
        return JsonConvert.DeserializeAnonymousType(value, anonymousType, jsonSerializerSettings ?? JsonDefaultSettings);
    }

    private static JsonSerializerSettings CreateSettingsWithConverters(params JsonConverter[] converters)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = JsonDefaultSettings.ContractResolver,
            DateFormatHandling = JsonDefaultSettings.DateFormatHandling,
            DateFormatString = JsonDefaultSettings.DateFormatString,
            NullValueHandling = JsonDefaultSettings.NullValueHandling,
            ReferenceLoopHandling = JsonDefaultSettings.ReferenceLoopHandling
        };

        foreach (JsonConverter converter in JsonDefaultSettings.Converters)
        {
            settings.Converters.Add(converter);
        }

        foreach (JsonConverter converter in converters)
        {
            settings.Converters.Add(converter);
        }

        return settings;
    }

    /// <summary>
    /// 
    /// </summary>
    public static JsonSerializerSettings JsonPagingResultSettings<T>(JsonSerializerSettings? jsonSerializerSettings = null)
    {
        if (jsonSerializerSettings != null)
        {
            JsonSerializerSettings settings = CreateSettingsWithConverters(
                new ValueObjectConverter<IPagingResult<T>, PagingResult<T>>(),
                new ValueObjectConverter<IPageResult, PageResult>(),
                new ValueObjectConverter<ISummaryResult, SummaryResult>(),
                new ValueObjectConverter<IMessageResult, MessageResult>());
            settings.ContractResolver = jsonSerializerSettings.ContractResolver ?? settings.ContractResolver;
            settings.NullValueHandling = jsonSerializerSettings.NullValueHandling;
            return settings;
        }

        return CreateSettingsWithConverters(
            new ValueObjectConverter<IPagingResult<T>, PagingResult<T>>(),
            new ValueObjectConverter<IPageResult, PageResult>(),
            new ValueObjectConverter<ISummaryResult, SummaryResult>(),
            new ValueObjectConverter<IMessageResult, MessageResult>());
    }

    /// <summary>
    /// 
    /// </summary>
    public static JsonSerializerSettings JsonBusinessResultSettings<T>(JsonSerializerSettings? jsonSerializerSettings = null)
    {
        if (jsonSerializerSettings != null)
        {
            JsonSerializerSettings settings = CreateSettingsWithConverters(
                new ValueObjectConverter<IBusinessResult<T>, BusinessResult<T>>(),
                new ValueObjectConverter<ISummaryResult, SummaryResult>(),
                new ValueObjectConverter<IMessageResult, MessageResult>());
            settings.ContractResolver = jsonSerializerSettings.ContractResolver ?? settings.ContractResolver;
            settings.NullValueHandling = jsonSerializerSettings.NullValueHandling;
            return settings;
        }

        return CreateSettingsWithConverters(
            new ValueObjectConverter<IBusinessResult<T>, BusinessResult<T>>(),
            new ValueObjectConverter<ISummaryResult, SummaryResult>(),
            new ValueObjectConverter<IMessageResult, MessageResult>());
    }

    /// <summary>
    /// 
    /// </summary>
    public static JsonSerializerSettings JsonBusinessEventSettings(JsonSerializerSettings? jsonSerializerSettings = null)
    {
        if (jsonSerializerSettings != null)
        {
            JsonSerializerSettings settings = CreateSettingsWithConverters(
                new ValueObjectConverter<IBusinessEvent, BusinessEvent>());
            settings.ContractResolver = jsonSerializerSettings.ContractResolver ?? settings.ContractResolver;
            settings.NullValueHandling = jsonSerializerSettings.NullValueHandling;
            return settings;
        }

        return CreateSettingsWithConverters(new ValueObjectConverter<IBusinessEvent, BusinessEvent>());
    }
}
