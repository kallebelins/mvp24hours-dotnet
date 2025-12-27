//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Mvp24Hours.Infrastructure.Caching.KeyGenerators
{
    /// <summary>
    /// Default implementation of ICacheKeyGenerator with prefix support and hash generation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This key generator provides:
    /// <list type="bullet">
    /// <item>Prefix-based key generation (namespace separation)</item>
    /// <item>Hash-based keys for long/complex keys</item>
    /// <item>Object-based key generation via JSON serialization</item>
    /// <item>Configurable separator</item>
    /// </list>
    /// </para>
    /// </remarks>
    public class DefaultCacheKeyGenerator : ICacheKeyGenerator
    {
        private readonly ILogger<DefaultCacheKeyGenerator>? _logger;

        /// <summary>
        /// Creates a new instance of DefaultCacheKeyGenerator.
        /// </summary>
        /// <param name="defaultPrefix">Optional default prefix for all keys.</param>
        /// <param name="separator">Separator used to join key parts (default: ":").</param>
        /// <param name="logger">Optional logger.</param>
        public DefaultCacheKeyGenerator(
            string? defaultPrefix = null,
            string separator = ":",
            ILogger<DefaultCacheKeyGenerator>? logger = null)
        {
            DefaultPrefix = defaultPrefix;
            Separator = separator ?? throw new ArgumentNullException(nameof(separator));
            _logger = logger;
        }

        /// <inheritdoc />
        public string? DefaultPrefix { get; set; }

        /// <inheritdoc />
        public string Separator { get; set; }

        /// <inheritdoc />
        public string Generate(params string[] parts)
        {
            if (parts == null || parts.Length == 0)
                throw new ArgumentException("At least one key part is required.", nameof(parts));

            var keyParts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(DefaultPrefix))
            {
                keyParts.Add(DefaultPrefix);
            }

            foreach (var part in parts)
            {
                if (!string.IsNullOrWhiteSpace(part))
                {
                    keyParts.Add(part);
                }
            }

            if (keyParts.Count == 0)
                throw new ArgumentException("No valid key parts provided.", nameof(parts));

            var key = string.Join(Separator, keyParts);
            _logger?.LogDebug("Generated cache key: {Key}", key);
            return key;
        }

        /// <inheritdoc />
        public string GenerateWithPrefix(string prefix, string key)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            return $"{prefix}{Separator}{key}";
        }

        /// <inheritdoc />
        public string GenerateHash(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key cannot be null or empty.", nameof(key));

            try
            {
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
                var hash = Convert.ToBase64String(hashBytes)
                    .Replace("+", "-")
                    .Replace("/", "_")
                    .Replace("=", "");

                var hashedKey = $"hash{Separator}{hash}";
                _logger?.LogDebug("Generated hash key: {HashedKey} from: {OriginalKey}", hashedKey, key);
                return hashedKey;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating hash for key: {Key}", key);
                throw;
            }
        }

        /// <inheritdoc />
        public string GenerateFromObject(string prefix, object obj)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            try
            {
                var json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                });

                // Use hash for object-based keys to keep them short
                var hash = GenerateHash(json);
                return GenerateWithPrefix(prefix, hash);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating key from object: {Type}", obj.GetType().Name);
                throw;
            }
        }
    }
}

