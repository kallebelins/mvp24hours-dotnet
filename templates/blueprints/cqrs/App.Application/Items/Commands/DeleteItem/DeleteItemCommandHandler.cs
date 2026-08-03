using App.Core.Entities;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Commands.DeleteItem;

public class DeleteItemCommandHandler(IUnitOfWorkAsync unitOfWork)
    : IMediatorCommandHandler<DeleteItemCommand, IBusinessResult<int>>
{
    public async Task<IBusinessResult<int>> Handle(DeleteItemCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Item>();
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error).ToBusiness<int>();
        }

        await repository.RemoveAsync(entity, cancellationToken: cancellationToken);
        if (await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken) == 0)
        {
            return "OPERATION_FAIL".ToMessageResult("OPERATION_FAIL", MessageType.Error).ToBusiness<int>();
        }

        return entity.Id.ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
