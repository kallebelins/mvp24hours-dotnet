using App.Core.Models;
using App.Core.ValueObjects.Items;

namespace App.Core.Ports;

/// <summary>
/// Outbound port for calling downstream item APIs. Implemented by infrastructure gateways.
/// </summary>
public interface IItemGateway
{
    Task<IReadOnlyList<Item>> GetAllAsync(ItemQuery filter, CancellationToken cancellationToken = default);
    Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Item item, CancellationToken cancellationToken = default);
}
