using App.Application.ValueObjects.Items;
using App.Domain.Entities;
using AutoMapper;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.CreateItem;

public class CreateItemCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    IValidator<Item> validator,
    TimeProvider timeProvider,
    ILogger<CreateItemCommandHandler> logger) : IMediatorCommandHandler<CreateItemCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(CreateItemCommand request, CancellationToken cancellationToken)
    {
        var dto = new ItemCreate { Name = request.Name, Note = request.Note };
        var entity = mapper.Map<Item>(dto);
        entity.Created = timeProvider.GetUtcNow().UtcDateTime;
        entity.Active = true;

        var errors = entity.TryValidate(validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        var repository = unitOfWork.GetRepository<Item>();
        await repository.AddAsync(entity, cancellationToken: cancellationToken);
        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) > 0)
        {
            logger.LogInformation("Created item {ItemId}", entity.Id);
            return entity.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
        }

        return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
    }
}
