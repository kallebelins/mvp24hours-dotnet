//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Mvp24Hours.Application.Contract.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mvp24Hours.Application.Logic.Cache
{
    /// <summary>
    /// Default implementation of <see cref="IQueryCacheKeyGenerator"/>.
    /// Generates deterministic cache keys for query caching.
    /// </summary>
    public class QueryCacheKeyGenerator : IQueryCacheKeyGenerator
    {
        private const string KeySeparator = ":";
        private const string ParameterSeparator = "_";
        private const string WildcardPattern = "*";

        /// <inheritdoc/>
        public string GenerateKey<TQuery>(TQuery query) where TQuery : ICacheableQuery
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return query.GetCacheKey();
        }

        /// <inheritdoc/>
        public string GenerateKey(MethodInfo method, object?[] parameters, Type entityType)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            var keyBuilder = new StringBuilder();

            // Start with entity type name
            keyBuilder.Append(entityType.Name);
            keyBuilder.Append(KeySeparator);

            // Add method name
            keyBuilder.Append(method.Name);

            // Add parameter hash if any parameters exist
            if (parameters != null && parameters.Length > 0)
            {
                keyBuilder.Append(KeySeparator);
                keyBuilder.Append(GenerateParameterHash(method.GetParameters(), parameters));
            }

            return keyBuilder.ToString();
        }

        /// <inheritdoc/>
        public string GenerateKeyFromTemplate(string template, IDictionary<string, object?> parameters)
        {
            if (string.IsNullOrWhiteSpace(template))
            {
                throw new ArgumentException("Template cannot be null or empty.", nameof(template));
            }

            var result = template;

            foreach (var (key, value) in parameters)
            {
                var placeholder = $"{{{key}}}";
                var replacement = value?.ToString() ?? "null";
                result = result.Replace(placeholder, replacement);
            }

            // Also support positional placeholders like {0}, {1}, etc.
            var positionalValues = parameters.Values.ToArray();
            for (var i = 0; i < positionalValues.Length; i++)
            {
                var placeholder = $"{{{i}}}";
                var replacement = positionalValues[i]?.ToString() ?? "null";
                result = result.Replace(placeholder, replacement);
            }

            return result;
        }

        /// <inheritdoc/>
        public string GenerateRegionKey<TEntity>()
        {
            return GenerateRegionKey(typeof(TEntity));
        }

        /// <inheritdoc/>
        public string GenerateRegionKey(Type entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            return $"region{KeySeparator}{entityType.Name}";
        }

        /// <inheritdoc/>
        public string GenerateInvalidationPattern(Type entityType, string? operation = null)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException(nameof(entityType));
            }

            var pattern = entityType.Name;

            if (!string.IsNullOrEmpty(operation))
            {
                pattern = $"{pattern}{KeySeparator}{operation}";
            }

            return $"{pattern}{KeySeparator}{WildcardPattern}";
        }

        /// <summary>
        /// Generates a hash from method parameters for cache key uniqueness.
        /// </summary>
        private static string GenerateParameterHash(ParameterInfo[] parameterInfos, object?[] parameterValues)
        {
            if (parameterValues.Length == 0)
            {
                return string.Empty;
            }

            var keyParts = new List<string>();

            for (var i = 0; i < parameterValues.Length && i < parameterInfos.Length; i++)
            {
                var paramInfo = parameterInfos[i];
                var paramValue = parameterValues[i];

                var valueStr = SerializeValue(paramValue);
                keyParts.Add($"{paramInfo.Name}={valueStr}");
            }

            var combinedParams = string.Join(ParameterSeparator, keyParts);

            // If the combined params are too long, hash them
            if (combinedParams.Length > 200)
            {
                return ComputeHash(combinedParams);
            }

            return combinedParams;
        }

        /// <summary>
        /// Serializes a parameter value to a string representation.
        /// </summary>
        private static string SerializeValue(object? value)
        {
            if (value == null)
            {
                return "null";
            }

            var type = value.GetType();

            // Handle primitive types directly
            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                type == typeof(Guid) || type == typeof(DateOnly) || type == typeof(TimeOnly))
            {
                return value.ToString() ?? "null";
            }

            // Handle collections
            if (value is System.Collections.IEnumerable enumerable and not string)
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                {
                    items.Add(SerializeValue(item));
                }
                return $"[{string.Join(",", items)}]";
            }

            // For complex objects, use JSON serialization and hash
            try
            {
                var json = JsonSerializer.Serialize(value, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                return ComputeHash(json);
            }
            catch
            {
                // Fallback to type name and hash code
                return $"{type.Name}_{value.GetHashCode()}";
            }
        }

        /// <summary>
        /// Computes a SHA256 hash of the input string.
        /// </summary>
        private static string ComputeHash(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var hashBytes = SHA256.HashData(inputBytes);
            return Convert.ToHexString(hashBytes)[..16].ToLowerInvariant();
        }
    }
}

