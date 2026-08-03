using System.Collections.Concurrent;
using App.Core.Models;
using App.Core.Ports;
using App.Core.ValueObjects.Items;

namespace App.Infrastructure.Gateways;

/// <summary>
/// In-memory gateway stub so the BFF compiles and runs without a downstream API.
/// Replace with <see cref="HttpItemGateway"/> when wiring real backends.
/// </summary>
public sealed class InMemoryItemGateway : IItemGateway
{
    private static readonly ConcurrentDictionary<int, Item> Store = new();
    private static int _nextId = 1;

    public Task<IReadOnlyList<Item>> GetAllAsync(ItemQuery filter, CancellationToken cancellationToken = default)
    {
        var items = Store.Values
            .Where(x => string.IsNullOrEmpty(filter.Name) || x.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase))
            .Where(x => filter.Active is null || x.Active == filter.Active.Value)
            .OrderBy(x => x.Id)
            .ToList()
            .AsReadOnly();

        return Task.FromResult<IReadOnlyList<Item>>(items);
    }

    public Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Store.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<int> CreateAsync(Item item, CancellationToken cancellationToken = default)
    {
        item.Id = Interlocked.Increment(ref _nextId);
        Store[item.Id] = item;
        return Task.FromResult(item.Id);
    }
}
