using App.Core.Entities;
using App.Core.Ports;
using App.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace App.Infrastructure.Adapters.Persistence;

/// <summary>
/// EF Core adapter implementing both read and write outbound ports.
/// </summary>
public sealed class ItemEFAdapter(EFDBContext db) : IItemReadPort, IItemWritePort
{
    public async Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await db.Item.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken cancellationToken = default)
        => await db.Item.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);

    public async Task AddAsync(Item item, CancellationToken cancellationToken = default)
        => await db.Item.AddAsync(item, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => db.SaveChangesAsync(cancellationToken);
}
