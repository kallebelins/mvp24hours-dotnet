using App.Core.Entities;

namespace App.Core.Ports;

/// <summary>
/// Outbound port for persisting items. Implemented by infrastructure adapters.
/// </summary>
public interface IItemWritePort
{
    Task AddAsync(Item item, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
