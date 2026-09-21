//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Reflection;
using System.Text;

namespace Mvp24Hours.Extensions;

public static class GenerateKeyExtensions
{
    public static string ToKey<T>(this T entity)
    {
        byte[] result = ToHash(entity);
        return Encoding.UTF8.GetString(result);
    }

    public static byte[] ToHash<T>(T entity)
    {
        var seen = new HashSet<object>();
        IEnumerable<object> properties = GetAllSimpleProperties(entity, seen);
        return [.. properties.Select(p => BitConverter.GetBytes(p.GetHashCode()).AsEnumerable()).Aggregate((ag, next) => ag.Concat(next))];
    }

    private static IEnumerable<object> GetAllSimpleProperties<T>(T entity, HashSet<object> seen)
    {
        foreach (dynamic property in PropertiesOf<T>.All(entity))
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
                foreach (object? simple in GetAllSimpleProperties(property, seen))
                {
                    yield return simple;
                }
            }
        }
    }

    private static class PropertiesOf<T>
    {
        // Uses PropertyInfo.GetValue (rather than a compiled Func<T, dynamic> delegate) because
        // Delegate.CreateDelegate cannot bind a getter that returns a nullable value type
        // (e.g. DateTime?) to a Func<T, dynamic> (== Func<T, object>) target signature; that
        // combination throws ArgumentException, which is cached forever by the static
        // constructor (TypeInitializationException) and permanently breaks ToHash<T>/ToKey<T>
        // for that T in this process.
        private static readonly List<PropertyInfo> Properties = [.. typeof(T)
            .GetProperties()
            .Where(property => property.GetGetMethod() != null)];

        public static IEnumerable<dynamic> All(T entity)
        {
            return Properties
                .Select(p => p.GetValue(entity))
                .Where(v => v != null)
                .Select(v => (dynamic)v!);
        }
    }
}
