//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Extensions;
using System.Dynamic;
using System.IO;
using System.Runtime.Serialization;

namespace Mvp24Hours.Helpers
{
    /// <summary>
    /// Contains functions to transform object (clone, serialize, ...)
    /// </summary>
    public static class ObjectHelper
    {
        /// <summary>
        /// Clone object instance with binary method in memory
        /// </summary>
        public static T Clone<T>(T source)
        {
            if (source == null)
            {
                return default;
            }

            DataContractSerializer dcSer = new(source.GetType());
            MemoryStream memoryStream = new();
            dcSer.WriteObject(memoryStream, source);
            memoryStream.Position = 0;
            return (T)dcSer.ReadObject(memoryStream);
        }

        /// <summary>
        /// 
        /// </summary>
        public static dynamic ConvertToDynamic(object obj)
        {
            var json = obj.ToSerialize();
            return json.ToDeserialize<ExpandoObject>();
        }
    }
}
