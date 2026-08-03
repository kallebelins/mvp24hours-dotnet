using System.Linq.Expressions;
using App.Core.Entities;
using App.Core.ValueObjects.Items;
using AutoMapper;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Queries.GetItems;

public class GetItemsQueryHandler(IUnitOfWorkAsync unitOfWork, IMapper mapper)
    : IMediatorQueryHandler<GetItemsQuery, IPagingResult<IList<ItemResult>>>
{
    public async Task<IPagingResult<IList<ItemResult>>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
    {
        Expression<Func<Item, bool>> clause =
            x => (string.IsNullOrEmpty(request.Filter.Name) || x.Name.Contains(request.Filter.Name))
                && (request.Filter.Active == null || x.Active == request.Filter.Active.Value);

        var repository = unitOfWork.GetRepository<Item>();
        var result = await repository.ToBusinessPagingAsync(clause, request.Paging);
        if (!result.HasData())
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusinessPaging<IList<ItemResult>>();
        }

        return mapper.MapPagingTo<IList<Item>, IList<ItemResult>>(result)!;
    }
}
