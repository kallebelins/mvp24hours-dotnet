//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Newtonsoft.Json;
using System;

namespace Mvp24Hours.Core.Converters
{
    /// <summary>
    /// Concrete object interface converter.
    /// </summary>
    /// <remarks>
    /// <code>
    /// jsonSerializerSettings.Converters.Add(new ValueObjectConverter<IPagingResult<T>, PagingResult<T>>());
    /// </code>
    /// </remarks>
    /// <typeparam name="TInterface">Interface</typeparam>
    /// <typeparam name="TConcrete">Class</typeparam>
    public class ValueObjectConverter<TInterface, TConcrete> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TInterface);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return serializer.Deserialize(reader, typeof(TConcrete));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value, typeof(TConcrete));
        }
    }
}
