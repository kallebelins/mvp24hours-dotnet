using System.Linq.Expressions;
using App.Core.Contract.Logic;
using App.Core.Entities;
using App.Core.ValueObjects.Items;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Application.Logic;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;

namespace App.Application.Logic;

public class ItemService(
    IUnitOfWorkAsync unitOfWork,
    IValidator<Item> validator,
    IMapper mapper,
    TimeProvider timeProvider,
    ILogger<ItemService> logger) : RepositoryPagingServiceAsync<Item, IUnitOfWorkAsync>(unitOfWork, validator), IItemService
{
    public async Task<IPagingResult<IList<ItemResult>>> GetBy(ItemQuery filter, IPagingCriteria criteria, CancellationToken cancellationToken = default)
    {
        Expression<Func<Item, bool>> clause =
            x => (string.IsNullOrEmpty(filter.Name) || x.Name.Contains(filter.Name))
                && (filter.Active == null || x.Active == filter.Active.Value);

        var result = await GetByWithPaginationAsync(clause, criteria, cancellationToken: cancellationToken);
        if (!result.HasData())
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusinessPaging<IList<ItemResult>>();
        }

        var mapped = mapper.MapPagingTo<IList<Item>, IList<ItemResult>>(result);
        if (mapped is null)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error)
                .ToBusinessPaging<IList<ItemResult>>();
        }

        return mapped;
    }

    public async Task<IBusinessResult<ItemResult>> GetById(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (entity is null)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusiness<ItemResult>();
        }

        return mapper.Map<ItemResult>(entity)
            .ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }

    public async Task<IBusinessResult<int>> Create(ItemCreate dto, CancellationToken cancellationToken = default)
    {
        var entity = mapper.Map<Item>(dto);
        entity.Created = timeProvider.GetUtcNow().UtcDateTime;
        entity.Active = true;

        var errors = entity.TryValidate(Validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        await Repository.AddAsync(entity, cancellationToken: cancellationToken);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) > 0)
        {
            logger.LogInformation("Created item {ItemId}", entity.Id);
            return entity.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
        }

        return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
    }

    public async Task<IBusinessResult<int>> Update(int id, ItemUpdate dto, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error).ToBusiness<int>();
        }

        mapper.Map(dto, entity);
        var errors = entity.TryValidate(Validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        await Repository.ModifyAsync(entity, cancellationToken: cancellationToken);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) == 0)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
        }

        return entity.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }

    public async Task<IBusinessResult<int>> Delete(int id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error).ToBusiness<int>();
        }

        await Repository.RemoveAsync(entity, cancellationToken: cancellationToken);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) == 0)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
        }

        return entity.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
