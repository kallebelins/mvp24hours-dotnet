//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;

namespace Mvp24Hours.Infrastructure.Caching.Invalidation;

/// <summary>
/// Manages cache tags for invalidation by group using the cache provider itself.
/// </summary>
/// <remarks>
/// <para>
/// This implementation stores tag-to-key mappings in the cache itself using a naming convention.
/// Each tag has a set of keys associated with it, stored as a separate cache entry.
/// </para>
/// </remarks>
/// <remarks>
/// Creates a new instance of CacheTagManager.
/// </remarks>
/// <param name="cacheProvider">The cache provider to use for storing tag mappings.</param>
/// <param name="logger">Optional logger.</param>
public class CacheTagManager(ICacheProvider cacheProvider, ILogger<CacheTagManager>? logger = null) : ICacheTagManager
{
    private const string TagPrefix = "mvp24hours:cache:tag:";
    private const string KeyPrefix = "mvp24hours:cache:key:";

    private readonly ICacheProvider _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
    private readonly ILogger<CacheTagManager>? _logger = logger;

    /// <inheritdoc />
    public async Task TagKeyAsync(string key, IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        if (tags == null)
        {
            throw new ArgumentNullException(nameof(tags));
        }

        var tagList = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (tagList.Count == 0)
        {
            return;
        }

        try
        {
            // For each tag, add the key to the tag's key set
            foreach (string? tag in tagList)
            {
                string tagKey = GetTagKey(tag);
                HashSet<string> keySet = await GetKeySetAsync(tagKey, cancellationToken);
                if (!keySet.Contains(key))
                {
                    keySet.Add(key);
                    await SaveKeySetAsync(tagKey, keySet, cancellationToken);
                }
            }

            // Store tags for the key (for reverse lookup)
            string keyTagKey = GetKeyTagKey(key);
            List<string> keyTags = await GetKeyTagsAsync(keyTagKey, cancellationToken);
            foreach (string? tag in tagList)
            {
                if (!keyTags.Contains(tag))
                {
                    keyTags.Add(tag);
                }
            }
            await SaveKeyTagsAsync(keyTagKey, keyTags, cancellationToken);

            _logger?.LogDebug("Tagged key {Key} with tags: {Tags}", key, string.Join(", ", tagList));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error tagging key {Key} with tags", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return [];
        }

        try
        {
            string tagKey = GetTagKey(tag);
            HashSet<string> keySet = await GetKeySetAsync(tagKey, cancellationToken);
            return keySet;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting keys for tag {Tag}", tag);
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<int> InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return 0;
        }

        try
        {
            string tagKey = GetTagKey(tag);
            HashSet<string> keySet = await GetKeySetAsync(tagKey, cancellationToken);

            if (keySet.Count == 0)
            {
                return 0;
            }

            // Remove all keys associated with the tag
            string[] keys = [.. keySet];
            await _cacheProvider.RemoveManyAsync(keys, cancellationToken);

            // Remove tag associations from each key
            foreach (string? key in keys)
            {
                string keyTagKey = GetKeyTagKey(key);
                List<string> keyTags = await GetKeyTagsAsync(keyTagKey, cancellationToken);
                keyTags.Remove(tag);
                if (keyTags.Count == 0)
                {
                    await _cacheProvider.RemoveAsync(keyTagKey, cancellationToken);
                }
                else
                {
                    await SaveKeyTagsAsync(keyTagKey, keyTags, cancellationToken);
                }
            }

            // Clear the tag's key set
            await _cacheProvider.RemoveAsync(tagKey, cancellationToken);

            _logger?.LogInformation("Invalidated {Count} keys for tag {Tag}", keys.Length, tag);
            return keys.Length;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invalidating tag {Tag}", tag);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> InvalidateByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        if (tags == null)
        {
            return 0;
        }

        var tagList = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (tagList.Count == 0)
        {
            return 0;
        }

        int totalInvalidated = 0;
        var invalidatedKeys = new HashSet<string>();

        try
        {
            // Collect all unique keys from all tags
            foreach (string? tag in tagList)
            {
                IEnumerable<string> keys = await GetKeysByTagAsync(tag, cancellationToken);
                foreach (string key in keys)
                {
                    invalidatedKeys.Add(key);
                }
            }

            if (invalidatedKeys.Count == 0)
            {
                return 0;
            }

            // Remove all unique keys
            string[] keysArray = [.. invalidatedKeys];
            await _cacheProvider.RemoveManyAsync(keysArray, cancellationToken);

            // Clean up tag associations
            foreach (string? key in keysArray)
            {
                string keyTagKey = GetKeyTagKey(key);
                List<string> keyTags = await GetKeyTagsAsync(keyTagKey, cancellationToken);
                foreach (string? tag in tagList)
                {
                    keyTags.Remove(tag);
                }

                if (keyTags.Count == 0)
                {
                    await _cacheProvider.RemoveAsync(keyTagKey, cancellationToken);
                }
                else
                {
                    await SaveKeyTagsAsync(keyTagKey, keyTags, cancellationToken);
                }
            }

            // Clear tag key sets
            foreach (string? tag in tagList)
            {
                string tagKey = GetTagKey(tag);
                await _cacheProvider.RemoveAsync(tagKey, cancellationToken);
            }

            totalInvalidated = invalidatedKeys.Count;
            _logger?.LogInformation("Invalidated {Count} keys for {TagCount} tags", totalInvalidated, tagList.Count);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invalidating tags");
            throw;
        }

        return totalInvalidated;
    }

    /// <inheritdoc />
    public async Task RemoveTagsAsync(string key, IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        if (tags == null)
        {
            throw new ArgumentNullException(nameof(tags));
        }

        var tagList = tags.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
        if (tagList.Count == 0)
        {
            return;
        }

        try
        {
            string keyTagKey = GetKeyTagKey(key);
            List<string> keyTags = await GetKeyTagsAsync(keyTagKey, cancellationToken);

            foreach (string? tag in tagList)
            {
                keyTags.Remove(tag);

                // Remove key from tag's key set
                string tagKey = GetTagKey(tag);
                HashSet<string> keySet = await GetKeySetAsync(tagKey, cancellationToken);
                keySet.Remove(key);
                if (keySet.Count == 0)
                {
                    await _cacheProvider.RemoveAsync(tagKey, cancellationToken);
                }
                else
                {
                    await SaveKeySetAsync(tagKey, keySet, cancellationToken);
                }
            }

            if (keyTags.Count == 0)
            {
                await _cacheProvider.RemoveAsync(keyTagKey, cancellationToken);
            }
            else
            {
                await SaveKeyTagsAsync(keyTagKey, keyTags, cancellationToken);
            }

            _logger?.LogDebug("Removed tags from key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing tags from key {Key}", key);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task RemoveAllTagsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        try
        {
            string keyTagKey = GetKeyTagKey(key);
            List<string> keyTags = await GetKeyTagsAsync(keyTagKey, cancellationToken);

            // Remove key from all tag key sets
            foreach (string tag in keyTags)
            {
                string tagKey = GetTagKey(tag);
                HashSet<string> keySet = await GetKeySetAsync(tagKey, cancellationToken);
                keySet.Remove(key);
                if (keySet.Count == 0)
                {
                    await _cacheProvider.RemoveAsync(tagKey, cancellationToken);
                }
                else
                {
                    await SaveKeySetAsync(tagKey, keySet, cancellationToken);
                }
            }

            // Remove key's tag associations
            await _cacheProvider.RemoveAsync(keyTagKey, cancellationToken);

            _logger?.LogDebug("Removed all tags from key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error removing all tags from key {Key}", key);
            throw;
        }
    }

    private string GetTagKey(string tag)
    {
        return $"{TagPrefix}{tag}";
    }

    private string GetKeyTagKey(string key)
    {
        return $"{KeyPrefix}{key}";
    }

    private async Task<HashSet<string>> GetKeySetAsync(string tagKey, CancellationToken cancellationToken)
    {
        HashSet<string>? keySet = await _cacheProvider.GetAsync<HashSet<string>>(tagKey, cancellationToken);
        return keySet ?? [];
    }

    private async Task SaveKeySetAsync(string tagKey, HashSet<string> keySet, CancellationToken cancellationToken)
    {
        // Store with no expiration (or very long expiration) since it's metadata
        var options = CacheEntryOptions.FromDuration(TimeSpan.FromDays(365));
        await _cacheProvider.SetAsync(tagKey, keySet, options, cancellationToken);
    }

    private async Task<List<string>> GetKeyTagsAsync(string keyTagKey, CancellationToken cancellationToken)
    {
        List<string>? tags = await _cacheProvider.GetAsync<List<string>>(keyTagKey, cancellationToken);
        return tags ?? [];
    }

    private async Task SaveKeyTagsAsync(string keyTagKey, List<string> tags, CancellationToken cancellationToken)
    {
        // Store with no expiration (or very long expiration) since it's metadata
        var options = CacheEntryOptions.FromDuration(TimeSpan.FromDays(365));
        await _cacheProvider.SetAsync(keyTagKey, tags, options, cancellationToken);
    }
}

