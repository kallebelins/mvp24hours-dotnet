using App.Core.Models;

namespace App.Core.Contract.Logic;

public interface IItemService
{
    Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Item> CreateAsync(string name, string? note, CancellationToken cancellationToken = default);
}
