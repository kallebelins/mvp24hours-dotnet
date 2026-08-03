using System.Collections.Concurrent;
using App.Core.Contract.Data;
using App.Core.Models;

namespace App.Infrastructure.Repositories;

public sealed class InMemoryItemRepository : IItemRepository
{
    private static readonly ConcurrentDictionary<int, Item> Store = new();
    private static int _nextId = 1;

    public Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Item> items = Store.Values.OrderBy(x => x.Id).ToList().AsReadOnly();
        return Task.FromResult(items);
    }

    public Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Store.TryGetValue(id, out var item);
        return Task.FromResult(item);
    }

    public Task<Item> AddAsync(Item item, CancellationToken cancellationToken = default)
    {
        item.Id = Interlocked.Increment(ref _nextId);
        Store[item.Id] = item;
        return Task.FromResult(item);
    }
}
