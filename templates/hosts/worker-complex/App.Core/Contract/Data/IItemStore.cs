using App.Core.Models;

namespace App.Core.Contract.Data;

public interface IItemStore
{
    Task<IReadOnlyList<Item>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task MarkProcessedAsync(int itemId, CancellationToken cancellationToken = default);
}
