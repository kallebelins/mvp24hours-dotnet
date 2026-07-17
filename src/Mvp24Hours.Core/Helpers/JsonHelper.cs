using System;
using System.Collections.Generic;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Converters;
using Mvp24Hours.Core.ValueObjects.Logic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Mvp24Hours.Helpers
{
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

        /// <summary>
        /// 
        /// </summary>
        public static JsonSerializerSettings JsonPagingResultSettings<T>(JsonSerializerSettings? jsonSerializerSettings = null)
        {
            JsonSerializerSettings settings = jsonSerializerSettings ?? JsonDefaultSettings;
            settings.Converters.Add(new ValueObjectConverter<IPagingResult<T>, PagingResult<T>>());
            settings.Converters.Add(new ValueObjectConverter<IPageResult, PageResult>());
            settings.Converters.Add(new ValueObjectConverter<ISummaryResult, SummaryResult>());
            settings.Converters.Add(new ValueObjectConverter<IMessageResult, MessageResult>());
            return settings;
        }

        /// <summary>
        /// 
        /// </summary>
        public static JsonSerializerSettings JsonBusinessResultSettings<T>(JsonSerializerSettings? jsonSerializerSettings = null)
        {
            JsonSerializerSettings settings = jsonSerializerSettings ?? JsonDefaultSettings;
            settings.Converters.Add(new ValueObjectConverter<IBusinessResult<T>, BusinessResult<T>>());
            settings.Converters.Add(new ValueObjectConverter<ISummaryResult, SummaryResult>());
            settings.Converters.Add(new ValueObjectConverter<IMessageResult, MessageResult>());
            return settings;
        }

        /// <summary>
        /// 
        /// </summary>
        public static JsonSerializerSettings JsonBusinessEventSettings(JsonSerializerSettings? jsonSerializerSettings = null)
        {
            JsonSerializerSettings settings = jsonSerializerSettings ?? JsonDefaultSettings;
            settings.Converters.Add(new ValueObjectConverter<IBusinessEvent, BusinessEvent>());
            return settings;
        }
    }
}
