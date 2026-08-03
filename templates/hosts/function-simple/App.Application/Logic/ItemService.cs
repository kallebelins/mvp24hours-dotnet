using System.Collections.Concurrent;
using App.Core.Contract.Logic;
using App.Core.Models;

namespace App.Application.Logic;

public class ItemService : IItemService
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

    public Task<Item> CreateAsync(string name, string? note, CancellationToken cancellationToken = default)
    {
        var item = new Item
        {
            Id = Interlocked.Increment(ref _nextId),
            Name = name,
            Note = note,
            Created = DateTime.UtcNow
        };

        Store[item.Id] = item;
        return Task.FromResult(item);
    }
}
