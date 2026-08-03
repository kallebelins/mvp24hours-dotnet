using App.Application.DTOs;
using App.Core.Entities;
using App.Core.Ports;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;

namespace App.Application.UseCases;

/// <summary>
/// Application service orchestrating use cases through outbound ports only (no repository/UoW).
/// </summary>
public sealed class ItemUseCase(
    IItemReadPort readPort,
    IItemWritePort writePort,
    TimeProvider timeProvider,
    ILogger<ItemUseCase> logger) : IItemUseCase
{
    public async Task<IBusinessResult<List<ItemResult>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await readPort.GetAllAsync(cancellationToken);
        if (items.Count == 0)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusiness<List<ItemResult>>();
        }

        var results = items.Select(MapToResult).ToList();
        return results.ToBusiness();
    }

    public async Task<IBusinessResult<ItemResult>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await readPort.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusiness<ItemResult>();
        }

        return MapToResult(item).ToBusiness();
    }

    public async Task<IBusinessResult<int>> CreateAsync(ItemCreate dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            return "VALIDATION_ERROR".ToMessageResult("Name is required.", MessageType.Error)
                .ToBusiness<int>();
        }

        var item = new Item
        {
            Created = timeProvider.GetUtcNow().UtcDateTime,
            Name = dto.Name.Trim(),
            Note = dto.Note,
            Active = true
        };

        await writePort.AddAsync(item, cancellationToken);
        if (await writePort.SaveChangesAsync(cancellationToken) <= 0)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
        }

        logger.LogInformation("Created item {ItemId}", item.Id);
        return item.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }

    private static ItemResult MapToResult(Item item) => new()
    {
        Id = item.Id,
        Created = item.Created,
        Name = item.Name,
        Note = item.Note,
        Active = item.Active
    };
}
