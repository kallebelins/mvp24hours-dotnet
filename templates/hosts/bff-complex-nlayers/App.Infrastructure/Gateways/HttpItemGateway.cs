using System.Net.Http.Json;
using App.Core.Models;
using App.Core.Ports;
using App.Core.ValueObjects.Items;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace App.Infrastructure.Gateways;

public sealed class HttpItemGatewayOptions
{
    public string BaseAddress { get; set; } = "http://localhost:5100";
}

/// <summary>
/// HTTP gateway for calling a downstream item API. Register instead of
/// <see cref="InMemoryItemGateway"/> when backends are available.
/// </summary>
public sealed class HttpItemGateway(
    HttpClient httpClient,
    IOptions<HttpItemGatewayOptions> options,
    ILogger<HttpItemGateway> logger) : IItemGateway
{
    public async Task<IReadOnlyList<Item>> GetAllAsync(ItemQuery filter, CancellationToken cancellationToken = default)
    {
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

        var response = await httpClient.GetFromJsonAsync<List<Item>>(url, cancellationToken);
        return response ?? [];
    }

    public async Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<Item>($"api/item/{id}", cancellationToken);
    }

    public async Task<int> CreateAsync(Item item, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("api/item", new { item.Name, item.Note }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<Item>(cancellationToken);
        return created?.Id ?? 0;
    }
}
