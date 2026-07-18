//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;

namespace Mvp24Hours.Infrastructure.Caching.KeyGenerators;

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
/// <remarks>
/// Creates a new instance of DefaultCacheKeyGenerator.
/// </remarks>
/// <param name="defaultPrefix">Optional default prefix for all keys.</param>
/// <param name="separator">Separator used to join key parts (default: ":").</param>
/// <param name="logger">Optional logger.</param>
public class DefaultCacheKeyGenerator(
    string? defaultPrefix = null,
    string separator = ":",
    ILogger<DefaultCacheKeyGenerator>? logger = null) : ICacheKeyGenerator
{
    private readonly ILogger<DefaultCacheKeyGenerator>? _logger = logger;

    /// <inheritdoc />
    public string? DefaultPrefix { get; set; } = defaultPrefix;

    /// <inheritdoc />
    public string Separator { get; set; } = separator ?? throw new ArgumentNullException(nameof(separator));

    /// <inheritdoc />
    public string Generate(params string[] parts)
    {
        if (parts == null || parts.Length == 0)
        {
            throw new ArgumentException("At least one key part is required.", nameof(parts));
        }

        var keyParts = new System.Collections.Generic.List<string>();

        if (!string.IsNullOrWhiteSpace(DefaultPrefix))
        {
            keyParts.Add(DefaultPrefix);
        }

        foreach (string part in parts)
        {
            if (!string.IsNullOrWhiteSpace(part))
            {
                keyParts.Add(part);
            }
        }

        if (keyParts.Count == 0)
        {
            throw new ArgumentException("No valid key parts provided.", nameof(parts));
        }

        string key = string.Join(Separator, keyParts);
        _logger?.LogDebug("Generated cache key: {Key}", key);
        return key;
    }

    /// <inheritdoc />
    public string GenerateWithPrefix(string prefix, string key)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        return $"{prefix}{Separator}{key}";
    }

    /// <inheritdoc />
    public string GenerateHash(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        try
        {
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            string hash = Convert.ToBase64String(hashBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            string hashedKey = $"hash{Separator}{hash}";
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
        {
            throw new ArgumentException("Prefix cannot be null or empty.", nameof(prefix));
        }

        if (obj == null)
        {
            throw new ArgumentNullException(nameof(obj));
        }

        try
        {
            string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            // Use hash for object-based keys to keep them short
            string hash = GenerateHash(json);
            return GenerateWithPrefix(prefix, hash);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating key from object: {Type}", obj.GetType().Name);
            throw;
        }
    }
}

