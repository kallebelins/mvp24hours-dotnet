//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mvp24Hours.Core.Serialization.Json
{
    /// <summary>
    /// Serializes all properties and fields (members) of the class and subclasses.
    /// <see href="https://www.ti-enxame.com/pt/c%23/json.net-forca-serializacao-de-todos-os-campos-particulares-e-todos-os-campos-nas-subclasses/1047235330/"/>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var settings = new JsonSerializerSettings() { ContractResolver = new MyContractResolver() };
    /// var json = JsonConvert.SerializeObject(obj, settings);
    /// </code>
    /// </remarks>
    public class PropertyAndFieldsSerializerResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        protected override IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(Type type, Newtonsoft.Json.MemberSerialization memberSerialization)
        {
            var props = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => base.CreateProperty(p, memberSerialization))
                .Union(type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Select(f => base.CreateProperty(f, memberSerialization)))
                .ToList();
            props.ForEach(p => { p.Writable = true; p.Readable = true; });
            return props;
        }
    }
}
