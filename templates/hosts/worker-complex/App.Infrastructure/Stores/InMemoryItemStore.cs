using System.Collections.Concurrent;
using App.Core.Contract.Data;
using App.Core.Models;

namespace App.Infrastructure.Stores;

public sealed class InMemoryItemStore : IItemStore
{
    private static readonly ConcurrentDictionary<int, Item> Pending = new();
    private static int _seeded;

    public InMemoryItemStore()
    {
        if (Interlocked.CompareExchange(ref _seeded, 1, 0) == 0)
        {
            Pending[1] = new Item { Id = 1, Name = "Seed item", Created = DateTime.UtcNow };
        }
    }

    public Task<IReadOnlyList<Item>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Item> items = Pending.Values.OrderBy(x => x.Id).ToList().AsReadOnly();
        return Task.FromResult(items);
    }

    public Task MarkProcessedAsync(int itemId, CancellationToken cancellationToken = default)
    {
        Pending.TryRemove(itemId, out _);
        return Task.CompletedTask;
    }
}
