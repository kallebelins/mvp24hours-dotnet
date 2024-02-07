//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mvp24Hours.Extensions
{
    public static class GenerateKeyExtensions
    {
        public static string ToKey<T>(this T entity)
        {
            var result = ToHash(entity);
            return Encoding.UTF8.GetString(result);
        }

        public static byte[] ToHash<T>(T entity)
        {
            var seen = new HashSet<object>();
            var properties = GetAllSimpleProperties(entity, seen);
            return properties.Select(p => BitConverter.GetBytes(p.GetHashCode()).AsEnumerable()).Aggregate((ag, next) => ag.Concat(next)).ToArray();
        }

        private static IEnumerable<object> GetAllSimpleProperties<T>(T entity, HashSet<object> seen)
        {
            foreach (var property in PropertiesOf<T>.All(entity))
            {
                if (property is short || property is int || property is long
                    || property is float || property is double || property is decimal || property is bool
                    || property is DateTime
                    || property is short? || property is int? || property is long?
                    || property is float? || property is double? || property is decimal? || property is bool?
                    || property is DateTime?
                    || property is string)
                {
                    yield return property;
                }
                else if (seen.Add(property)) // Handle cyclic references
                {
                    foreach (var simple in GetAllSimpleProperties(property, seen))
                    {
                        yield return simple;
                    }
                }
            }
        }

        private static class PropertiesOf<T>
        {
            private static readonly List<Func<T, dynamic>> Properties = [];

            static PropertiesOf()
            {
                foreach (var property in typeof(T).GetProperties())
                {
                    var getMethod = property.GetGetMethod();
                    var function = (Func<T, dynamic>)Delegate.CreateDelegate(typeof(Func<T, dynamic>), getMethod);
                    Properties.Add(function);
                }
            }

            public static IEnumerable<dynamic> All(T entity)
            {
                return Properties.Select(p => p(entity)).Where(v => v != null);
            }
        }
    }
}
