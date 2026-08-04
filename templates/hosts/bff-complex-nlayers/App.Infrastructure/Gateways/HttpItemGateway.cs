using System.Net.Http.Json;
using App.Core.Models;
using App.Core.Ports;
using App.Core.ValueObjects.Items;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mvp24Hours.Core.Contract.Infrastructure.Caching;
using Mvp24Hours.Infrastructure.Caching.HybridCache;

namespace App.Infrastructure.Gateways;

public sealed class HttpItemGatewayOptions
{
    public bool UseHttpGateway { get; set; }
    public string BaseAddress { get; set; } = "http://localhost:5100";
    public int TimeoutSeconds { get; set; } = 10;
    public int ListCacheMinutes { get; set; } = 2;
    public int ItemCacheMinutes { get; set; } = 3;
}

/// <summary>
/// HTTP gateway for calling a downstream item API. Register instead of
/// <see cref="InMemoryItemGateway"/> when backends are available.
/// </summary>
public sealed class HttpItemGateway(
    HttpClient httpClient,
    IOptions<HttpItemGatewayOptions> options,
    ICacheProvider cacheProvider,
    ILogger<HttpItemGateway> logger) : IItemGateway
{
    public async Task<IReadOnlyList<Item>> GetAllAsync(ItemQuery filter, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildListCacheKey(filter);
        var cached = await cacheProvider.GetAsync<List<Item>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var query = new List<string>();
        if (!string.IsNullOrEmpty(filter.Name))
        {
            query.Add($"name={Uri.EscapeDataString(filter.Name)}");
        }

        if (filter.Active is not null)
        {
            query.Add($"active={filter.Active.Value}");
        }

        var url = "api/item" + (query.Count > 0 ? "?" + string.Join('&', query) : string.Empty);
        logger.LogDebug("Fetching items from {BaseAddress}/{Url}", options.Value.BaseAddress, url);

        try
        {
            var response = await httpClient.GetFromJsonAsync<List<Item>>(url, cancellationToken) ?? [];
            await cacheProvider.SetAsync(cacheKey, response, new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, options.Value.ListCacheMinutes)),
                Tags = ["items", "items:list"]
            }, cancellationToken);
            return response;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to fetch item list from downstream API.");
            return [];
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Timeout fetching item list from downstream API.");
            return [];
        }
    }

    public async Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cacheKey = BuildItemCacheKey(id);
        var cached = await cacheProvider.GetAsync<Item>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        try
        {
            var item = await httpClient.GetFromJsonAsync<Item>($"api/item/{id}", cancellationToken);
            if (item is null)
            {
                return null;
            }

            await cacheProvider.SetAsync(cacheKey, item, new CacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(Math.Max(1, options.Value.ItemCacheMinutes)),
                Tags = ["items", "items:item"]
            }, cancellationToken);
            return item;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to fetch item id {ItemId} from downstream API.", id);
            return null;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Timeout fetching item id {ItemId} from downstream API.", id);
            return null;
        }
    }

    public async Task<int> CreateAsync(Item item, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/item", new { item.Name, item.Note }, cancellationToken);
            response.EnsureSuccessStatusCode();
            var created = await response.Content.ReadFromJsonAsync<Item>(cancellationToken);
            if (created?.Id is null or <= 0)
            {
                return 0;
            }

            await InvalidateItemCachesAsync(cancellationToken);
            return created.Id;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Failed to create item in downstream API.");
            return 0;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Timeout creating item in downstream API.");
            return 0;
        }
    }

    private static string BuildListCacheKey(ItemQuery filter)
    {
        var name = string.IsNullOrWhiteSpace(filter.Name)
            ? "all"
            : filter.Name.Trim().ToLowerInvariant();
        var active = filter.Active?.ToString() ?? "all";
        return $"bff:item:list:{name}:{active}";
    }

    private static string BuildItemCacheKey(int id)
    {
        return $"bff:item:{id}";
    }

    private async Task InvalidateItemCachesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await cacheProvider.InvalidateByTagsAsync(["items", "items:list", "items:item"], cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogDebug(ex, "Cache provider does not support tag invalidation.");
        }
    }
}
