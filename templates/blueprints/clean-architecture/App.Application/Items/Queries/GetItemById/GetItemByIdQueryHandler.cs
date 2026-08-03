using App.Application.ValueObjects.Items;
using App.Domain.Entities;
using AutoMapper;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Queries.GetItemById;

public class GetItemByIdQueryHandler(IUnitOfWorkAsync unitOfWork, IMapper mapper)
    : IMediatorQueryHandler<GetItemByIdQuery, IBusinessResult<ItemResult>>
{
    public async Task<IBusinessResult<ItemResult>> Handle(GetItemByIdQuery request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.GetRepository<Item>();
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken: cancellationToken);
        if (entity == null)
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusiness<ItemResult>();
        }

        return mapper.Map<ItemResult>(entity).ToBusiness("OPERATION_SUCCESS".ToMessageResult("OPERATION_SUCCESS", MessageType.Success));
    }
}
