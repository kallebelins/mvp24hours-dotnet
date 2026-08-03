using App.Core.Models;

namespace App.Core.Contract.Data;

public interface IItemRepository
{
    Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Item> AddAsync(Item item, CancellationToken cancellationToken = default);
}
