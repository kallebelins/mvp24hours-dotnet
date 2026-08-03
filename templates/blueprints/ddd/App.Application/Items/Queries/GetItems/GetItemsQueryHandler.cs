using System.Linq.Expressions;
using App.Core.Entities;
using App.Core.ValueObjects.Items;
using Mvp24Hours.Core.Contract.Data;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using Mvp24Hours.Infrastructure.Cqrs.Abstractions;

namespace App.Application.Items.Queries.GetItems;

public sealed class GetItemsQueryHandler(IUnitOfWorkAsync unitOfWork)
    : IMediatorQueryHandler<GetItemsQuery, IPagingResult<List<ItemResult>>>
{
    public async Task<IPagingResult<List<ItemResult>>> Handle(GetItemsQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;

        Expression<Func<Item, bool>> clause =
            x => (string.IsNullOrEmpty(filter.Name) || x.Name.Contains(filter.Name))
                && (filter.Active == null || x.Active == filter.Active.Value);

        var repository = unitOfWork.GetRepository<Item>();
        var result = await repository.ToBusinessPagingAsync(clause, request.Criteria);

        if (!result.HasData())
        {
            return "RECORD_NOT_FOUND".ToMessageResult("RECORD_NOT_FOUND", MessageType.Error)
                .ToBusinessPaging<List<ItemResult>>();
        }

        var data = result.GetDataValue() ?? [];
        var mapped = data
            .Select(x => new ItemResult
            {
                Id = x.Id,
                Created = x.Created,
                Name = x.Name,
                Note = x.Note,
                Active = x.Active
            })
            .ToList();

        return mapped.ToBusinessPaging(result.Paging, result.Summary);
    }
}
