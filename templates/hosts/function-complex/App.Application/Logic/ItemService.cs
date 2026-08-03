using App.Core.Contract.Data;
using App.Core.Contract.Logic;
using App.Core.Models;

namespace App.Application.Logic;

public class ItemService(IItemRepository repository) : IItemService
{
    public Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken cancellationToken = default)
        => repository.GetAllAsync(cancellationToken);

    public Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => repository.GetByIdAsync(id, cancellationToken);

    public async Task<Item> CreateAsync(string name, string? note, CancellationToken cancellationToken = default)
    {
        var item = new Item
        {
            Name = name,
            Note = note,
            Created = DateTime.UtcNow
        };

        return await repository.AddAsync(item, cancellationToken);
    }
}
