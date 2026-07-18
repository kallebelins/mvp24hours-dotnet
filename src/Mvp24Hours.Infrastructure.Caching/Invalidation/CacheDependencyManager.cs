//=====================================================================================
// Developed by Kallebe Lins (https://github.com/kallebelins)
//=====================================================================================
// Reproduction or sharing is free! Contribute to a better world!
//=====================================================================================
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;

namespace Mvp24Hours.Infrastructure.Caching.Invalidation;

/// <summary>
/// Manages cache dependencies for invalidation by dependency tracking.
/// </summary>
/// <remarks>
/// <para>
/// When a cache entry has dependencies, it will be automatically invalidated
/// when any of its dependencies are invalidated.
/// </para>
/// </remarks>
/// <remarks>
/// Creates a new instance of CacheDependencyManager.
/// </remarks>
/// <param name="cacheProvider">The cache provider.</param>
/// <param name="logger">Optional logger.</param>
public class CacheDependencyManager(ICacheProvider cacheProvider, ILogger<CacheDependencyManager>? logger = null)
{
    private const string DependencyPrefix = "mvp24hours:cache:dependency:";

    private readonly ICacheProvider _cacheProvider = cacheProvider ?? throw new ArgumentNullException(nameof(cacheProvider));
    private readonly ILogger<CacheDependencyManager>? _logger = logger;

    /// <summary>
    /// Registers dependencies for a cache key.
    /// </summary>
    /// <param name="key">The cache key that depends on other keys.</param>
    /// <param name="dependencies">The dependency keys.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RegisterDependenciesAsync(string key, IEnumerable<string> dependencies, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        if (dependencies == null)
        {
            throw new ArgumentNullException(nameof(dependencies));
        }

        var dependencyList = dependencies.Where(d => !string.IsNullOrWhiteSpace(d)).ToList();
        if (dependencyList.Count == 0)
        {
            return;
        }

        try
        {
            // For each dependency, add the key to the dependency's dependent keys set
            foreach (string? dependency in dependencyList)
            {
                string dependencyKey = GetDependencyKey(dependency);
                HashSet<string> dependentKeys = await GetDependentKeysAsync(dependencyKey, cancellationToken);
                if (!dependentKeys.Contains(key))
                {
                    dependentKeys.Add(key);
                    await SaveDependentKeysAsync(dependencyKey, dependentKeys, cancellationToken);
                }
            }

            _logger?.LogDebug("Registered dependencies for key {Key}: {Dependencies}", key, string.Join(", ", dependencyList));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error registering dependencies for key {Key}", key);
            throw;
        }
    }

    /// <summary>
    /// Invalidates all keys that depend on the specified dependency key.
    /// </summary>
    /// <param name="dependencyKey">The dependency key that was invalidated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of dependent keys invalidated.</returns>
    public async Task<int> InvalidateDependentsAsync(string dependencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dependencyKey))
        {
            return 0;
        }

        try
        {
            string dependencyKeyName = GetDependencyKey(dependencyKey);
            HashSet<string> dependentKeys = await GetDependentKeysAsync(dependencyKeyName, cancellationToken);

            if (dependentKeys.Count == 0)
            {
                return 0;
            }

            // Remove all dependent keys
            string[] keys = [.. dependentKeys];
            await _cacheProvider.RemoveManyAsync(keys, cancellationToken);

            // Clean up dependency registrations for each dependent key
            foreach (string? key in keys)
            {
                await RemoveDependencyRegistrationAsync(key, dependencyKey, cancellationToken);
            }

            // Clear the dependency's dependent keys set
            await _cacheProvider.RemoveAsync(dependencyKeyName, cancellationToken);

            _logger?.LogInformation("Invalidated {Count} dependent keys for dependency {Dependency}", keys.Length, dependencyKey);
            return keys.Length;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error invalidating dependents for dependency {Dependency}", dependencyKey);
            throw;
        }
    }

    /// <summary>
    /// Removes dependency registrations for a key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RemoveAllDependenciesAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        // Note: We'd need to track reverse dependencies to efficiently remove all dependencies.
        // For now, this is a placeholder that could be enhanced with additional tracking.
        _logger?.LogDebug("RemoveAllDependencies called for key {Key} (not fully implemented)", key);
    }

    private string GetDependencyKey(string dependency)
    {
        return $"{DependencyPrefix}{dependency}";
    }

    private async Task<HashSet<string>> GetDependentKeysAsync(string dependencyKey, CancellationToken cancellationToken)
    {
        HashSet<string>? dependentKeys = await _cacheProvider.GetAsync<HashSet<string>>(dependencyKey, cancellationToken);
        return dependentKeys ?? [];
    }

    private async Task SaveDependentKeysAsync(string dependencyKey, HashSet<string> dependentKeys, CancellationToken cancellationToken)
    {
        // Store with no expiration (or very long expiration) since it's metadata
        var options = CacheEntryOptions.FromDuration(TimeSpan.FromDays(365));
        await _cacheProvider.SetAsync(dependencyKey, dependentKeys, options, cancellationToken);
    }

    private async Task RemoveDependencyRegistrationAsync(string key, string dependency, CancellationToken cancellationToken)
    {
        // This would require reverse tracking, which is complex.
        // For now, we rely on the dependency key being removed when the dependent key is removed.
        await Task.CompletedTask;
    }
}

