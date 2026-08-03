using App.Application.ValueObjects.Items;
using App.Domain.Entities;
using AutoMapper;
using FluentValidation;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.UpdateItem;

public class UpdateItemCommandHandler(
    IUnitOfWorkAsync unitOfWork,
    IMapper mapper,
    IValidator<Item> validator) : IMediatorCommandHandler<UpdateItemCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(UpdateItemCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Item>();
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error).ToBusiness<int>();
        }

        mapper.Map(new ItemUpdate { Name = request.Name, Note = request.Note }, entity);
        var errors = entity.TryValidate(validator);
        if (errors.AnySafe())
        {
            return errors.ToBusiness<int>();
        }

        await repository.ModifyAsync(entity, cancellationToken: cancellationToken);
        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) == 0)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
        }

        return entity.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
