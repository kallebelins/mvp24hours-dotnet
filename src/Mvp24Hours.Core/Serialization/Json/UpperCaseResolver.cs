//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Mvp24Hours.Core.Serialization.Json
{
    /// <summary>
    /// <see href="https://github.com/ssukhpinder/SerializationContractResolverExample/blob/master/SerializationContractResolverExample/ContractResolver/UpperCaseResolver.cs"/>
    /// </summary>
    /// <remarks>
    /// <code>
    /// CustomerInfo customerInfo = new CustomerInfo()
    /// {
    /// 	FirstName = "Sukhpinder",
    /// 	LastName = "Singh",
    /// 	MobileNumbers = new System.Collections.Generic.List<string>() {
    /// 	 "33443434343"
    /// 	}
    /// };
    /// var responseDefaultResolver = JsonConvert.SerializeObject(customerInfo, Formatting.Indented);
    /// var responseUpperCase = JsonConvert.SerializeObject(customerInfo, Formatting.Indented, new JsonSerializerSettings()
    /// {
    /// 	ContractResolver = new UpperCaseResolver<CustomerInfo>()
    /// });
    /// </code>
    /// </remarks>
    public class UpperCaseResolver<T> : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            if (property.DeclaringType == typeof(T))
            {
                property.PropertyName = property.PropertyName.ToUpper();
            }
            return property;
        }
    }
}
