//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Mvp24Hours.Core.Serialization.Json
{
    /// <summary>
    /// Dynamically rename or ignore properties without changing the serialized class
    /// <see href="https://blog.rsuter.com/advanced-newtonsoft-json-dynamically-rename-or-ignore-properties-without-changing-the-serialized-class/"/>
    /// </summary>
    /// <remarks>
    /// <code>
    /// var person = new Person();
    /// var jsonResolver = new PropertyRenameAndIgnoreSerializerContractResolver();
    /// jsonResolver.IgnoreProperty(typeof(Person), "Title");
    /// jsonResolver.RenameProperty(typeof(Person), "FirstName", "firstName");        
    /// var serializerSettings = new JsonSerializerSettings();
    /// serializerSettings.ContractResolver = jsonResolver;        
    /// var json = JsonConvert.SerializeObject(person, serializerSettings);
    /// </code>
    /// </remarks>
    public class PropertyRenameAndIgnoreSerializerContractResolver : DefaultContractResolver
    {
        private readonly Dictionary<Type, HashSet<string>> _ignores;
        private readonly Dictionary<Type, Dictionary<string, string>> _renames;

        public PropertyRenameAndIgnoreSerializerContractResolver()
        {
            _ignores = [];
            _renames = [];
        }

        public void IgnoreProperty(Type type, params string[] jsonPropertyNames)
        {
            if (!_ignores.TryGetValue(type, out HashSet<string> value))
            {
                value = [];
                _ignores[type] = value;
            }

            foreach (var prop in jsonPropertyNames)
            {
                value.Add(prop);
            }
        }

        public void RenameProperty(Type type, string propertyName, string newJsonPropertyName)
        {
            if (!_renames.TryGetValue(type, out Dictionary<string, string> value))
            {
                value = [];
                _renames[type] = value;
            }

            value[propertyName] = newJsonPropertyName;
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (IsIgnored(property.DeclaringType, property.PropertyName))
            {
                property.ShouldSerialize = i => false;
                property.Ignored = true;
            }
            if (IsRenamed(property.DeclaringType, property.PropertyName, out var newJsonPropertyName))
            {
                property.PropertyName = newJsonPropertyName;
            }

            return property;
        }

        private bool IsIgnored(Type type, string jsonPropertyName)
        {
            if (!_ignores.TryGetValue(type, out HashSet<string> value))
            {
                return false;
            }

            return value.Contains(jsonPropertyName);
        }

        private bool IsRenamed(Type type, string jsonPropertyName, out string newJsonPropertyName)
        {
            if (!_renames.TryGetValue(type, out Dictionary<string, string> renames) || !renames.TryGetValue(jsonPropertyName, out newJsonPropertyName))
            {
                newJsonPropertyName = null;
                return false;
            }
            return true;
        }
    }
}
