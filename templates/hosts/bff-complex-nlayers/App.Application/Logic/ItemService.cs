using App.Core.Contract.Logic;
using App.Core.Models;
using App.Core.Ports;
using App.Core.ValueObjects.Items;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Core.DTOs.Models;
using Mvp24Hours.Core.ValueObjects.Logic;
using Mvp24Hours.Extensions;

namespace App.Application.Logic;

public class ItemService(
    IItemGateway gateway,
    IValidator<Item> validator,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<ItemService> logger) : IItemService
{
    public async Task<IPagingResult<IList<ItemResult>>> GetBy(ItemQuery filter, IPagingCriteria criteria, CancellationToken cancellationToken = default)
    {
        var items = await gateway.GetAllAsync(filter, cancellationToken);
        if (items.Count == 0)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusinessPaging<IList<ItemResult>>();
        }

        var mapped = mapper.Map<IList<ItemResult>>(items);
        var limit = criteria.Limit > 0 ? criteria.Limit : mapped.Count;
        var offset = criteria.Offset;
        IList<ItemResult> paged = mapped
            .Skip(offset)
            .Take(limit)
            .ToList();

        var totalCount = mapped.Count;
        var totalPages = limit > 0 ? (int)Math.Ceiling((double)totalCount / limit) : 1;

        return paged.ToBusinessPaging(
            new PageResult(limit, offset, paged.Count),
            new SummaryResult(totalCount, totalPages));
    }

    public async Task<IBusinessResult<ItemResult>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var item = await gateway.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusiness<ItemResult>();
        }

        return mapper.Map<ItemResult>(item)
            .ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }

    public async Task<IBusinessResult<int>> Create(ItemCreate dto, CancellationToken cancellationToken = default)
    {
        var entity = mapper.Map<Item>(dto);
        entity.Created = timeProvider.GetUtcNow().UtcDateTime;
        entity.Active = true;

        var errors = entity.TryValidate(validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        var id = await gateway.CreateAsync(entity, cancellationToken);
        if (id <= 0)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
        }

        logger.LogInformation("Created item {ItemId} via gateway", id);
        return id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
