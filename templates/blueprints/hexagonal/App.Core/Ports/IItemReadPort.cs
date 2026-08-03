using App.Core.Entities;

namespace App.Core.Ports;

/// <summary>
/// Outbound port for reading items. Implemented by infrastructure adapters.
/// </summary>
public interface IItemReadPort
{
    Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken cancellationToken = default);
}
